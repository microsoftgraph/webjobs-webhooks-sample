---
page_type: sample
description: "Este exemplo do Azure WebJobs mostra como começar a receber notificações do Microsoft Graph."
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

# Exemplo do Microsoft Graph Webhooks usando o SDK do WebJobs
Inscreva-se no [Microsoft Graph Webhooks](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks) para ser notificado quando os dados do usuário mudarem, para que você não precise fazer enquetes de mudanças.

Este exemplo do Azure WebJobs mostra como começar a receber notificações do Microsoft Graph. O Microsoft Graph oferece um terminal de API unificado para acessar dados da nuvem do Microsoft. 

>Este exemplo usa o terminal do Azure AD para obter um token de acesso de contas corporativas ou escolares. O exemplo usa uma permissão somente para o aplicativo, mas permissões delegadas também devem funcionar.

A seguir, são apresentadas tarefas comuns que um aplicativo executa com assinaturas dos webhooks:

- Obtenha consentimento para se inscrever nos recursos de usuários e receber um token de acesso.
- Use o token de acesso para [criar uma assinatura](https://developer.microsoft.com/en-us/graph/docs/api-reference/beta/api/subscription_post_subscriptions) para um recurso.
- Devolva um token de validação para confirmar a URL da notificação.
- Ouça as notificações do Microsoft Graph e responda com o código de status 202.
- Solicite mais informações dos recursos alterados usando os dados da notificação.

Depois que o aplicativo cria uma assinatura usando apenas o token de autenticação do aplicativo, o Microsoft Graph envia uma notificação para o terminal de notificação registrado quando acontecem eventos nos dados do usuário. Em seguida, o aplicativo reage ao evento.

Este exemplo se inscreve no recurso `Usuários` para `alterações atualizadas` e ` excluídas. O exemplo supõe que a URL da notificação é uma [Função do Azure](https://docs.microsoft.com/en-us/azure/azure-functions/functions-overview) que monitora o webhook por http e adiciona imediatamente essas notificações a uma fila de armazenamento do Azure. O exemplo usa o [Azure WebJobs SDK](https://docs.microsoft.com/en-us/azure/app-service-web/websites-dotnet-webjobs-sdk) para associar à fila de armazenamento do Azure e receber novas notificações à medida que elas são enfileiradas pela função do Azure.

## Pré-requisitos

Para usar o exemplo Microsoft Graph Webhooks usando o WebJobs SDK, você precisa do seguinte:

* Visual Studio 2017 instalado no computador de desenvolvimento. 

* Uma [conta corporativa ou de estudante](http://dev.office.com/devprogram).

* A ID do aplicativo e a chave do aplicativo que você [registra no Portal do Azure](#register-the-app).

* Um terminal do HTTPS público para receber e enviar solicitações HTTP. Você pode hospedar isso no Microsoft Azure ou em outro serviço. Este exemplo foi testado usando a [Função do Azure](https://docs.microsoft.com/en-us/azure/azure-functions/functions-overview)

* Uma conta de armazenamento do Azure que será usada pelo [Azure WebJobs SDK](https://docs.microsoft.com/en-us/azure/app-service-web/websites-dotnet-webjobs-sdk) 

### Criar seu aplicativo

#### Escolher o locatário para o qual você quer criar seu aplicativo

1. Entre no [Portal do Azure](https://portal.azure.com) usando uma conta corporativa ou de estudante.
1. Se a sua conta estiver presente em mais de um locatário do Azure AD:
   1. Selecione seu perfil no menu no canto superior direito da página e **Alterne o diretório**.
   1. Altere sua sessão no locatário do Azure AD em que você quer criar o aplicativo.

#### Registrar o aplicativo

1. Navegue até o [Portal do Azure > Registros de aplicativos](https://go.microsoft.com/fwlink/?linkid=2083908) para registrar seu aplicativo.
![Registro do aplicativo](readme-images/aad-app-registration.PNG)
1. Selecione **Novo registro**.
![Registro do Aplicativo](readme-images/aad-new-registration.PNG)
1. Quando a **página Registrar um aplicativo** for exibida, insira as informações de registro do aplicativo:
   1. Na seção **Nome**, insira um nome relevante que será exibido aos usuários do aplicativo. Por exemplo: `MyWebApp`
   1. Na seção **Tipos de conta com suporte**, selecione **Contas em qualquer diretório organizacional e contas pessoais do Microsoft (por exemplo: Skype, Xbox, Outlook.com)**.
1. Selecione **Registrar** para criar o aplicativo.
![Registrar um aplicativo](readme-images/aad-register-an-app.png)
1. Na página **Visão geral** do aplicativo, encontre o valor de **ID do aplicativo (cliente)** e registre-o para usar mais tarde. Esse valor será necessário para configurar o arquivo de configuração do Visual Studio desse projeto.
![ID do Aplicativo](readme-images/aad-application-id.PNG)
1. Na lista de páginas do aplicativo, selecione **Autenticação**.
   1. Na seção **Redirecionar URIs**, selecione **Web** na caixa de combinação e digite as seguintes URIs de redirecionamento:
       - `https://mysigninurl`
	   ![URI de redirecionamento](readme-images/aad-redirect-uri.PNG)
1. Selecione **Salvar**.
1. Na página **Certificados e segredos**, na seção **Segredos do cliente**, escolha **Novo segredo do cliente**.
   1. Insira uma descrição da chave (da instância `segredo do aplicativo`).
   1. Selecione uma duração de chave de **1 ano**, **2 anos** ou **Nunca expirará**.
   ![Segredo do cliente](readme-images/aad-new-client-secret.png)
   1. Ao clicar no botão **Adicionar**, o valor da chave será exibido. Copie o valor da chave e salve-o em um local seguro.
   ![Segredo do cliente](readme-images/aad-copy-client-secret.png)
   Você precisará dessa chave mais tarde para configurar o projeto no Visual Studio. Esse valor da chave não será exibido novamente, nem será recuperável por nenhum outro meio, portanto, grave-o assim que estiver visível no portal do Azure.
      

1. Na lista de páginas do aplicativo, selecione **Permissões do API**.
   1. Clique no botão **Adicionar uma permissão** e verifique se a guia **APIs da Microsoft** está selecionada.
   1. Na seção **APIs mais usadas do Microsoft**, selecione **Microsoft Graph**.
   1. Na seção **Permissões do aplicativo**, certifique-se de que a permissão **Mail.Read.** está marcada. Use a caixa de pesquisa, se necessário.
   1. Selecione o botão **Adicionar permissão**.

1. Na página **Gerenciar**, selecione **Permissões do API** > **Adicionar uma permissão**. 

    ![Uma captura de tela das permissões de selecionar API](readme-images/aad-api-permissions.PNG)

1. Escolha **API do Microsoft** > **Microsoft Graph**.

    ![Uma captura de tela das permissões de selecionar API](readme-images/aad-request-api-permissions.PNG)

1. Selecione **Permissões do aplicativo. Na caixa de pesquisa, digite **directory.read.all** e selecione a primeira opção na lista. Selecione **Adicionar permissões**.

    ![Uma captura de tela das permissões delegadas](readme-images/aad-application-permissions.PNG)


## Configurar função do Azure
Você deve expor um terminal HTTPS público para criar uma assinatura e receber as notificações do Microsoft Graph. Você pode usar as funções do Azure para o mesmo.

1. Entre no [Portal do Azure](https://portal.azure.com/) usando uma conta corporativa ou de estudante.

2. Escolha **Aplicativos da função** no painel de navegação à esquerda.

3. Siga as instruções para criar um novo **Aplicativo da função**

4. Criar uma nova função **WebHook/API** com o seguinte exemplo de código

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

5. Escolha integrar > Nova saída > **Azure Queue Storage** 

6. Insira `fila` como **nome do Parâmetro da mensagem**

7. Insira `webhooksnotificationqueue` como **Nome da fila**

8. Use uma nova **Conexão de conta de armazenamento** existente ou crie uma

9. Escolha a função criada e **Execute** do portal para garantir que ela tenha êxito.

10. Escolha em **Obter URL da função** para copiar a notificação a ser usada no exemplo.

## Configure o projeto do exemplo

1. No Gerenciador de Soluções, selecione o projeto **App.config**.

	a. Na chave **clientId**, substitua *ENTER_YOUR_APP_ID* com a ID do aplicativo registeredAzure.
	
	b. Na chave **ClientSecret**, substitua *ENTER_YOUR_SECRET* com a chave do seu aplicativo registeredAzure.  
	
	c. Na chave **tenantid**, substitua *ENTER_YOUR_ORGANIZATION_ID* pela ID da sua organização.

	d. Na chave **webjobs**, substitua *ENTER_YOUR_AZURE_STORAGE_CONNECTION_STRING* com série de conexões do armazenamento azure integrado na função do Azure.
	
	e. Na chave **notificationurl**, substitua *ENTER_YOUR_NOTIFICATION_URL* pela URL da função do Azure.

## Usar o aplicativo do exemplo
1. Pressione **F5** para iniciar seu exemplo.

2. Aguarde até que o exemplo imprima a mensagem **Criou uma nova assinatura com a ID:**

3. Atualize qualquer propriedade de qualquer usuário na organização. Exemplo: Atualizar o número de telefone

4. Em poucos minutos, o exemplo deve receber uma notificação para o usuário atualizado junto com a respectiva identificação.

5. A cada 30 segundos, o exemplo renovará a assinatura. Pode-se alterar o período de operação atualizado uma vez a cada 24 horas.

## Componentes principais do exemplo

**Controladores**  
- [`Function.cs`](https://github.com/microsoftgraph/webjobs-webhooks-sample/blob/master/WebHooksSample/Functions.cs) Gerencia assinaturas e recebe notificações.  
- [`Program.cs`](https://github.com/microsoftgraph/webjobs-webhooks-sample/blob/master/WebHooksSample/SubscriptionController.cs) Inicialize o organizador do Azure WebJob.
 
## Solução de problemas

| Problema | Solução |
|:------|:------|
| Você recebe uma resposta de 403 Proibido ao tentar criar uma assinatura. | Certifique-se de que o registro do aplicativo inclui a permissão do aplicativo **Mail.Read** do Microsoft Graph (como descrito na seção [Registrar o aplicativo](#register-the-app)). |  
| Você não recebe notificações. | Verifique a função logs do Azure no](https://portal.azure.com/)portal[. Se o Microsoft Graph não estiver enviando notificações, abra um problema no Stack Overflow marcado com *[MicrosoftGraph]*. Inclua a ID da assinatura e o horário da criação.<br /><br /> |  
| Você recebe uma resposta *A solicitação de validação da assinatura expirou*. | Isso indica que o Microsoft Graph não recebeu uma resposta de validação dentro do prazo esperado (de aproximadamente 10 segundos).<br /><br />Se você criou a função Azure em **plano de consumo**, a função poderá ser suspensa devido a inatividade. O exemplo tentará novamente e deve ser bem-sucedido nas próximas tentativas. Como alternativa, tente criar a função Azure usando o plano de serviços do aplicativo. |  
| Você recebe erros ao instalar pacotes. | Verifique se o caminho local onde você colocou a solução não é muito longo/extenso. Mover a solução para mais perto da unidade raiz resolverá esse problema. |

<a name="contributing"></a>
## Colaboração ##

Se quiser contribuir para esse exemplo, confira [CONTRIBUTING.MD](/CONTRIBUTING.md).

Este projeto adotou o [Código de Conduta de Código Aberto da Microsoft](https://opensource.microsoft.com/codeofconduct/).  Para saber mais, confira as [Perguntas frequentes sobre o Código de Conduta](https://opensource.microsoft.com/codeofconduct/faq/) ou entre em contato pelo [opencode@microsoft.com](mailto:opencode@microsoft.com) se tiver outras dúvidas ou comentários.

## Perguntas e comentários

Gostaríamos de receber seus comentários sobre o exemplo Microsoft Graph Webhooks usando o WebJobs SDK. Você pode nos enviar perguntas e sugestões na seção [Problemas](https://github.com/microsoftgraph/webjobs-webhooks-sample/issues) deste repositório.

Em geral, as perguntas sobre o Microsoft Graph devem ser postadas no [Stack Overflow](https://stackoverflow.com/questions/tagged/MicrosoftGraph). Verifique se suas perguntas ou comentários estão marcados com *[MicrosoftGraph]*.

Se você tiver uma sugestão de recurso, poste sua ideia na nossa página em [Voz do Usuário](https://officespdev.uservoice.com/) e vote em suas sugestões.

## Recursos adicionais

* [](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample)Exemplo de Microsoft Graph Node.js Webhooks
* [Trabalhando com o Webhooks no Microsoft Graph](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks)
* [Recurso da assinatura](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/subscription)
* [](https://developer.microsoft.com/en-us/graph/)Site do desenvolvedor do Microsoft Graph
* [Chamar o Microsoft Graph em um aplicativo do ASP.NET MVC](https://developer.microsoft.com/en-us/graph/docs/platform/aspnetmvc)

Direitos autorais (c) 2019 Microsoft Corporation. Todos os direitos reservados.
