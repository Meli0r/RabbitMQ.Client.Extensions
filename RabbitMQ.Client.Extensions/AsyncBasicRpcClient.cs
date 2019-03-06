using System;
using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client.Extensions.Interfaces;
using RabbitMQ.Client.Events;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Extensions.Configuration;
using RabbitMQ.Client.Extensions.Models;
using Microsoft.Extensions.Options;
using System.Threading;

namespace RabbitMQ.Client.Extensions
{
    public interface IRpcClient: IDisposable
    {
        Task<byte[]> CallAsync(RabbitTransportMessage message);
    }

    public class AsyncRpcClient : IRpcClient
    {
        private readonly ILogger _logger;
        private readonly IRabbitConnectionManager _rabbitConnectionManager;
        private readonly RabbitQueue _requestQueue;
        private readonly RabbitQueue _replyQueue = RabbitQueue.GetDirectReplyToQueue();
        private int? _threadCount;
        private TimeSpan _timeout;
        private ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _pendingMessages;
        private IModel _channel;
        private AsyncEventingBasicConsumer _consumer;

        public AsyncRpcClient(IOptions<RpcClientConfiguration> rpcClientConfiguration, IRabbitConnectionManager rabbitConnectionManager, ILogger<AsyncRpcClient> logger)
        {
            _logger = logger;
            _requestQueue = rpcClientConfiguration.Value.RequestQueue;
            _threadCount = rpcClientConfiguration.Value.ThreadCount;
            _timeout = rpcClientConfiguration.Value.Timeout ?? Timeout.InfiniteTimeSpan;
            _rabbitConnectionManager = rabbitConnectionManager;
            _pendingMessages = new ConcurrentDictionary<string, TaskCompletionSource<byte[]>>();
            _channel = rabbitConnectionManager.GetChannel();
            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.Received += async (model, ea) => {
                _pendingMessages.TryRemove(ea.BasicProperties.CorrelationId, out var tcs);
                if (tcs != null)
                {
                    tcs.SetResult(ea.Body);
                }
                else _logger.LogWarning("No result on {CorrelationId}", ea.BasicProperties.CorrelationId);
            };
            _channel.BasicQos(0,(ushort?)_threadCount ?? 1,false);
            _channel.BasicConsume(_replyQueue.QueueName,true,_consumer);
        }

        public async Task<byte[]> CallAsync(RabbitTransportMessage message)
        {
            var tcs = new TaskCompletionSource<byte[]>();
            var correlationId = Guid.NewGuid().ToString();
            var cts = new CancellationTokenSource(_timeout);
            cts.Token.Register(() => tcs.TrySetException(new TimeoutException(string.Format(
                "Request timeout on {0}. {1} response not recieved in {2} ms!", correlationId, Encoding.UTF8.GetString(message.BodyBytes), _timeout.Milliseconds.ToString()
                ))), false);
            _pendingMessages.TryAdd(correlationId, tcs);

            var props = _channel.CreateBasicProperties();
            props.ReplyTo = _replyQueue.QueueName;
            props.CorrelationId = correlationId;

            _channel.BasicPublish(
                exchange: "",
                routingKey: _requestQueue.QueueName,
                basicProperties: props,
                body: message.BodyBytes);

            return await tcs.Task;

        }

        public void Dispose()
        {
            _channel.Close();
            _channel.Dispose();
        }
    }
}