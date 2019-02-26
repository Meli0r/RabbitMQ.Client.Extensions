using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Extensions.Configuration;
using RabbitMQ.Client.Extensions.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading.Tasks;
using RabbitMQ.Client.Extensions.Infrastructure;

namespace RabbitMQ.Client.Extensions
{
    public abstract class AsyncBasicConsumer : IRabbitConsumer
    {
        protected IModel _channel;
        protected ILogger _logger;
        protected AsyncEventingBasicConsumer _consumer;

        public AsyncBasicConsumer(IRabbitConnectionManager rabbitConnectionManager, ILogger<AsyncBasicConsumer> logger)
        {
            _channel = rabbitConnectionManager.Channel;
            _logger = logger;
        }

        public void StartConsuming(RabbitQueue rabbitQueue, bool autoAck = false)
        {
            try
            {
                
                _channel.QueueDeclare(rabbitQueue);
                _consumer = new AsyncEventingBasicConsumer(_channel);
                _consumer.Received += AsyncDataHandler;
                _channel.BasicConsume(rabbitQueue.QueueName, autoAck, _consumer);
            }
            catch (Exception exception)
            {
                _logger.LogError("An {@Exception} occured on {QueueName} consuming start", exception, rabbitQueue.QueueName);
            }
        }

        public void StartConsuming(RabbitExchange rabbitExchange, bool autoAck = false)
        {
            try
            {
                _channel.ExchangeDeclare(rabbitExchange);
                _consumer = new AsyncEventingBasicConsumer(_channel);
                _consumer.Received += AsyncDataHandler;
                var queue = new RabbitQueue() { QueueName = rabbitExchange.ExchangeName + "Queue" };
                _channel.QueueDeclare(queue);
                _channel.QueueBind(queue.QueueName, rabbitExchange.ExchangeName, routingKey: "");
                _channel.BasicConsume(queue.QueueName, autoAck, _consumer);
            }
            catch (Exception exception)
            {
                _logger.LogError("An {@Exception} occured on {ExchangeName} consuming start", exception, rabbitExchange.ExchangeName);
            }
        }

        public abstract Task AsyncDataHandler(object sender, BasicDeliverEventArgs @event);

        public void Dispose()
        {
            _channel.Close();
        }
    }
}
