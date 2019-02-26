using RabbitMQ.Client;
using RabbitMQ.Client.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQ.Client.Extensions.Infrastructure
{
    public static class IModelExtensions
    {
        public static void ExchangeDeclare(this IModel model, RabbitExchange exchange)
        {
            model.ExchangeDeclare(
                exchange: exchange.ExchangeName, 
                type: exchange.Type, 
                durable: exchange.Durable, 
                autoDelete: exchange.AutoDelete, 
                arguments: exchange.Arguments);
        }

        public static QueueDeclareOk QueueDeclare(this IModel model, RabbitQueue queue)
        {
            return model.QueueDeclare(
                queue: queue.QueueName,
                durable: queue.Durable,
                exclusive: queue.Exclusive,
                autoDelete: queue.AutoDelete,
                arguments: queue.Arguments);
        }
    }
}
