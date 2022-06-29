using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Api.Model;
using Api.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using RabbitMQ.Client;

public class StooqServiceTests
{
    [SetUp]
    public void Setup()
    {
        Environment.SetEnvironmentVariable("STOCK_URI", "http://#code#/");
        Environment.SetEnvironmentVariable("RABBIT_QUEUE", "q");
    }

    [Test]
    [Ignore("HttpClient doen't implement a interface to receive :(")]
    public void Receive()
    {
        var code = Encoding.UTF8.GetBytes("aapl.us");
        var queue = Substitute.For<IModel>();
        queue.When(q => q.BasicConsume("q", true, Arg.Any<IBasicConsumer>()))
            .Do(q => q.ArgAt<IBasicConsumer>(2).HandleBasicDeliver(
                string.Empty, 0, false, string.Empty, string.Empty, Substitute.For<IBasicProperties>(), new ReadOnlyMemory<byte>(code)));

        var content = Encoding.UTF8.GetBytes("Symbol,Date,Time,Open,High,Low,Close,Volume\nAAPL.US,2022-06-28,22:00:10,142.13,143.422,137.325,137.44,66935132");
        var httpClient = Substitute.For<HttpClient>();
        httpClient.GetByteArrayAsync("http://aapl.us/").Returns(Task.FromResult(content));

        var messageService = Substitute.For<IMessageService>();

        var stooqService = new StooqService(Substitute.For<ILogger<StooqService>>(), messageService, queue, httpClient);

        messageService.Received().Broadcast(
            Arg.Is<MessageOut>(message => message.User == "(QSECOFR)" && message.Text == "{AAPL.US quote is 142.13 per share"));
    }
}
