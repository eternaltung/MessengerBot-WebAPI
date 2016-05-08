using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using MessengerBot.Models;

namespace MessengerBot.Controllers
{
    public class WebhookController : ApiController
    {
        string pageToken = "your fb page token";

        public HttpResponseMessage Get()
        {
            var querystrings = Request.GetQueryNameValuePairs().ToDictionary(x => x.Key, x => x.Value);
            if (querystrings["hub.verify_token"] == "hello")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(querystrings["hub.challenge"], Encoding.UTF8, "text/plain")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        [HttpPost]
        public async Task Post([FromBody]WebhookModel value)
        {
            foreach (var item in value.entry[0].messaging)
            {
                if (item.message == null)
                    break;
                else
                    await SendMessage(GetMessageTemplate(item.message.text, item.sender.id));
            }
        }

        /// <summary>
        /// get text message template
        /// </summary>
        /// <param name="text">text</param>
        /// <param name="sender">sender id</param>
        /// <returns>json</returns>
        private string GetMessageTemplate(string text, string sender)
        {
            return $@"
{{
recipient: {{id: ""{sender}"" }},
message: {{ text: ""{text}"" }}
}}";
        }

        /// <summary>
        /// send message
        /// </summary>
        /// <param name="json">json</param>
        private async Task SendMessage(string json)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage res = await client.PostAsync($"https://graph.facebook.com/v2.6/me/messages?access_token={pageToken}", new StringContent(json, Encoding.UTF8, "application/json"));
            }
        }
    }
}

