using System.Text;
using Api.Services;
using NSubstitute;
using NUnit.Framework;
using RabbitMQ.Client;

namespace api_ut;

public class StockServiceTests
{
    [SetUp]
    public void Setup()
    {
        Environment.SetEnvironmentVariable("RABBIT_EXCHANGE", "e");
    }

    [Test]
    public void Enqueue()
    {
        var connectionFactory = Substitute.For<IConnectionFactory>();
        var connection = Substitute.For<IConnection>();
        var channel = Substitute.For<IModel>();

        connectionFactory.CreateConnection().Returns(connection);
        connection.CreateModel().Returns(channel);

        IStockService stockService = new StockService(connectionFactory, Substitute.For<IStooqService>());

        stockService.Enqueue("aapl.us");

        channel.Received().BasicPublish("e", string.Empty, null,
            Arg.Is<ReadOnlyMemory<byte>>(body => body.ToArray().SequenceEqual(Encoding.UTF8.GetBytes("aapl.us"))));
    }
}
