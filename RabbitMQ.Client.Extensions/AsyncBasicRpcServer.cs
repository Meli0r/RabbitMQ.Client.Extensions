using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Extensions.Configuration;
using RabbitMQ.Client.Extensions.Infrastructure;
using RabbitMQ.Client.Extensions.Interfaces;
using RabbitMQ.Client.Extensions.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace RabbitMQ.Client.Extensions
{
    public abstract class AsyncBasicRpcServer : IRpcServer
    {
        protected readonly ILogger _logger;
        private readonly RabbitQueue _requestQueue;
        private readonly IRabbitConnectionManager _rabbitConnectionManager;

        public AsyncBasicRpcServer(RabbitQueue rpcRabbitQueue, IRabbitConnectionManager  rabbitConnectionManager, ILogger<AsyncBasicRpcServer> logger)
        {
            _requestQueue = rpcRabbitQueue;
            _rabbitConnectionManager = rabbitConnectionManager;
            _logger = logger;
        }

        public async Task RunAsync()
        {
            var channel = _rabbitConnectionManager.Channel;
            channel.QueueDeclare(_requestQueue);
            channel.BasicQos(0, 1, false);
            var consumer = new AsyncEventingBasicConsumer(channel);
            channel.BasicConsume(_requestQueue.QueueName, autoAck: true, consumer: consumer);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body;
                var props = ea.BasicProperties;
                var replyProps = channel.CreateBasicProperties();

                var response = await GetResponseAsync(ea.Body);

                if (response.Headers != null && response.Headers.Count > 0)
                {
                    props.Headers = response.Headers.ToDictionary(h => h.Key, h => (object)h.Value);
                }
                replyProps.CorrelationId = props.CorrelationId;

                channel.BasicPublish(exchange: "", routingKey: props.ReplyTo, basicProperties: replyProps, body: response.BodyBytes);
            };
        }

        public abstract Task<RabbitTransportMessage> GetResponseAsync(byte[] requestBody);

    }
}
