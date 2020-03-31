---
page_type: sample
description: "Cet exemple Azure WebJobs montre comment commencer à recevoir des notifications de Microsoft Graph."
products:
- ms-graph
languages:
- csharp
extensions:
  contentType: samples
  technologies:
  - Microsoft Graph 
  services:
  - Users
  createdDate: 5/10/2017 5:24:11 PM
---

# Exemple de Webhooks Microsoft Graph à l’aide d’un kit de développement logiciel (SDK) WebJobs
Abonnez-vous à des [webhooks Microsoft Graph](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks) pour être averti lorsque les données de vos utilisateurs changent, de sorte que vous n’avez pas besoin d’interroger les modifications.

Cet exemple Azure WebJobs montre comment commencer à recevoir des notifications de Microsoft Graph. Microsoft Graph fournit un point de terminaison d’API unifiée pour accéder aux données à partir de Microsoft Cloud. 

>Cet exemple utilise le point de terminaison Azure AD pour obtenir un jeton d’accès pour les comptes professionnels ou scolaires. L’exemple utilise une autorisation uniquement pour les applications, mais les autorisations déléguées doivent fonctionner également.

Voici les tâches courantes qu’une application effectue avec des abonnements webhook :

- Obtenir le consentement pour vous abonner aux ressources des utilisateurs, puis obtenir un jeton d’accès.
- Utiliser le jeton d'accès pour [créer un abonnement](https://developer.microsoft.com/en-us/graph/docs/api-reference/beta/api/subscription_post_subscriptions) à une ressource.
- Renvoyer un jeton de validation pour confirmer l'URL de notification.
- Écouter les notifications de Microsoft Graph et répondre avec un code d’état 202.
- Demander plus d’informations sur les ressources modifiées à l’aide des données de la notification.

Une fois que l'application a créé un abonnement à l’aide du jeton d’accès à authentification application uniquement, Microsoft Graph envoie une notification au terminal enregistré lorsque des événements se produisent dans les données de l'utilisateur. L’application réagit ensuite à l’événement.

