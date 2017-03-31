using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Bot.Messenger;
using System.Web.Http.Controllers;
using Bot.Messenger.Models;

namespace MessengerBot.Controllers
{
    public class WebhookController : ApiController
    {
        string pageToken = "page token";
        string appSecret = "app secret";
        string verifyToken = "hello";

        private MessengerPlatform _Bot { get; set; }

        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);

            /***Credentials are fetched from web.config ApplicationSettings when the CreateInstance
            ----method is called without a credentials parameter or if the parameterless constructor
            ----is used to initialize the MessengerPlatform class. This holds true for all types that inherit from
            ----Bot.Messenger.ApiBase

                _Bot = MessengerPlatform.CreateInstance();
                _Bot = new MessengerPlatform();
            ***/

            _Bot = MessengerPlatform.CreateInstance(
                MessengerPlatform.CreateCredentials(appSecret, pageToken, verifyToken));
        }

        public HttpResponseMessage Get()
        {
            var querystrings = Request.GetQueryNameValuePairs().ToDictionary(x => x.Key, x => x.Value);

            if (_Bot.Authenticator.VerifyToken(querystrings["hub.verify_token"]))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(querystrings["hub.challenge"], Encoding.UTF8, "text/plain")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {
            var body = await Request.Content.ReadAsStringAsync();

            LogInfo("WebHook_Received", new Dictionary<string, string>
            {
                { "Request Body", body }
            });

            if (!_Bot.Authenticator.VerifySignature(Request.Headers.GetValues("X-Hub-Signature").FirstOrDefault(), body))
                return new HttpResponseMessage(HttpStatusCode.BadRequest);

            WebhookModel webhookModel = _Bot.ProcessWebhookRequest(body);

            if (webhookModel._Object != "page")
                return new HttpResponseMessage(HttpStatusCode.OK);

            string quickReplyPayload_IsUserMsg = "WAS_USER_MESSAGE";
            string quickReplyPayload_IsNotUserMsg = "WAS_NOT_USER_MESSAGE";

            foreach (var entry in webhookModel.Entries)
            {
                foreach (var evt in entry.Events)
                {
                    if (evt.EventType == WebhookEventType.PostbackRecievedCallback
                        || evt.EventType == WebhookEventType.MessageReceivedCallback)
                    {
                        await _Bot.SendApi.SendActionAsync(evt.Sender.ID, SenderAction.typing_on);

                        var userProfileRsp = await _Bot.UserProfileApi.GetUserProfileAsync(evt.Sender.ID);

                        if (evt.EventType == WebhookEventType.PostbackRecievedCallback)
                        {
                            await ProcessPostBack(evt.Sender.ID, userProfileRsp?.FirstName, evt.Postback, quickReplyPayload_IsUserMsg, quickReplyPayload_IsNotUserMsg);
                        }
                        if (evt.EventType == WebhookEventType.MessageReceivedCallback)
                        {
                            if (evt.Message.IsQuickReplyPostBack)
                                await ProcessPostBack(evt.Sender.ID, userProfileRsp?.FirstName, evt.Message.QuickReplyPostback, quickReplyPayload_IsUserMsg, quickReplyPayload_IsNotUserMsg);
                            else
                            {
                                await _Bot.SendApi.SendTextAsync(evt.Sender.ID, $"We got your message {userProfileRsp?.FirstName}, to prove it, we'll send it back to you :)");
                                await ResendMessageToUser(evt);
                                await ConfirmIfCorrect(quickReplyPayload_IsUserMsg, quickReplyPayload_IsNotUserMsg, evt);
                            }
                        }
                    }

                    await _Bot.SendApi.SendActionAsync(evt.Sender.ID, SenderAction.typing_off);

                }
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        private async Task ConfirmIfCorrect(string quickReplyPayload_IsUserMsg, string quickReplyPayload_IsNotUserMsg, WebhookEvent evt)
        {
            SendApiResponse sendQuickReplyResponse = await _Bot.SendApi.SendTextAsync(evt.Sender.ID, "Is that you message?", new List<QuickReply>
            {
                new QuickReply
                {
                    ContentType = QuickReplyContentType.text,
                    Title = "Yes",
                    Payload = quickReplyPayload_IsUserMsg
                },
                new QuickReply
                {
                    ContentType = QuickReplyContentType.text,
                    Title = "No",
                    Payload = quickReplyPayload_IsNotUserMsg
                }
            });

            LogSendApiResponse(sendQuickReplyResponse);
        }

        private async Task ResendMessageToUser(WebhookEvent evt)
        {
            SendApiResponse response = new SendApiResponse();

            if (evt.Message.Attachments == null)
            {
                string text = evt.Message?.Text;

                if (string.IsNullOrWhiteSpace(text))
                    text = "Hello :)";

                response = await _Bot.SendApi.SendTextAsync(evt.Sender.ID, $"Your Message => {text}");
            }
            else
            {
                foreach (var attachment in evt.Message.Attachments)
                {
                    if (attachment.Type != AttachmentType.fallback && attachment.Type != AttachmentType.location)
                    {
                        response = await _Bot.SendApi.SendAttachmentAsync(evt.Sender.ID, attachment);
                    }
                }
            }

            LogSendApiResponse(response);
        }

        private async Task ProcessPostBack(string userId, string username, Postback postback, string quickReplyPayload_IsUserMsg, string quickReplyPayload_IsNotUserMsg)
        {
            if (postback.Payload == quickReplyPayload_IsNotUserMsg)
                await _Bot.SendApi.SendTextAsync(userId, $"Sorry about that {username}, try sending something else.");
            else if (postback.Payload == quickReplyPayload_IsUserMsg)
                await _Bot.SendApi.SendTextAsync(userId, $"Yay! We got it.");
        }

        private static void LogSendApiResponse(SendApiResponse response)
        {
            LogInfo("SendApi Web Request", new Dictionary<string, string>
            {
                { "Response", response?.ToString() }
            });
        }

        private static void LogInfo(string eventName, Dictionary<string, string> telemetryProperties)
        {
            //Log telemetry in DB or Application Insights
        }
    }
}

