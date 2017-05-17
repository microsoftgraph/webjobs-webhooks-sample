/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

namespace WebHooksSample
{
    using Microsoft.Azure.WebJobs;
    using System.Configuration;
    
    class Program
    {
        static void Main(string[] args)
        {
            var config = new JobHostConfiguration();

            // Enable this based on some configuration
            config.UseDevelopmentSettings();            

            config.StorageConnectionString = ConfigurationManager.AppSettings["webjobs"];
            config.DashboardConnectionString = null;
            config.UseTimers();

            var host = new JobHost(config);
            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
        }
    }
}
