---
page_type: sample
description: "此 Azure WebJobs 示例演示了如何开始从 Microsoft Graph 获取通知。"
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

# 使用 WebJobs SDK 的 Microsoft Graph Webhook 示例
订阅 [Microsoft Graph Webhook](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks)，在用户数据更改时获得通知，以便无需轮询更改。

此 Azure WebJobs 示例演示了如何开始从 Microsoft Graph 获取通知。Microsoft Graph 提供一个统一的 API 终结点，用于从 Microsoft 云访问数据。 

>此示例使用 Azure AD 终结点获取工作或学校帐户的访问令牌。此示例使用仅限应用程序的权限，但委派的权限也应正常发挥作用。

下面是应用程序可通过 Webhook 订阅执行的常见任务：

- 获得订阅用户资源的许可，然后获取访问令牌。
- 使用访问令牌为资源[创建订阅](https://developer.microsoft.com/en-us/graph/docs/api-reference/beta/api/subscription_post_subscriptions)。
- 回发验证令牌以确认通知 URL。
- 收听来自 Microsoft Graph 的通知并使用 202 状态代码进行响应。
- 请求与使用通知中的数据更改的资源相关的更多信息。

应用通过仅限应用的身份验证令牌创建订阅之后，当用户数据中发生事件时，Microsoft Graph 将向注册的通知终结点发送通知。应用随后会对事件作出回应。

此示例会订阅`用户`订阅来了解`已更新`和`已删除`更改。该示例假定通知 URL 是 [Azure 函数](https://docs.microsoft.com/en-us/azure/azure-functions/functions-overview)，它会侦听 http 上的 Webhook 并立即将这些通知添加到 Azure 存储队列中。该示例使用 [Azure WebJobs SDK](https://docs.microsoft.com/en-us/azure/app-service-web/websites-dotnet-webjobs-sdk) 以绑定到 Azure 存储队列，并在 Azure 函数对新通知进行排队时接收这些通知。

## 先决条件

要通过 WebJobs SDK 使用 Microsoft Graph Webhook 示例，需执行以下操作：

* 在开发计算机上安装 Visual Studio 2017。 

* 一个[工作或学校帐户](http://dev.office.com/devprogram)。

* [在 Azure 门户中注册](#register-the-app)的应用程序的 ID 和密钥。

* 用于接收和发送 HTTP 请求的公共 HTTPS 终结点。你可在 Microsoft Azure 或其他服务上托管此项。该示例已使用 [Azure 函数](https://docs.microsoft.com/en-us/azure/azure-functions/functions-overview)进行了测试

* [Azure WebJobs SDK](https://docs.microsoft.com/en-us/azure/app-service-web/websites-dotnet-webjobs-sdk) 将使用的 Azure 存储帐户 

### 创建应用

#### 选择要在其中创建应用的租户

1. 使用工作/学校帐户登录 [Azure 门户](https://portal.azure.com)。
1. 如果你的帐户存在于多个 Azure AD 租户中：
   1. 请从页面右上角的菜单中选择你的个人资料，然后选择“**切换目录**”。
   1. 将会话更改为要在其中创建应用程序的 Azure AD 租户。

#### 注册应用

1. 导航到 [Azure 门户 >“应用注册”](https://go.microsoft.com/fwlink/?linkid=2083908)以注册应用。
![应用程序注册](readme-images/aad-app-registration.PNG)
1. 选择“**新注册**”。
![应用程序注册](readme-images/aad-new-registration.PNG)
1. 出现“**注册应用程序”页面**后，输入应用的注册信息：
   1. 在“**名称**”部分输入一个有意义的名称，该名称将显示给应用用户。例如：`MyWebApp`
   1. 在“**支持的帐户类型**”部分，选择“**任何组织目录中的帐户和个人 Microsoft 帐户(例如 Skype、Xbox、Outlook.com)**”。
1. 选择“**注册**”以创建应用。
![注册应用](readme-images/aad-register-an-app.png)
1. 在应用的“**概述**”页上，查找“**应用程序(客户端) ID**”值，记下它供稍后使用。你将需要此值来为此项目配置 Visual Studio 配置文件。
![应用程序 ID](readme-images/aad-application-id.PNG)
1. 在应用的页面列表中，选择“**身份验证**”。
   1. 在“**重定向 URI**”部分中，选择组合框中的“ **Web**”，然后输入以下重定向 URI：
       - `https://mysigninurl`
	   ![重定向 URI](readme-images/aad-redirect-uri.PNG)
1. 选择“**保存**”。
1. 在“**证书和密钥**”页面的“**客户端密码**”部分中，选择“**新建客户端密码**”。
   1. 键入密钥说明（例如`应用实例`）。
   1. 选择密钥持续时间：“**1 年内**”、“**2 年内**”或“**永不过期**”。
   ![客户端密码](readme-images/aad-new-client-secret.png)
   1. 单击“**添加**”按钮时，将显示密钥值。复制密钥值并将其存储在安全的位置。
   ![客户端密码](readme-images/aad-copy-client-secret.png)
   稍后需要此密钥来配置 Visual Studio 中的项目。此密钥值将不再显示，也不可用其他任何方式进行检索，因此请在 Azure 门户中看到此值时立即进行记录。
      

1. 在应用的页面列表中，选择“**API 权限**”。
   1. 单击“**添加权限**”按钮，然后确保选中“**Microsoft API**”选项卡。
   1. 在“**常用 Microsoft API**”部分，选择“**Microsoft Graph**”。
   1. 在“**应用程序权限**”部分，确保已勾选 **Directory.Read.All**。必要时请使用搜索框。
   1. 选择“**添加权限**”按钮。

1. 在“**管理**”页面中，选择“**API 权限**”>“**添加权限**”。

    ![“选择 API 权限”屏幕截图](readme-images/aad-api-permissions.PNG)

1. 选择“**Microsoft API**”>“**Microsoft Graph**”。

    ![“请求 API 权限”屏幕截图](readme-images/aad-request-api-permissions.PNG)

1. 选择“**应用程序权限**”。在搜索框中，键入 **directory.read.all**，然后从列表中选择第一个选项。选择“**添加权限**”。

    ![委派的权限屏幕截图](readme-images/aad-application-permissions.PNG)


## 设置 Azure 函数
你必须公开一个公共 HTTPS 终结点才能创建订阅和接收来自 Microsoft Graph 的通知。可使用 Azure Functions 实现这一目的。

1. 使用工作/学校帐户登录 [Azure 门户](https://portal.azure.com/)。

2. 在左侧导航窗格中选择“**函数应用**”。

3. 按照说明新建一个**函数应用**

4. 使用以下示例代码新建一个 **WebHook/API** 函数

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

5. 选择“**集成**”>“**新输出**”>“**Azure 查询存储**” 

6. 输入 `queue` 作为 **消息参数名称**

7. 输入 `webhooksnotificationqueue` 作为**队列名称**

8. 使用现有**存储帐户连接**或新建一个

9. 选择所创建的函数，再从门户中**运行**以确保操作成功。

10. 选择**获取函数 URL**，复制要在示例中使用的通知 URL。

## 设置示例项目

1. 在解决方案资源管理器中，选择 **App.config** 项目。

	a.对于 **clientId** 键，请将 *ENTER_YOUR_APP_ID* 替换为已注册的 Azure 应用的应用程序 ID。
	
	b.对于 **clientSecret** 键，请将 *ENTER_YOUR_SECRET* 替换为已注册的 Azure 应用程序的密钥。  
	
	c.对于 **tenantId** 键，请将 *ENTER_YOUR_ORGANIZATION_ID* 替换为你的组织的 ID。

	d.对于 **webjobs** 键，请将 *ENTER_YOUR_AZURE_STORAGE_CONNECTION_STRING* 替换为 Azure 函数中集成的 Azure 存储的连接字符串。
	
	e.对于 **notificationurl** 键，请将 *ENTER_YOUR_NOTIFICATION_URL* 替换为 Azure 函数的 URL。

## 使用示例应用
1. 按 **F5** 启动示例。

2. 等待示例打印“**已使用以下 ID 创建新的订阅:**”消息

3. 更新组织中所有用户的所有属性。示例：更新其电话号码

4. 几分钟后，示例应会收到有关更新的用户及其标识的通知。

5. 示例每 30 秒续订订阅一次。可将已更新操作的时间段更新为每 24 小时一次。

## 示例主要组件

**控制器**  
- [`Function.cs`](https://github.com/microsoftgraph/webjobs-webhooks-sample/blob/master/WebHooksSample/Functions.cs)：管理订阅和接收通知。  
- [`Program.cs`](https://github.com/microsoftgraph/webjobs-webhooks-sample/blob/master/WebHooksSample/SubscriptionController.cs) 启动 Azure WebJob 主机。
 
## 疑难解答

| 问题 | 解决方案 |
|:------|:------|
| 你在尝试创建订阅时获得 403 禁用响应。 | 请确保你的应用注册包含对 Microsoft Graph 的**读取目录数据**应用程序权限（如[注册应用](#register-the-app)部分中所述）。 |  
| 你没有收到通知。 | 请在[门户](https://portal.azure.com/)中查看 Azure 函数的日志。如果 Microsoft Graph 没有发送通知，请打开标有 *[MicrosoftGraph]* 的 [Stack Overflow](https://stackoverflow.com/questions/tagged/MicrosoftGraph) 问题。包含订阅 ID 及其创建时间。<br /><br /> |  
| 你收到*订阅验证请求超时*响应。 | 此表示 Microsoft Graph 未在预期时间范围内（约 10 秒）收到验证响应。<br /><br />如果你在**消耗计划**中创建了 Azure 函数，则该函数可能会因为不活动而进入睡眠状态。示例将再次尝试，在后面的尝试中应该会成功。或者，请尝试通过应用服务计划创建 Azure 函数。 |  
| 你在安装包时出错。 | 请确保你放置解决方案的本地路径不太长/太深。要解决此问题，可将解决方案移到更接近根驱动器的位置。 |

<a name="contributing"></a>
## 参与 ##

如果想要参与本示例，请参阅 [CONTRIBUTING.MD](/CONTRIBUTING.md)。

此项目已采用 [Microsoft 开放源代码行为准则](https://opensource.microsoft.com/codeofconduct/)。有关详细信息，请参阅[行为准则 FAQ](https://opensource.microsoft.com/codeofconduct/faq/)。如有其他任何问题或意见，也可联系 [opencode@microsoft.com](mailto:opencode@microsoft.com)。

## 问题和意见

我们乐于收到反馈，了解使用 WebJobs SDK 的 Microsoft Graph Webhook 示例的情况。你可通过该存储库中的[问题](https://github.com/microsoftgraph/webjobs-webhooks-sample/issues)部分向我们发送问题和建议。

与 Microsoft Graph 相关的一般问题应发布到 [Stack Overflow](https://stackoverflow.com/questions/tagged/MicrosoftGraph)。请确保你的问题或意见标记有 *[MicrosoftGraph]*。

如果有功能建议，请将你的想法发布在我们的 [User Voice](https://officespdev.uservoice.com/) 页上，并为你的建议进行投票。

## 其他资源

* [Microsoft Graph Node.js Webhook 示例](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample)
* [在 Microsoft Graph 中使用 Webhooks](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks)
* [订阅资源](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/subscription)
* [Microsoft Graph 开发人员网站](https://developer.microsoft.com/en-us/graph/)
* [在 ASP.NET MVC 应用中调用 Microsoft Graph](https://developer.microsoft.com/en-us/graph/docs/platform/aspnetmvc)

版权所有 (c) 2019 Microsoft Corporation。保留所有权利。
