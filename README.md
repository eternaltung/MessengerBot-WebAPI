# Facebook Messenger Bot WebAPI Template

This project is the template to build the Facebook Messenger Bot with  ASP.NET Web API.

## Steps to run

- Open the project and add your Facebook Page Token & App Secret at the Controllers/WebhookController.cs.

```csharp
string pageToken = "page token";
string appSecret = "app secret";
```

- In the Facebook webhook setting page. Verify token is the value of the key "hub.verify_token".  (this sample is hello)

```csharp
if (querystrings["hub.verify_token"] == "hello")
```

![screenshot](screenshot/img1.png)
