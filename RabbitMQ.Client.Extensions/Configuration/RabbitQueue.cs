using System.Collections.Generic;

namespace RabbitMQ.Client.Extensions.Configuration
{
    public class RabbitQueue
    {
        public string QueueName { get; set; }
        public bool Durable { get; set; } = false;
        public bool AutoDelete { get; set; } = false;
        public bool Exclusive { get; set; } = true;
        public IDictionary<string, object> Arguments { get; set; } = null;

        public static RabbitQueue GetDirectReplyToQueue()
        {
            return new RabbitQueue
            {
                QueueName = "amq.rabbitmq.reply-to"
            };
        }
    }
}
