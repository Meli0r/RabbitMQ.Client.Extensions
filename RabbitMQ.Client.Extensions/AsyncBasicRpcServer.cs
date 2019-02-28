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
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace RabbitMQ.Client.Extensions
{
    public abstract class AsyncBasicRpcServer : IHostedService
    {
        protected readonly ILogger _logger;
        private readonly RabbitQueue _requestQueue;
        private readonly IRabbitConnectionManager _rabbitConnectionManager;
        private readonly int _threadCount;
        private ConcurrentDictionary<string, IModel> _channelsInUse;

        public AsyncBasicRpcServer(IOptions<RpcServerConfiguration> rpcServerConfiguration, IRabbitConnectionManager  rabbitConnectionManager, ILogger logger)
        {
            var options = rpcServerConfiguration.Value;
            _requestQueue = options.RequestQueue;
            _threadCount = options.ThreadCount ?? 1;
            _rabbitConnectionManager = rabbitConnectionManager;
            _channelsInUse = new ConcurrentDictionary<string, IModel>();
            _logger = logger;
        }

        public abstract Task<RabbitTransportMessage> GetResponseAsync(byte[] requestBody);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RPC Server started.");
            var tasks = new List<Task>();
            for (int i = 0; i < _threadCount; i++)
            {
                tasks.Add(Task.Factory.StartNew(() => RunAsync(), cancellationToken));
            }
            await Task.WhenAll(tasks);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => {
                foreach (var channel in _channelsInUse)
                {
                    channel.Value.Close();
                    channel.Value.Dispose();
                    _logger.LogInformation(channel.Key + " disposed.");
                }
                _logger.LogInformation("RpcServer stopped.");
            },cancellationToken);
        }

        private async Task RunAsync()
        {
            var channelId = "RpcServerChannel_" + Guid.NewGuid();
            _channelsInUse.TryAdd(channelId, _rabbitConnectionManager.GetChannel());
            IModel channel;
            _channelsInUse.TryGetValue(channelId, out channel);
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
    }
}
