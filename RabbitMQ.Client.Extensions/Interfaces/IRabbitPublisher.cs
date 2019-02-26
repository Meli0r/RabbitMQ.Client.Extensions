using RabbitMQ.Client.Extensions.Configuration;
using RabbitMQ.Client.Extensions.Models;
using System;

namespace RabbitMQ.Client.Extensions.Interfaces
{
    public interface IRabbitPublisher
    {
        void Send(RabbitExchange rabbitExchange, RabbitTransportMessage message);
        void Send(RabbitQueue rabbitQueue, RabbitTransportMessage message);
    }
}