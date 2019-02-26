using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
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
using System.Threading;

namespace RabbitMQ.Client.Extensions
{
    public abstract class AsyncBasicRpcServer : IHostedService
    {
        protected readonly ILogger _logger;
        private readonly RabbitQueue _requestQueue;
        private readonly IRabbitConnectionManager _rabbitConnectionManager;
        private IModel _channel;

        public AsyncBasicRpcServer(RabbitQueue rpcRabbitQueue, IRabbitConnectionManager  rabbitConnectionManager, ILogger<AsyncBasicRpcServer> logger)
        {
            _requestQueue = rpcRabbitQueue;
            _rabbitConnectionManager = rabbitConnectionManager;
            _logger = logger;
            _channel = _rabbitConnectionManager.Channel;
        }

        public abstract Task<RabbitTransportMessage> GetResponseAsync(byte[] requestBody);

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => RunAsync(), cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => {
                _channel.Close();
                _channel.Dispose();
                _logger.LogInformation("RpcServer stopped. Channel disposed.");
            },cancellationToken);
        }

        private async Task RunAsync()
        {
            _channel.QueueDeclare(_requestQueue);
            _channel.BasicQos(0, 1, false);
            var consumer = new AsyncEventingBasicConsumer(_channel);
            _channel.BasicConsume(_requestQueue.QueueName, autoAck: true, consumer: consumer);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body;
                var props = ea.BasicProperties;
                var replyProps = _channel.CreateBasicProperties();

                var response = await GetResponseAsync(ea.Body);

                if (response.Headers != null && response.Headers.Count > 0)
                {
                    props.Headers = response.Headers.ToDictionary(h => h.Key, h => (object)h.Value);
                }
                replyProps.CorrelationId = props.CorrelationId;

                _channel.BasicPublish(exchange: "", routingKey: props.ReplyTo, basicProperties: replyProps, body: response.BodyBytes);
            };
        }
    }
}
