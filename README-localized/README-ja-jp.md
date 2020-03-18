---
page_type: sample
description: "この Azure WebJobs サンプルは、Microsoft Graph から通知を取得する方法を示しています。"
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

# WebJobs SDK を使用した Microsoft Graph Webhook のサンプル
[Microsoft Graph webhook](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks) をサブスクライブすると、ユーザーのデータが変更された場合に通知を受け取ることができ、変更内容についてポーリングを行う必要がなくなります。

この Azure WebJobs サンプルでは、Microsoft Graph からの通知の取得を開始する方法を示します。Microsoft Graph は、Microsoft クラウドのデータにアクセスするための統合 API エンドポイントを提供します。 

>このサンプルは Azure AD エンドポイントを使用して、職場または学校のアカウントのアクセス トークンを取得します。このサンプルでは、アプリケーションのみのアクセス許可を使用します。ただし、委任されたアクセス許可も機能します。

アプリケーションが Webhook のサブスクリプションを使用して実行する一般的なタスクを次に示します。

- ユーザーのリソースをサブスクライブするための同意を取得し、アクセス トークンを取得する。
- アクセス トークンを使用して、リソースへの[サブスクリプションを作成](https://developer.microsoft.com/en-us/graph/docs/api-reference/beta/api/subscription_post_subscriptions)する。
- 検証トークンを送り返して通知 URL を確認する。
- Microsoft Graph からの通知をリッスンし、状態コード 202 で応答する。
- 通知内のデータを使用して、変更されたリソースの詳細情報を要求する。

アプリがアプリ専用の認証トークンを使用してサブスクリプションを作成した後、ユーザーのデータでイベントが発生すると、Microsoft Graph は登録済み通知エンドポイントに通知を送信します。これに対して、アプリがイベントに反応します。

このサンプルは、`更新`および`削除`された変更の `Users` リソースをサブスクライブします。サンプルでは、​​通知 URL が [Azure 関数](https://docs.microsoft.com/en-us/azure/azure-functions/functions-overview)であり、Webhook over http をリッスンし、それらの通知を Azure ストレージ キューにすぐに追加すると想定しています。サンプルでは、[Azure WebJobs SDK](https://docs.microsoft.com/en-us/azure/app-service-web/websites-dotnet-webjobs-sdk) を使用して Azure ストレージ キューにバインドし、Azure 関数によってキューに入ったときに新しい通知を受信します。

## 前提条件

WebJobs SDK を使用して Microsoft Graph Webhooks サンプルを使用するには、次のものが必要です。

* 開発用コンピューターにインストールされている Visual Studio 2017。 

* [職場または学校のアカウント](http://dev.office.com/devprogram)。

* [Azure ポータルに登録](#register-the-app)するアプリケーションのアプリケーション ID とキー。

* HTTP 要求を送受信するためのパブリック HTTPS エンドポイント。Microsoft Azure または別のサービスでこれをホストできます。このサンプルは [Azure 関数](https://docs.microsoft.com/en-us/azure/azure-functions/functions-overview)を使用してテストされました。

* [Azure WebJobs SDK](https://docs.microsoft.com/en-us/azure/app-service-web/websites-dotnet-webjobs-sdk) で使用される Azure ストレージ アカウントです。 

### アプリを作成する

#### アプリを作成するテナントを選択する

1. 職場または学校のアカウントを使用して、[Azure ポータル](https://portal.azure.com)にサインインします。
1. 複数の Azure AD テナントにアカウントが存在する場合:
   1. ページの右上隅にあるメニューからプロファイルを選択し、[**ディレクトリの切り替え**] を選択します。
   1. アプリケーションを作成する Azure AD テナントにセッションを変更します。

#### アプリを登録する

1. [[Azure ポータル]、[アプリの登録]](https://go.microsoft.com/fwlink/?linkid=2083908) の順に移動してアプリを登録します。
![アプリケーションの登録](readme-images/aad-app-registration.PNG)
1. [**新規登録**] を選択します。
![アプリケーションの登録](readme-images/aad-new-registration.PNG)
1. [**アプリケーションの登録ページ**] が表示されたら、以下のアプリの登録情報を入力します。
   1. [**名前**] セクションに、アプリのユーザーに表示されるわかりやすい名前を入力します。次に例を示します。`MyWebApp`
   1. [**サポートされているアカウントの種類**] セクションで、[**組織ディレクトリ内のアカウントと個人の Microsoft アカウント (例: Skype、Xbox、Outlook.com)**] を選択します。
1. [**登録**] を選択して、アプリを作成します。
![アプリを登録します](readme-images/aad-register-an-app.png)
1. アプリの [**概要**] ページで、[**Application (client) ID**] (アプリケーション (クライアント) ID) の値を確認し、後で使用するために記録します。この値は、このプロジェクトで Visual Studio 構成ファイルを設定するのに必要になります。
![アプリケーション ID](readme-images/aad-application-id.PNG)
1. アプリのページの一覧から [**認証**] を選択します。
   1. [**リダイレクト URI**] セクションで、コンボ ボックスの [**Web**] を選択し、次のリダイレクト URI を入力します。
       - `https://mysigninurl`
	   ![リダイレクト URI](readme-images/aad-redirect-uri.PNG)
1. [**保存**] を選択します。
1. [**証明書とシークレット**] ページの [**クライアント シークレット**] セクションで、[**新しいクライアント シークレット**]を選択します。
   1. キーの説明を入力します (例: `アプリ シークレット`)。
   1. [**1 年**]、[**2 年**]、または [**有効期限なし**] からキーの期間を選択します。
   ![クライアントの秘密情報](readme-images/aad-new-client-secret.png)
   1. [**追加**] ボタンをクリックすると、キー値が表示されます。キー値をコピーして安全な場所に保存します。
   ![クライアント シークレット](readme-images/aad-copy-client-secret.png)
   Visual Studio でプロジェクトを構成するには、このキーが必要になります。このキー値は二度と表示されず、他の方法で取得することもできませんので、Azure ポータルで表示されたらすぐに記録してください。
      

1. アプリのページの一覧から [**API のアクセス許可**] を選択します。
   1. [**アクセス許可の追加**] ボタンをクリックして、[**Microsoft API**] タブが選択されていることを確認します。
   1. [**一般的に使用される Microsoft API**] セクションで、[**Microsoft Graph**] を選択します。
   1. [**アプリケーションのアクセス許可**] セクションで、**Directory.Read.All** アクセス許可が選択されていることを確認します。必要に応じて検索ボックスを使用します。
   1. [**アクセス許可の追加**] ボタンを選択します。

1. [**管理**] ページで、[**API アクセス許可**]、[**アクセス許可の追加**] の順に選択します。

    ![[API アクセス許可] を選択したスクリーンショット](readme-images/aad-api-permissions.PNG)

1. [**Microsoft API**]、[**Microsoft Graph**] の順に選択します。

    ![[API のアクセス許可] を要求するスクリーンショット](readme-images/aad-request-api-permissions.PNG)

1. [**アプリケーションのアクセス許可**] を選択します。[検索] ボックスに **directory.read.all** を入力し、リストから最初のオプションを選択します。[**アクセス許可の追加**] を選択します。

    ![委任されたアクセス許可のスクリーンショット](readme-images/aad-application-permissions.PNG)


## Azure 関数のセットアップ
サブスクリプションを作成し、Microsoft Graph から通知を受信するには、パブリック HTTPS エンドポイントを公開する必要があります。Azure 関数は、同じく使用できます。

1. 職場または学校のアカウントを使用して、[Azure Portal](https://portal.azure.com/) にサインインします。

2. 左側のナビゲーション ウィンドウにある [**関数アプリ**] を選択します。

3. 指示に従って新しい**関数アプリ**を作成します。

4. 次のサンプル コードを使用して、新しい **WebHook/API** 関数を作成します。

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

5. [**統合**]、[**新規出力**]、[**Azure Queue Storage**] の順に選択します。 

6. [**メッセージ パラメーター名**] として `queue` を入力します。

7. [**キュー名**] として `webhooksnotificationqueue` を入力します。

8. 既存のものを使用するか、新しい [**ストレージ アカウント接続**] を作成します。

9. 作成された関数を選択し、ポータルから [**実行**] して、正常に動作することを確認します。

10. [**関数 URL を取得する**] を選択して、サンプルに使用する通知 URL をコピーします。

## セットアップ サンプル プロジェクト

1. ソリューション エクスプローラーで、**App.config** プロジェクトを選択します。

	a.**clientId** キーは、*ENTER_YOUR_APP_ID* を登録済み Azure アプリケーションのアプリケーション ID で置き換えます。
	
	b.**clientSecret** キーは、*ENTER_YOUR_SECRET* を登録済み Azure アプリケーションのキーで置き換えます。  
	
	c.**tenantId** キーは、*ENTER_YOUR_ORGANIZATION_ID* を組織 ID で置き換えます。

	d.**webjobs** キーは、*ENTER_YOUR_AZURE_STORAGE_CONNECTION_STRING* を Azure関数に統合された Azure ストレージの接続文字列で置き換えます。
	
	e. **notificationurl** キーは、*ENTER_YOUR_NOTIFICATION_URL* を Azure 関数の URL で置き換えます。

## サンプル アプリを使用する
1. **F5** キーを押して、サンプルを開始します。

2. 「**次の ID で新しいサブスクリプションを作成しました**」というメッセージを出力するまでサンプルを待ちます。

3. 組織内のユーザーのプロパティを更新します。例:電話番号を更新する

4. サンプルは数分で、更新されたユーザーとその識別情報の通知を受信する必要があります。

5. サンプルでは、30 秒ごとにサブスクリプションが更新されます。更新された操作の期間を 24 時間ごとに変更できます。

## サンプルの主な構成要素

**コントローラー**  
- [`Function.cs`](https://github.com/microsoftgraph/webjobs-webhooks-sample/blob/master/WebHooksSample/Functions.cs) サブスクリプションを管理し、通知を受信します。  
- [`Program.cs`](https://github.com/microsoftgraph/webjobs-webhooks-sample/blob/master/WebHooksSample/SubscriptionController.cs) Azure WebJob ホストをブートストラップします。
 
## トラブルシューティング

| 問題 | 解決方法 |
|:------|:------|
| サブスクリプションを作成しようとすると、403 禁止の応答が表示されます。 | アプリの登録に、Microsoft Graph の**ディレクトリ データを読み取る**アプリのアクセス許可が含まれていることを確認します ([[アプリの登録](#register-the-app)] セクションで説明されているように)。|  
| 通知を受信できません。 | [ポータル](https://portal.azure.com/)で Azure 関数のログを確認します。Microsoft Graph で通知が送信されていない場合は、[[MicrosoftGraph]](https://stackoverflow.com/questions/tagged/MicrosoftGraph) タグの付いた*スタックオーバーフロー*問題を開いてください。サブスクリプション ID と作成時刻を含めます。<br /><br /> |  
| [*サブスクリプション検証リクエストがタイムアウトしました*] 応答が表示されました。| これは、Microsoft Graph が予想される期間 (約 10 秒) 以内に検証応答を受信しなかったことを示します。<br /><br />[**従量課金プラン**] で Azure 関数を作成した場合、関数は非アクティブのため、スリープ状態になります。サンプルは再び実行され、次の試行で成功するはずです。または、App Service プランを使用して Azure 関数を作成します。|  
| パッケージのインストール中にエラーが発生します。 | ソリューションを保存したローカル パスが長すぎたり深すぎたりしていないかご確認します。この問題は、ドライブのルート近くにソリューションを移動すると解決します。 |

<a name="contributing"></a>
## 投稿 ##

このサンプルに投稿する場合は、[CONTRIBUTING.MD](/CONTRIBUTING.md) を参照してください。

このプロジェクトでは、[Microsoft Open Source Code of Conduct (Microsoft オープン ソース倫理規定)](https://opensource.microsoft.com/codeofconduct/) が採用されています。詳細については、「[Code of Conduct の FAQ (倫理規定の FAQ)](https://opensource.microsoft.com/codeofconduct/faq/)」を参照してください。また、その他の質問やコメントがあれば、[opencode@microsoft.com](mailto:opencode@microsoft.com) までお問い合わせください。

## 質問とコメント

WebJobs SDK を使用して、Microsoft Graph Webhook のサンプルに関するフィードバックをぜひお寄せください。質問や提案は、このリポジトリの「[問題](https://github.com/microsoftgraph/webjobs-webhooks-sample/issues)」セクションで送信できます。

Microsoft Graph 全般の質問については、「[Stack Overflow](https://stackoverflow.com/questions/tagged/MicrosoftGraph)」に投稿してください。質問やコメントには、必ず "*MicrosoftGraph*" とタグを付けてください。

機能に関して提案がございましたら、「[User Voice](https://officespdev.uservoice.com/)」ページでアイデアを投稿してから、その提案に投票してください。

## その他のリソース

* [Microsoft Graph Node.js Webhook のサンプル](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample)
* [Microsoft Graph の Webhook での作業](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks)
* [サブスクリプション リソース](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/subscription)
* [Microsoft Graph 開発者向けサイト](https://developer.microsoft.com/en-us/graph/)
* [ASP.NET MVC アプリで Microsoft Graph を呼び出す](https://developer.microsoft.com/en-us/graph/docs/platform/aspnetmvc)

Copyright (c) 2019 Microsoft Corporation.All rights reserved.
