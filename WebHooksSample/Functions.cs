/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

namespace WebHooksSample
{
    using Microsoft.Azure.WebJobs;
    using Microsoft.Graph;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    /// <summary>
    /// This class will include all functions responsible for managing microsoft graph webhook subscriptions and 
    /// for receiving notifications through Azure Queues.
    /// Documentation on webjobs can be found here: https://docs.microsoft.com/en-us/azure/app-service-web/websites-dotnet-webjobs-sdk
    /// </summary>
    public class Functions
    {        
        private const string ODataDateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";
        private const string QueueName = "webhooksnotificationqueue";
        private const string SubscriptionRenewalSchedule = "00:00:30";
        private const string ContentTypeApplicationJson = "application/json";
        private const string ResourceMicrosoftGraph = "https://graph.microsoft.com";
        private const string SubscriptionsEntitySetUrl = "https://graph.microsoft.com/beta/subscriptions";
        private const string SubscriptionUrlTemplate = "https://graph.microsoft.com/beta/subscriptions/{0}";


        private static readonly string SubscriptionBlobName = ConfigurationManager.AppSettings["subscriptionblobname"];
        private static readonly HttpClient Client = new HttpClient();
        private static readonly string ClientId = ConfigurationManager.AppSettings["clientId"];
        private static readonly string ClientSecret = ConfigurationManager.AppSettings["clientSecret"];
        private static readonly string TenantId = ConfigurationManager.AppSettings["tenantId"];
        private static readonly string NotificationUrl = ConfigurationManager.AppSettings["notificationurl"];
        private static readonly HttpMethod HttpMethodPatch = new HttpMethod("PATCH");
        
        /// <summary>
        /// This function will execute on a timer. For testing purposes, it is setup to execute every 30 seconds. 
        /// Typically one would trigger these every 24 hours to ensure subscriptions don't expire.
        /// </summary>
        /// <param name="timer">Timer object containing schedule information.</param>
        /// <param name="binder">Binder object can be used to bind to remote resources such as Azure storage blobs, queues etc.</param>
        /// <param name="cancellationToken">A cancellation token that can be used for long executing functions, in case webjobs is shutting down.</param>
        /// <param name="log">Log object for adding diagnostic logs.</param>
        /// <returns></returns>
        public static async Task ManageSubscriptions(
            [TimerTrigger(Functions.SubscriptionRenewalSchedule, RunOnStartup = true)]TimerInfo timer,
            IBinder binder,
            CancellationToken cancellationToken,
            TextWriter log)
        {
            // Step#1: Obtain App only auth token
            string authority = string.Format(
                CultureInfo.InvariantCulture,
                "https://login.windows.net/{0}",
                Functions.TenantId);

            ClientCredential clientCredential = new ClientCredential(Functions.ClientId, Functions.ClientSecret);

            AuthenticationContext context = new AuthenticationContext(authority);
            AuthenticationResult authenticationResult = await context.AcquireTokenAsync(Functions.ResourceMicrosoftGraph, clientCredential);
            
            // Step#2: Check if a subscription already exists from a cache. We are using blob storage to cache the subscriptionId.
            // If a subscription doesn't exist in blob store, then we need to create one.
            BlobAttribute blobAttributeRead = new BlobAttribute(Functions.SubscriptionBlobName, FileAccess.Read);
            Stream steram = binder.Bind<Stream>(blobAttributeRead);
            StreamBinder streamBinder = new StreamBinder();
            string subscriptionId = await streamBinder.ReadFromStreamAsync(steram, cancellationToken);

            bool subscriptionExists = false;
            string subscriptionsUrl = null;

            if (!string.IsNullOrWhiteSpace(subscriptionId))
            {
                subscriptionsUrl = string.Format(
                    CultureInfo.InvariantCulture,
                    Functions.SubscriptionUrlTemplate,
                    subscriptionId);

                using (HttpRequestMessage message = new HttpRequestMessage(
                                        HttpMethod.Post,
                                        subscriptionsUrl))
                {
                    message.Headers.Authorization = new AuthenticationHeaderValue(
                                authenticationResult.AccessTokenType,
                                authenticationResult.AccessToken);

                    HttpResponseMessage response = await Functions.Client.SendAsync(
                        message);
                    subscriptionExists = response.IsSuccessStatusCode;                    
                }
            }

            if (!subscriptionExists)
            {
                Subscription subscription = new Subscription();
                subscription.ChangeType = "updated,deleted";
                subscription.ClientState = "mysecret";
                subscription.ExpirationDateTime = DateTime.UtcNow.AddDays(2);
                subscription.NotificationUrl = Functions.NotificationUrl;
                subscription.Resource = "Users";

                using (HttpRequestMessage message = new HttpRequestMessage(
                                    HttpMethod.Post,
                                    Functions.SubscriptionsEntitySetUrl))
                {
                    message.Content = new StringContent(
                        JsonConvert.SerializeObject(subscription),
                        Encoding.UTF8,
                        Functions.ContentTypeApplicationJson);

                    message.Headers.Authorization = new AuthenticationHeaderValue(
                        authenticationResult.AccessTokenType,
                        authenticationResult.AccessToken);

                    HttpResponseMessage response = await Functions.Client.SendAsync(
                        message);
                    response.EnsureSuccessStatusCode();
                    subscription = await response.Content.ReadAsAsync<Subscription>();

                    BlobAttribute blobAttributeWrite = new BlobAttribute(Functions.SubscriptionBlobName, FileAccess.Write);
                    Stream newState = binder.Bind<Stream>(blobAttributeWrite);
                    await streamBinder.WriteToStreamAsync(subscription.Id, newState, cancellationToken);

                    log.WriteLine("Created new subscription with id:" + subscription.Id);
                }
            }
            else
            {
                // Step#3: If a subscription already exists in blob store, then we will extend the expiration date by 1 day.
                Dictionary<string, string> update = new Dictionary<string, string>();
                update.Add("expirationDateTime", DateTime.UtcNow.AddDays(1).ToString(Functions.ODataDateTimeFormat));

                using (HttpRequestMessage message = new HttpRequestMessage(
                                    Functions.HttpMethodPatch,
                                    subscriptionsUrl))
                {
                    message.Content = new StringContent(
                        JsonConvert.SerializeObject(update),
                        Encoding.UTF8,
                        Functions.ContentTypeApplicationJson);

                    message.Headers.Authorization = new AuthenticationHeaderValue(
                        authenticationResult.AccessTokenType,
                        authenticationResult.AccessToken);

                    HttpResponseMessage response = await Functions.Client.SendAsync(message);
                    response.EnsureSuccessStatusCode();

                    log.WriteLine("Updated existing subscription with id:" + subscriptionId);
                }
            }
        }

        /// <summary>
        /// This function will get triggered when a new message is added on an Azure Queue called 'webhooksnotificationqueue'.
        /// An Azure function is used to receive webhook notifications and immediately post such messages to azure queues.
        /// </summary>
        /// <param name="message">Message received from azure queue.</param>
        /// <param name="log">Log object for adding diagnostic logs.</param>
        public static void OnNotificationReceived(
            [QueueTrigger(Functions.QueueName)] string message,
            TextWriter log)
        {
            log.WriteLine(message);
        }
    }
}
