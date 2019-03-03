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
using System.Collections;
using System.Collections.Generic;

namespace RabbitMQ.Client.Extensions
{
    public interface IRpcClient
    {
        Task<byte[]> Call(RabbitTransportMessage message);
    }

    public class AsyncRpcClient : IRpcClient
    {
        private int _threadCount;
        private readonly IRabbitConnectionManager _rabbitConnectionManager;
        private readonly RabbitQueue _requestQueue;
        private readonly RabbitQueue _replyQueue = RabbitQueue.GetDirectReplyToQueue();
        private readonly BufferBlock<byte[]> _response;

        public AsyncRpcClient(IOptions<RpcClientConfiguration> rpcClientConfiguration, IRabbitConnectionManager rabbitConnectionManager, ILogger<AsyncRpcClient> logger)
        {
            _requestQueue = rpcClientConfiguration.Value.RequestQueue;
            _threadCount = rpcClientConfiguration.Value.ThreadCount;           
            _rabbitConnectionManager = rabbitConnectionManager;
            _response = new BufferBlock<byte[]>(new DataflowBlockOptions { MaxMessagesPerTask = 1 });
        }

        public async Task<byte[]> Call(RabbitTransportMessage message)
        {
            using (var channel = _rabbitConnectionManager.GetChannel())
            {
                //channel.BasicQos(0, (ushort)_threadCount, false);
        
                var consumer = new AsyncEventingBasicConsumer(channel);

                var props = channel.CreateBasicProperties();
                props.ReplyTo = _replyQueue.QueueName;
                props.CorrelationId = Guid.NewGuid().ToString();
                consumer.Received += async (model, ea) => {
                    var response = ea.Body;
                    if (ea.BasicProperties.CorrelationId == props.CorrelationId)
                    {
                        await _response.SendAsync(response);
                    }
                };

                channel.BasicConsume(_replyQueue.QueueName, true, consumer);
                channel.BasicPublish(
                    exchange: RabbitExchange.GetDefault().ExchangeName,
                    routingKey: _requestQueue.QueueName,
                    basicProperties: props,
                    body: message.BodyBytes);

                return await _response.ReceiveAsync();
            }
            
        }
    }
}