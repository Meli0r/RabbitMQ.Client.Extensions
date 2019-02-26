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

namespace RabbitMQ.Client.Extensions
{
    public interface IRpcClient
    {
        byte[] Call(RabbitTransportMessage message);
    }

    public class AsyncRpcClient : AsyncBasicConsumer, IRpcClient
    {
        private readonly RabbitQueue _requestQueue;
        private readonly RabbitQueue _replyQueue = RabbitQueue.GetDirectReplyToQueue();
        private readonly BlockingCollection<byte[]> respQueue = new BlockingCollection<byte[]>();
        private IBasicProperties _props;

        public AsyncRpcClient(RabbitQueue rpcRabbitQueue, IRabbitConnectionManager rabbitChannelManager, ILogger<AsyncRpcClient> logger) : base(rabbitChannelManager, logger)
        {
            _requestQueue = rpcRabbitQueue;
            StartConsuming(_replyQueue, true);
        }

        public override sealed async Task AsyncDataHandler(object sender, BasicDeliverEventArgs @event)
        {
            if (@event.BasicProperties.CorrelationId == _props.CorrelationId)
            {
                respQueue.Add(@event.Body);
            }
        }

        public byte[] Call(RabbitTransportMessage message)
        {
            _props = _channel.CreateBasicProperties();
            if (message.Headers != null || message.Headers.Count > 0) _props.Headers = message.Headers;
            var correlationId = Guid.NewGuid().ToString();
            _props.CorrelationId = correlationId;
            _props.ReplyTo = _replyQueue.QueueName;

            _channel.BasicPublish(
                exchange: RabbitExchange.GetDefault().ExchangeName,
                routingKey: _requestQueue.QueueName,
                basicProperties: _props,
                body: message.BodyBytes);
            return respQueue.Take();
        }
    }
}