Cet exemple s’abonne à la ressource`Utilisateurs` pour les changements `mis à jour` et `supprimés`. Cet exemple part du principe que l’URL de notification est une [fonction Azure](https://docs.microsoft.com/en-us/azure/azure-functions/functions-overview), qui écoute les webhook sur http et ajoute immédiatement ces notifications à une file d’attente de stockage Azure. L’exemple utilise [SDK Azure WebJobs](https://docs.microsoft.com/en-us/azure/app-service-web/websites-dotnet-webjobs-sdk) pour se lier à la file d’attente de stockage Azure et recevoir de nouvelles notifications, telles qu’elles sont mises en file d’attente par la fonction Azure.

## Conditions préalables

Pour utiliser l’exemple Microsoft Graph Webhooks à l’aide d’un SDK WebJobs, vous avez besoin des éléments suivants :

* Visual Studio 2017 installé sur votre ordinateur de développement. 

* Un [compte professionnel ou scolaire](http://dev.office.com/devprogram).

* ID de l’application et clé de l’application que vous [inscrivez sur le portail Azure](#register-the-app).

* Un point de terminaison public HTTPS pour recevoir et envoyer des demandes HTTP. Vous pouvez héberger celui-ci sur Microsoft Azure ou un autre service. Cet exemple a été testé à l’aide de la [fonction Azure](https://docs.microsoft.com/en-us/azure/azure-functions/functions-overview)

* Un compte de stockage Azure qui sera utilisé par le [Kit de développement de Azure WebJobs](https://docs.microsoft.com/en-us/azure/app-service-web/websites-dotnet-webjobs-sdk) 

### Créer votre application

#### Sélectionnez le locataire dans lequel vous voulez créer votre application

1. Connectez-vous au [Portail Microsoft Azure](https://portal.azure.com) à l’aide d’un compte professionnel ou scolaire.
1. Si votre compte est présent dans plusieurs locataires Azure AD :
   1. Sélectionnez votre profil dans le menu situé dans le coin supérieur droit de la page, puis **Basculer entre les répertoires**.
   1. Sélectionnez le locataire Azure AD dans lequel vous souhaitez créer votre application.

#### Inscription de l’application

1. Accédez au [Portail Microsoft Azure > enregistrement des applications](https://go.microsoft.com/fwlink/?linkid=2083908) pour enregistrer votre application.
![Inscription de l’application](readme-images/aad-app-registration.PNG)
1. Sélectionnez **Nouvelle inscription**.
![Inscription de l’application](readme-images/aad-new-registration.PNG)
1. Lorsque la **page Inscrire une application** s’affiche, saisissez les informations d’inscription de votre application :
   1. Dans la section **Nom**, saisissez un nom explicite qui s’affichera pour les utilisateurs de l’application. Par exemple : `MyWebApp`
   1. Dans la section **Types de comptes pris en charge**, sélectionnez **Comptes dans un annuaire organisationnel et comptes personnels Microsoft (par ex. Skype, Xbox, Outlook.com)**.
1. Sélectionnez **S’inscrire** pour créer l’application.
![Inscrire une application](readme-images/aad-register-an-app.png)
1. Sur la page **Vue d’ensemble** de l’application, notez la valeur **ID d’application (client)** et conservez-la pour plus tard. Vous aurez besoin de cette valeur pour paramétrer le fichier de configuration de Visual Studio pour ce projet.
![ID de l’application](readme-images/aad-application-id.PNG)
1. Dans la liste des pages de l’application, sélectionnez **Authentification**.
   1. Dans la section **URI de redirection**, sélectionnez **Web** dans la zone de liste déroulante et entrez les URI de redirection suivants :
       - `https://mysigninurl`
	   ![de redirection d’URI](readme-images/aad-redirect-uri.PNG)
1. Sélectionnez **Enregistrer**.
1. Dans la page **Certificats et clés secrètes**, dans la section **Clés secrètes de clients**, sélectionnez **Nouvelle clé secrète client**.
   1. Entrez une description de clé (par exemple `clé secrète de l’application`),
   1. Sélectionnez une durée de clé : **Dans 1 an**, **Dans 2 ans** ou **N’expire jamais**.
   ![Clé secrète client](readme-images/aad-new-client-secret.png)
   1. Lorsque vous cliquez sur le bouton **Ajouter**, la valeur de la clé s’affiche. Copiez la clé et enregistrez-le dans un endroit sûr.
   ![Clé secrète client](readme-images/aad-copy-client-secret.png)
   Vous aurez besoin de cette clé ultérieurement pour configurer le projet dans Visual Studio. Cette valeur de clé ne sera plus affichée, ni récupérée par d’autres moyens. Par conséquent, enregistrez-la dès qu’elle est visible depuis le Portail Microsoft Azure.
      

1. Dans la liste des pages de l’application, sélectionnez **Permissions API**.
   1. Cliquez sur le bouton **Ajouter une autorisation**, puis assurez-vous que l’onglet **Microsoft APIs** est sélectionné.
   1. Dans la section **API Microsoft couramment utilisées**, sélectionnez **Microsoft Graph**.
   1. Dans la section **Autorisations applicatives**, assurez-vous que l’autorisation **Directory.Read.All** est activée. Utilisez la zone de recherche, le cas échéant.
   1. Cliquez sur le bouton **Ajouter des autorisations**.

1. Dans la page **Gérer**, sélectionnez **Autorisations API** > **Ajouter une autorisation**.

    ![Capture d’écran des autorisations de sélection API](readme-images/aad-api-permissions.PNG)

1. Sélectionnez **API Microsoft** > **Microsoft Graph**.

    ![Capture d’écran des autorisations de requête API](readme-images/aad-request-api-permissions.PNG)

1. Sélectionnez **Autorisations de l’application**. Dans la zone de recherche, tapez **directory.read.all** et sélectionnez la première option dans la liste. Sélectionnez **Ajouter des autorisations**.

    ![Capture d’écran des autorisations déléguées](readme-images/aad-application-permissions.PNG)


## Configurer la fonction Azure
Vous devez exposer un point de terminaison public HTTPS pour créer un abonnement et recevoir des notifications de Microsoft Graph. Vous pouvez utiliser les fonctions Azure pour ceci.

1. Connectez-vous au [Portail Azure](https://portal.azure.com/) à l’aide d’un compte professionnel ou scolaire.

2. Sélectionnez **Applications fonction** dans le volet de navigation gauche.

3. Suivez les instructions pour créer une nouvelle **application de fonction**

4. Créez une nouvelle fonction**Webhook/API** à l’aide de l’exemple de code suivant

```csharp
using System.Net;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<string> queue, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");

    // parse query parameter
    string validationToken = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "validationToken", true) == 0)
        .Value;
    
    log.Info("validationToken: " + validationToken);

    // Get request body
    string data = await req.Content.ReadAsStringAsync();

    log.Info("Body of request: " + data);

    queue.Add(data);

    log.Info("Added message to queue");

    return string.IsNullOrWhiteSpace(validationToken)
        ? req.CreateResponse(HttpStatusCode.Accepted, "Notification received")
        : req.CreateResponse(HttpStatusCode.OK, validationToken);
}
```

5. Sélectionnez **Intégrer** > **Nouvelle sortie** > **Espace de stockage file d’attente Azure** 

6. Entrez `file d’attente` comme **nom du paramètre de message**

7. Entrez `webhooksnotificationqueue` comme nom de **la file d’attente**

8. Utiliser une nouvelle ou existante **connexion à un compte de stockage**

9. Sélectionnez la fonction créée et**Exécutez** à partir du portail pour vérifier qu’elle réussit.

10. Sélectionnez **Obtenir l’URL de la fonction** pour copier l’URL de notification à utiliser dans l’exemple.

## Exemple de projet de configuration

1. Dans l’Explorateur de solutions, sélectionnez le projet **App.config**.

	a. Pour la clé **clientId**, remplacez *ENTER_YOUR_APP_ID* par l’ID d’application de votre application Azure inscrite.
	
	b. Pour la clé**clientSecret**, remplacez *ENTER_YOUR_SECRET* par la clé de votre application Azure enregistrée.  
	
	c. Pour la clé de **tenantId**, remplacez *ENTER_YOUR_ORGANIZATION_ID* par l’ID de votre organisation.

	d. Pour la clé **webjobs**, remplacez *ENTER_YOUR_AZURE_STORAGE_CONNECTION_STRING* par une chaîne de connexion de l’espace de stockage Azure intégrée dans la fonction Azure.
	
	e. Pour la clé **notificationURL**, remplacez *ENTER_YOUR_NOTIFICATION_URL* par l’URL de la fonction Azure.

## Utiliser l’exemple d’application
1. Appuyez sur **F5** pour démarrer votre exemple.

2. Attendez que l’exemple imprime le message **Créer un nouvel abonnement avec ID :**

3. Mettre à jour les propriétés d’un utilisateur de l’organisation. Exemple : Mettre à jour son numéro de téléphone

4. Sous quelques minutes, l’exemple doit recevoir une notification pour l’utilisateur mis à jour avec son identification.

5. Toutes les 30 secondes, l’échantillon renouvelle l’abonnement. Vous pouvez modifier la durée de l’opération mise à jour pour qu’elle soit effectuée toutes les 24 heures.

## Composants clés de l’exemple

**Contrôleurs**  
-[`Function.cs`](https://github.com/microsoftgraph/webjobs-webhooks-sample/blob/master/WebHooksSample/Functions.cs) gère les abonnements et reçoit les notifications.  
-[`Program.cs`](https://github.com/microsoftgraph/webjobs-webhooks-sample/blob/master/WebHooksSample/SubscriptionController.cs) amorce l’hôte Azure WebJob.
 
## Résolution des problèmes

| Problème | Résolution |
| :------| :------|
| Vous obtenez une réponse 403 Interdit lorsque vous essayez de créer un abonnement. | Assurez-vous que l’inscription de l’application inclut l’autorisation d’application **Read directory data** pour Microsoft Graph (comme décrit dans la section [Inscrire l’application](#register-the-app)) |  
| Vous ne recevez pas de notifications. | Consultez les journaux pour la fonction Azure dans le[portail](https://portal.azure.com/). Si Microsoft Graph n’envoie pas de notifications, veuillez ouvrir un problème de dépassement de capacité de la pile marqué*[MicrosoftGraph]*. Incluez l’ID de l’abonnement et l’heure à laquelle il a été créé.<br /><br /> |  
| Vous recevez une réponse *La demande de validation d’abonnement a expiré*. | Cela indique que Microsoft Graph n’a pas reçu de réponse de validation dans le délai prévu (environ 10 secondes).<br /><br />Si vous avez créé une fonction Azure sur **régime de consommation**, la fonction peut être mise en veille en raison de l’inactivité. L’exemple réessaiera et devrait réussir dans les prochaines tentatives. Vous pouvez également essayer de créer une fonction Azure à l’aide du plan de service d’application. |  
| Vous recevez des erreurs pendant l’installation des packages. | vérifiez que le chemin d’accès local où vous avez sauvegardé la solution n’est pas trop long/profond. Pour résoudre ce problème, il vous suffit de déplacer la solution dans un dossier plus près du répertoire racine de lecteur. |

<a name="contributing"></a>
## Contribution ##

Si vous souhaitez contribuer à cet exemple, voir [CONTRIBUTING.MD](/CONTRIBUTING.md).

Ce projet a adopté le [code de conduite Open Source de Microsoft](https://opensource.microsoft.com/codeofconduct/). Pour en savoir plus, reportez-vous à la [FAQ relative au code de conduite](https://opensource.microsoft.com/codeofconduct/faq/) ou contactez [opencode@microsoft.com](mailto:opencode@microsoft.com) pour toute question ou tout commentaire.

## Questions et commentaires

N’hésitez pas à nous faire part de vos commentaires sur l’exemple de webhooks Microsoft Graph utilisant le kit de développement logiciel webjobs. Vous pouvez nous faire part de vos questions et suggestions dans la rubrique [Problèmes](https://github.com/microsoftgraph/webjobs-webhooks-sample/issues) de ce référentiel.

Les questions générales sur Microsoft Graph doivent être publiées sur la page [Dépassement de capacité de la pile](https://stackoverflow.com/questions/tagged/MicrosoftGraph). Veillez à poser vos questions ou à rédiger vos commentaires en utilisant les tags *[MicrosoftGraph]*.

Si vous avez des suggestions de fonctionnalité, soumettez votre idée sur notre page [Voix utilisateur](https://officespdev.uservoice.com/) et votez pour votre suggestion.

## Ressources supplémentaires

* [Exemple de Webhooks Microsoft Graph Node.js](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample)
* [Utiliser des Webhooks dans Microsoft Graph](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks)
* [Ressource abonnement](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/subscription)
* [Site des développeurs de Microsoft Graph](https://developer.microsoft.com/en-us/graph/)
* [Appel de Microsoft Graph dans une application ASP.NET MVC](https://developer.microsoft.com/en-us/graph/docs/platform/aspnetmvc)

Copyright (c) 2019 Microsoft Corporation. Tous droits réservés.
