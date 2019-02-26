using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Extensions.Configuration;
using RabbitMQ.Client.Extensions.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQ.Client.Extensions.Infrastructure
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMqConnection(this IServiceCollection services, RabbitConnection rabbitConnection)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (rabbitConnection == null || rabbitConnection.ConnectionName == string.Empty) throw new ArgumentNullException(nameof(rabbitConnection));

            services.AddLogging();
            services.AddOptions();
            services.Configure<RabbitConnection>(opts => {
                opts.AutomaticRecoveryEnabled = rabbitConnection.AutomaticRecoveryEnabled;
                opts.ClientProperties = rabbitConnection.ClientProperties;
                opts.ConnectionName = rabbitConnection.ConnectionName;
                opts.DispatchConsumersAsync = rabbitConnection.DispatchConsumersAsync;
                opts.HostName = rabbitConnection.HostName;
                opts.NetworkRecoveryInterval = rabbitConnection.NetworkRecoveryInterval;
                opts.Password = rabbitConnection.Password;
                opts.Port = rabbitConnection.Port;
                opts.PrefetchCount = rabbitConnection.PrefetchCount;
                opts.RequestedHeartbeat = rabbitConnection.RequestedHeartbeat;
                opts.TopologyRecoveryEnabled = rabbitConnection.TopologyRecoveryEnabled;
                opts.UserName = rabbitConnection.UserName;
                opts.VirtualHost = rabbitConnection.VirtualHost;
            });
            services.AddSingleton<IRabbitConnectionManager, RabbitConnectionManager>();
            return services;
        }
    }
}
