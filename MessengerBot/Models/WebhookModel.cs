using System.Collections.Generic;
using Newtonsoft.Json;

namespace MessengerBot.Models
{
    public class WebhookModel
    {
        [JsonProperty("object")]
        public string _object { get; set; }
        public List<Entry> entry { get; set; }
    }

    public class Entry
    {
        public string id { get; set; }
        public long time { get; set; }
        public List<Messaging> messaging { get; set; }
    }

    public class Messaging
    {
        public Sender sender { get; set; }
        public Recipient recipient { get; set; }
        public long timestamp { get; set; }
        public Message message { get; set; }
        public Postback postback { get; set; }
    }

    public class Postback
    {
        public string payload { get; set; }
    }

    public class Sender
    {
        public string id { get; set; }
    }

    public class Recipient
    {
        public string id { get; set; }
    }

    public class Message
    {
        public string mid { get; set; }
        public int seq { get; set; }
        public string text { get; set; }
    }
}