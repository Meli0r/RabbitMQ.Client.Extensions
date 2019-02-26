using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace RabbitMQ.Client.Extensions.Configuration
{
    public class RabbitConnection : ICloneable
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, Required = Required.Always)]
        public string ConnectionName { get; set; } = null;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, NullValueHandling = NullValueHandling.Include)]
        public string HostName { get; set; } = "localhost";

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, NullValueHandling = NullValueHandling.Include)]
        public int Port { get; set; } = 5672;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, NullValueHandling = NullValueHandling.Include)]
        public string VirtualHost { get; set; } = "/";

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, NullValueHandling = NullValueHandling.Include)]
        public string UserName { get; set; } = "guest";

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, NullValueHandling = NullValueHandling.Include)]
        public string Password { get; set; } = "guest";

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, NullValueHandling = NullValueHandling.Include)]
        public ushort RequestedHeartbeat { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, NullValueHandling = NullValueHandling.Include)]
        public int PrefetchCount { get; set; } = 1;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, NullValueHandling = NullValueHandling.Include)]
        public bool DispatchConsumersAsync { get; set; } = true;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, NullValueHandling = NullValueHandling.Include)]
        public bool TopologyRecoveryEnabled { get; set; } = true;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, NullValueHandling = NullValueHandling.Include)]
        public bool AutomaticRecoveryEnabled { get; set; } = true;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate, NullValueHandling = NullValueHandling.Include)]
        public int NetworkRecoveryInterval { get; set; } = 10;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, object> ClientProperties { get; set; }

        public string ConnectionString
        {
            get { return $"amqp://{UserName}:{Password}@{HostName}:{Port.ToString()}{(VirtualHost == "/" ? "/%2f" : VirtualHost)}"; }
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

    }
}
