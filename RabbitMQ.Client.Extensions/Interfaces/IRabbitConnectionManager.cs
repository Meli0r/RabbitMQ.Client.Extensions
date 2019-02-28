namespace RabbitMQ.Client.Extensions.Interfaces
{
    public interface IRabbitConnectionManager
    {
        IConnection Connection { get; }
        IModel GetChannel();
    }
}