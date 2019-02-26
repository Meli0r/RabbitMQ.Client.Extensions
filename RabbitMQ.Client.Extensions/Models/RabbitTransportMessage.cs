using System.Collections.Generic;

namespace RabbitMQ.Client.Extensions.Models
{
    public class RabbitTransportMessage
    {
        public IDictionary<string, object> Headers { get; set; }
        public byte[] BodyBytes { get; set; }
    }
}
