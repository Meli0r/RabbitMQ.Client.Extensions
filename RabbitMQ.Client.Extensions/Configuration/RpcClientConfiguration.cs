using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQ.Client.Extensions.Configuration
{
    public class RpcClientConfiguration
    {
        public RabbitQueue RequestQueue { get; set; }
        public int? ThreadCount { get; set; }
        public TimeSpan? Timeout { get; set; }
    }
}
