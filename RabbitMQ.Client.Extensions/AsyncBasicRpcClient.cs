using System;
using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client.Extensions.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Extensions.Configuration;
using RabbitMQ.Client.Extensions.Infrastructure;
using RabbitMQ.Client.Extensions.Models;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks.Dataflow;

namespace RabbitMQ.Client.Extensions
{
    public interface IRpcClient: IDisposable
    {
        Task<byte[]> Call(RabbitTransportMessage message);
    }

    public class AsyncRpcClient : IRpcClient
    {
        private readonly int _threadCount;
        private readonly IRabbitConnectionManager _rabbitConnectionManager;
        private readonly RabbitQueue _requestQueue;
        private readonly RabbitQueue _replyQueue = RabbitQueue.GetDirectReplyToQueue();
        private ConcurrentDictionary<IBasicProperties, IModel> _channelsInUse = new ConcurrentDictionary<IBasicProperties,IModel>();
        private readonly BufferBlock<byte[]> _respQueue = new BufferBlock<byte[]>();

        public AsyncRpcClient(IOptions<RpcClientConfiguration> rpcClientConfiguration, IRabbitConnectionManager rabbitConnectionManager, ILogger<AsyncRpcClient> logger)
        {
            _requestQueue = rpcClientConfiguration.Value.RequestQueue;
            _threadCount = rpcClientConfiguration.Value.ThreadCount;
            _rabbitConnectionManager = rabbitConnectionManager;

            for (int i = 0; i < _threadCount; i++)
            {
                var channel = _rabbitConnectionManager.GetChannel();
                var props = channel.CreateBasicProperties();
                var correlationId = Guid.NewGuid().ToString();
                props.CorrelationId = correlationId;
                props.ReplyTo = _replyQueue.QueueName;
                _channelsInUse.TryAdd(props, channel);
                var consumer = new AsyncEventingBasicConsumer(channel);
                

                consumer.Received += async (model, ea) =>
                {
                    var response = ea.Body;
                    if (_channelsInUse.Keys.Select(x => x.CorrelationId).Contains(ea.BasicProperties.CorrelationId))
                    {
                        await _respQueue.SendAsync(response);
                    }
                };

                channel.BasicConsume(_replyQueue.QueueName, true, consumer);
            }
        }

        public async Task<byte[]> Call(RabbitTransportMessage message)
        {
            
                var enumerator = _channelsInUse.GetEnumerator();
                var res = enumerator.MoveNext();

                var props = enumerator.Current.Key;
                var channel = enumerator.Current.Value;

            channel.BasicPublish(
                exchange: RabbitExchange.GetDefault().ExchangeName,
                routingKey: _requestQueue.QueueName,
                basicProperties: props,
                body: message.BodyBytes);

            return await _respQueue.ReceiveAsync();
        }

        public void Dispose()
        {
            foreach (var item in _channelsInUse)
            {
                item.Value.Close();
                item.Value.Dispose();
            }
        }
    }
}