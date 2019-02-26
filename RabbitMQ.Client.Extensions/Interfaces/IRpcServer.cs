using System.Threading.Tasks;

namespace RabbitMQ.Client.Extensions
{
    public interface IRpcServer
    {
        Task RunAsync();
    }
}
