---
page_type: sample
description: "Este ejemplo de Azure WebJobs muestra cómo empezar a obtener notificaciones de Microsoft Graph."
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

# Ejemplo de webhooks de Microsoft Graph con el SDK de WebJobs
Suscríbase a [webhooks de Microsoft Graph](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks) para recibir una notificación cuando se produzcan cambios en los datos del usuario, de modo que no tenga que realizar un sondeo de los cambios.

En este ejemplo de Azure WebJobs se muestra cómo empezar a obtener notificaciones de Microsoft Graph. Microsoft Graph ofrece un punto de conexión unificado de API para obtener acceso a datos desde la nube de Microsoft. 

>Este ejemplo usa el punto de conexión de Azure AD para obtener un token de acceso para cuentas profesionales o educativas. El ejemplo usa un permiso solo de la aplicación, pero los permisos delegados también deberían funcionar.

Las tareas comunes que una aplicación realiza con las suscripciones de webhooks son las siguientes:

- Obtener consentimiento para suscribirse a los recursos de los usuarios y después, obtener un token de acceso.
- Usar el token de acceso para [crear una suscripción](https://developer.microsoft.com/en-us/graph/docs/api-reference/beta/api/subscription_post_subscriptions) a un recurso.
- Devolver un token de validación para confirmar la dirección URL de notificación.
- Escuchar las notificaciones de Microsoft Graph y responder con un código de estado 202.
- Solicitar más información sobre los recursos modificados utilizando los datos en la notificación.

Después de que la aplicación cree una suscripción con el token de autenticación solo para la aplicación, Microsoft Graph envía una notificación al extremo de notificaciones registrado cuando se produce un evento en los datos del usuario. Entonces, la aplicación reaccionará al evento.

Este ejemplo está suscrito al recurso de `usuarios` para cambios `actualizados` y `eliminados`. En el ejemplo se presupone que la dirección URL de notificación es una [función de Azure](https://docs.microsoft.com/en-us/azure/azure-functions/functions-overview), que escucha el webhook en http e inmediatamente agrega las notificaciones a una cola de almacenamiento de Azure. El ejemplo usa el [SDK de Azure WebJobs](https://docs.microsoft.com/en-us/azure/app-service-web/websites-dotnet-webjobs-sdk) para enlazar con la cola de almacenamiento de Azure y recibir notificaciones nuevas a medida que se ponen en cola por la función de Azure.

## Requisitos previos

Para usar el ejemplo de Webhooks de Microsoft Graph con el SDK de WebJobs, necesita lo siguiente:

* Visual Studio 2017 instalado en el equipo de desarrollo. 

* Una [cuenta profesional o educativa](http://dev.office.com/devprogram).

* El Id. de la aplicación y la clave que [registró en el Portal de Azure](#register-the-app).

* Un extremo HTTPS público para recibir y enviar solicitudes HTTP. Puede hospedar esto en Microsoft Azure u otro servicio. Este ejemplo se ha probado con [Función de Azure](https://docs.microsoft.com/en-us/azure/azure-functions/functions-overview)

* Una cuenta de almacenamiento de Azure que usará el [SDK de Azure WebJobs](https://docs.microsoft.com/en-us/azure/app-service-web/websites-dotnet-webjobs-sdk) 

### Crear su aplicación

#### Elija el inquilino en el que desea crear su aplicación

1. Inicie sesión en el [Portal de Azure](https://portal.azure.com) con una cuenta profesional o educativa.
1. Si su cuenta se encuentra en más de un inquilino de Azure AD:
   1. Seleccione su perfil en el menú de la esquina superior derecha de la página y después, elija **Cambiar directorio**.
   1. Cambie su sesión al inquilino de Azure AD donde quiera crear su aplicación.

#### Registrar la aplicación

1. Vaya al [Microsoft Azure Portal > Registro de aplicaciones](https://go.microsoft.com/fwlink/?linkid=2083908) para registrar su aplicación.
![Registro de la aplicación](readme-images/aad-app-registration.PNG)
1. Haga clic en **Nuevo registro**.
![Registro de la aplicación](readme-images/aad-new-registration.PNG)
1. Cuando aparezca la página **Registrar una aplicación**, introduzca la información de registro de su aplicación:
   1. En la sección **Nombre**, escriba un nombre significativo que se mostrará a los usuarios de la aplicación. Por ejemplo: `MyWebApp`
   1. En la sección **Tipos de cuentas admitidas**, seleccione **Cuentas en cualquier directorio organizacional y cuentas personales de Microsoft (por ejemplo, Skype, Xbox, Outlook.com)**.
1. Seleccione **Registrar** para crear la aplicación.
![Registrar una aplicación](readme-images/aad-register-an-app.png)
1. En la página **Información general** de la aplicación, busque el valor **Id. de la aplicación (cliente)** y guárdelo para más tarde. Necesitará este valor para configurar el archivo de configuración de Visual Studio para este proyecto.
![Id. de aplicación](readme-images/aad-application-id.PNG)
1. En la lista de páginas de la aplicación, seleccione **Autenticación**.
   1. En la sección **URI de redirección**, seleccione **Web** en el cuadro combinado y escriba los siguientes URI de redirección:
       - `https://mysigninurl`
	   ![URI de redireccionamiento](readme-images/aad-redirect-uri.PNG)
1. Seleccione **Guardar**.
1. En la página **Certificados y secretos**, en la sección **Secretos de cliente**, elija **Nuevo secreto de cliente**.
   1. Escriba una descripción de clave (de la instancia `app secret`).
   1. Seleccione una duración de clave de **En un año**, **En 2 años** o **Nunca expira**.
   ![Secreto de cliente](readme-images/aad-new-client-secret.png)
   1. Cuando haga clic en el botón **Agregar**, se mostrará el valor de clave. Copie el valor de clave y guárdelo en una ubicación segura.
   ![Secreto de cliente](readme-images/aad-copy-client-secret.png)
   Necesitará esta clave más tarde para configurar el proyecto en Visual Studio. Este valor de clave no se volverá a mostrar, ni se podrá recuperar por cualquier otro medio, por lo que deberá registrarlo tan pronto como sea visible desde Microsoft Azure Portal.
      

1. En la lista de páginas de la aplicación, seleccione **Permisos de API**.
   1. Haga clic en el botón **Agregar un permiso** y después, asegúrese de que la pestaña **API de Microsoft** esté seleccionada.
   1. En la sección **API de Microsoft más usadas**, seleccione **Microsoft Graph**.
   1. En la sección **Permisos de aplicación**, asegúrese de que el permiso **Directory.Read.All** está activado. Si es necesario, use el cuadro de búsqueda.
   1. Seleccione el botón **Agregar permisos**.

1. En la página **Administrar**, seleccione **Permisos de API** > **Agregar un permiso**.

    ![Una captura de pantalla de Seleccionar permisos de API](readme-images/aad-api-permissions.PNG)

1. Elija **API de Microsoft** > **Microsoft Graph**.

    ![Una captura de pantalla de Solicitar permisos de API](readme-images/aad-request-api-permissions.PNG)

1. Elija **Permisos de aplicación**. En el cuadro de búsqueda, escriba **directory.read.all** y seleccione la primera opción de la lista. Seleccione **Agregar permisos**.

    ![Una captura de pantalla de permisos delegados](readme-images/aad-application-permissions.PNG)


## Configurar Función de Azure
Debe exponer un extremo HTTPS público para crear una suscripción y recibir notificaciones de Microsoft Graph. Puede usar Azure Functions para lo mismo.

1. Inicie sesión en el [Portal de Azure](https://portal.azure.com/) con una cuenta profesional o educativa.

2. Elija **aplicaciones de función** en el panel de navegación de la parte izquierda.

3. Siga las instrucciones para crear una nueva **aplicación de función**

4. Crear una nueva función **WebHook/API** con el siguiente código de muestra

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

5. Elija **Integrar** > **Nueva salida** > **Almacenamiento de cola de Azure ** 

6. Escriba `queue` como **nombre de parámetro de mensaje**

7. Escriba `webhooksnotificationqueue` como **nombre de cola**

8. Use una **conexión de cuenta de almacenamiento** existente o cree una nueva

9. Elija la función creada y **Ejecutar** desde el portal para asegurarse de que funciona correctamente.

10. Elija **Obtener dirección URL de la función** para copiar la dirección URL de notificación que se usará en el ejemplo.

## Configurar proyecto de ejemplo

1. En el explorador de soluciones, seleccione el proyecto **App.config**

	a. Para la clave **ClientId**, reemplace *ENTER_YOUR_APP_ID* con el Id. de aplicación de la aplicación de Azure registrada.
	
	b. Para la clave **ClientSecret**, reemplace *ENTER_YOUR_SECRET* con la clave de la aplicación de Azure registrada.  
	
	c. Para la clave **tenantId**, reemplace *ENTER_YOUR_ORGANIZATION_ID* con el ID. de su organización.

	d. Para la clave **webjobs**, reemplace *ENTER_YOUR_AZURE_STORAGE_CONNECTION_STRING* por la cadena de conexión de almacenamiento de Azure integrada en la función Azure.
	
	e. Para la clave **notificationurl**, reemplace *ENTER_YOUR_NOTIFICATION_URL* por la dirección URL de la función de Azure.

## Usar la aplicación de ejemplo
1. Para iniciar el ejemplo, presione **F5**.

2. Espere a que la muestra imprima el mensaje **nueva suscripción creada con ID.:**

3. Actualice las propiedades de cualquier usuario de la organización. Ejemplo: Actualizar el número de teléfono

4. En unos minutos, la muestra debería recibir la notificación para el usuario actualizado, así como su identificación.

5. Cada 30 segundos, el ejemplo renovará la suscripción. Se puede cambiar el período de tiempo de la operación actualizada para que sea cada 24 horas.

## Componentes clave del ejemplo

**Controladores**  
- [`Function.cs`](https://github.com/microsoftgraph/webjobs-webhooks-sample/blob/master/WebHooksSample/Functions.cs) administra las suscripciones y recibe las notificaciones.  
- [`Program.cs`](https://github.com/microsoftgraph/webjobs-webhooks-sample/blob/master/WebHooksSample/SubscriptionController.cs) arranca el host de Azure WebJob.
 
## Solución de problemas

| Problema | Resolución |
|:------|:------|
| Recibe una respuesta 403 Prohibido al intentar crear una suscripción. | Asegúrese de que el registro de su aplicación incluya el permiso de la aplicación **Read directory data** para Microsoft Graph (como se describe en la sección [Registrar la aplicación](#register-the-app)). |  
| No recibe notificaciones. | Consulte los registros para la función de Azure en [portal](https://portal.azure.com/). Si Microsoft Graph no está enviando las notificaciones, abra una incidencia con la etiqueta[Stack Overflow](https://stackoverflow.com/questions/tagged/MicrosoftGraph) en *[MicrosoftGraph]*. Incluya el ID. de suscripción y la hora en que se creó.<br /><br /> |  
| Recibe una respuesta de *Se ha agotado el tiempo de espera de la solicitud de validación de suscripción*. | Esto indica que Microsoft Graph no recibió una respuesta de validación en el intervalo de tiempo esperado (10 segundos).<br /><br />Si creó la función de Azure en **plan de consumo**, la función puede entrar en suspensión por inactividad. El ejemplo probará de nuevo y debería tener éxito en los siguientes intentos. Asimismo, pruebe a crear una función de Azure con el plan de App Service. |  
| Recibe errores durante la instalación de los paquetes | Asegúrese de que la ruta de acceso local donde colocó la solución no es demasiado larga o profunda. Para resolver este problema, mueva la solución más cerca de la unidad raíz. |

<a name="contributing"></a>
## Colaboradores ##

Si quiere hacer su aportación a este ejemplo, vea [CONTRIBUTING.MD](/CONTRIBUTING.md).

Este proyecto ha adoptado el [Código de conducta de código abierto de Microsoft](https://opensource.microsoft.com/codeofconduct/). Para obtener más información, vea [Preguntas frecuentes sobre el código de conducta](https://opensource.microsoft.com/codeofconduct/faq/) o póngase en contacto con [opencode@microsoft.com](mailto:opencode@microsoft.com) si tiene otras preguntas o comentarios.

## Preguntas y comentarios

Nos encantaría recibir sus comentarios sobre Webhooks de Microsoft Graph con el SDK de WebJobs. Puede enviarnos sus preguntas y sugerencias a través de la sección [Problemas](https://github.com/microsoftgraph/webjobs-webhooks-sample/issues) de este repositorio.

Las preguntas sobre Microsoft Graph en general deben publicarse en [Stack Overflow](https://stackoverflow.com/questions/tagged/MicrosoftGraph). Asegúrese de que sus preguntas o comentarios estén etiquetados con *[MicrosoftGraph]*.

Si quiere sugerir alguna función, publique su idea en nuestra página de [User Voice](https://officespdev.uservoice.com/) y vote por sus sugerencias.

## Recursos adicionales

* [Ejemplo de webhooks de Microsoft Graph Node.js](https://github.com/microsoftgraph/nodejs-webhooks-rest-sample)
* [Trabajar con webhooks en Microsoft Graph](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/webhooks)
* [Recurso de suscripción](https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/subscription)
* [Sitio para desarrolladores de Microsoft Graph](https://developer.microsoft.com/en-us/graph/)
* [Llamar a Microsoft Graph desde una aplicación de ASP.NET MVC](https://developer.microsoft.com/en-us/graph/docs/platform/aspnetmvc)

Copyright (c) 2019 Microsoft Corporation. Todos los derechos reservados.
