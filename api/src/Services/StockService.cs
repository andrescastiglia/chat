using System.Text;
using RabbitMQ.Client;

namespace Api.Services;

public interface IStockService
{
    void Enqueue(string code);
}

public class StockService : IStockService
{
    private IConnectionFactory _factory;
    private string _exchangeName;
    private IStooqService _stooqService;

    public StockService(IConnectionFactory connectionFactory, IStooqService stooqService)
    {
        _factory = connectionFactory;
        _stooqService = stooqService;
        _exchangeName = Environment.GetEnvironmentVariable("RABBIT_EXCHANGE") ?? throw new Exception("RABBIT_EXCHANGE is missing");
    }

    public void Enqueue(string code)
    {
        using var connection = _factory.CreateConnection();
        using var channel = connection.CreateModel();
        var body = Encoding.UTF8.GetBytes(code);

        channel.BasicPublish(_exchangeName, string.Empty, null, body ); 
    }
}