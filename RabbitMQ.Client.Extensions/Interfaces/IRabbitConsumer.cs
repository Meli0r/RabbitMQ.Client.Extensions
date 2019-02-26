using RabbitMQ.Client.Extensions.Configuration;
using System;

namespace RabbitMQ.Client.Extensions.Interfaces
{
    public interface IRabbitConsumer: IDisposable
    {
        void StartConsuming(RabbitQueue rabbitQueue, bool autoAck = false);
        void StartConsuming(RabbitExchange rabbitExchange, bool autoAck = false);
    }
}