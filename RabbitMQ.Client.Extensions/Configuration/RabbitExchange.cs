using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQ.Client.Extensions.Configuration
{
    public class RabbitExchange
    {
        public string ExchangeName { get; set; }
        public string Type { get; set; } = "fanout";
        public bool Durable { get; set; } = true;
        public bool AutoDelete { get; set; } = false;
        public IDictionary<string, object> Arguments { get; set; } = null;

        public static RabbitExchange GetDefault()
        {
            return new RabbitExchange
            {
                ExchangeName = ""
            };
        }
    }
}
