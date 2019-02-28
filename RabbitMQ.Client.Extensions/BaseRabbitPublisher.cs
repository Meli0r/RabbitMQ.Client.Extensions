using RabbitMQ.Client.Extensions.Configuration;
using RabbitMQ.Client.Extensions.Infrastructure;
using RabbitMQ.Client.Extensions.Interfaces;
using RabbitMQ.Client.Extensions.Models;
using RabbitMQ.Client;
using System.Linq;

namespace RabbitMQ.Client.Extensions
{
    public class BaseRabbitPublisher : IRabbitPublisher
    {
        private IRabbitConnectionManager _rabbitChannelManager;

        public BaseRabbitPublisher(IRabbitConnectionManager rabbitChannelManager)
        {
            _rabbitChannelManager = rabbitChannelManager;
        }

        public void Send(RabbitQueue rabbitQueue, RabbitTransportMessage message)
        {
            using (var channel = _rabbitChannelManager.GetChannel())
            {
                channel.QueueDeclare(rabbitQueue);
                var props = channel.CreateBasicProperties();
                if (message.Headers != null && message.Headers.Count > 0)
                {
                    props.Headers = message.Headers;
                }
                channel.BasicPublish("", rabbitQueue.QueueName, props, message.BodyBytes);
            } 
        }

        public void Send(RabbitExchange rabbitExchange, RabbitTransportMessage message)
        {
            using (var channel = _rabbitChannelManager.GetChannel())
            {
                channel.ExchangeDeclare(rabbitExchange);
                var props = channel.CreateBasicProperties();
                if (message.Headers != null && message.Headers.Count > 0)
                {
                    props.Headers = message.Headers.ToDictionary(h => h.Key, h => (object)h.Value);
                }
                channel.BasicPublish(rabbitExchange.ExchangeName, "", props, message.BodyBytes);
            }
        }
    }
}
