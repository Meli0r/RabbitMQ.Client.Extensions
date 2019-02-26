using RabbitMQ.Client.Extensions.Configuration;
using RabbitMQ.Client.Extensions.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace RabbitMQ.Client.Extensions.Infrastructure
{
    public class RabbitConnectionManager : IRabbitConnectionManager
    {
        private ILogger _logger;
        private RabbitConnection _rabbitConnection;

        public RabbitConnectionManager(IOptions<RabbitConnection> rabbitConnectionOptions, ILogger<RabbitConnectionManager> logger)
        {
            _logger = logger;
            _rabbitConnection = rabbitConnectionOptions.Value;
            _logger.LogInformation("Connection {ConnectionName} is in use.", _rabbitConnection.ConnectionName);
            Connection = new ConnectionFactory()
            {
                HostName = _rabbitConnection.HostName,
                Port = _rabbitConnection.Port,
                VirtualHost = _rabbitConnection.VirtualHost,
                UserName = _rabbitConnection.UserName,
                Password = _rabbitConnection.Password,
                DispatchConsumersAsync = _rabbitConnection.DispatchConsumersAsync,
                TopologyRecoveryEnabled = _rabbitConnection.TopologyRecoveryEnabled,
                RequestedHeartbeat = _rabbitConnection.RequestedHeartbeat,
                NetworkRecoveryInterval = TimeSpan.FromMilliseconds(_rabbitConnection.NetworkRecoveryInterval)
            }.CreateConnection();

            Connection.ConnectionShutdown += (sender, reason) => {
                _logger.LogInformation("Connection {ConnectionName} closed. Reason: {@ShutdownEventArgs}", _rabbitConnection.ConnectionName.ToString(), reason);
            };
        }

        public IConnection Connection { get; private set; }
        
        public IModel Channel
        {
            get { return Connection.CreateModel(); }
        }
    }
}
