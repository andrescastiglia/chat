using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Api.Model;
using Api.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace api_ut;

public class MessageServiceExtended : MessageService
{
    public MessageServiceExtended(ISecurityService securityService)
        : base(Substitute.For<ILogger<MessageService>>(), securityService)
    {
    }

    public IDictionary<string, WebSocket> GetSocketCollection()
    {
        return _socketCollection;
    }
}

public class MessageServiceTests
{
    private ISecurityService _securityService;
    private MessageServiceExtended _messageService;

    public MessageServiceTests()
    {
        _securityService = Substitute.For<ISecurityService>();
        _messageService = new MessageServiceExtended(_securityService);
    }

    [SetUp]
    public void Setup()
    {
        _securityService.ClearReceivedCalls();
        _messageService.GetSocketCollection().Clear();
    }

    [Test]
    public void AddSession()
    {
        Assert.Zero(_messageService.GetSocketCollection().Count);

        _messageService.AddSession("1", Substitute.For<WebSocket>());

        Assert.AreEqual(_messageService.GetSocketCollection().Count, 1);
    }

    [Test]
    public void RemoveSession()
    {
        _messageService.AddSession("1", Substitute.For<WebSocket>());

        Assert.AreEqual(_messageService.GetSocketCollection().Count, 1);

        _messageService.RemoveSession("1");

        Assert.Zero(_messageService.GetSocketCollection().Count);

        _securityService.Received().Logoff("1");
    }

    [Test]
    public void SingleSend_Success()
    {
        _messageService.AddSession("x", Substitute.For<WebSocket>());
        _messageService.AddSession("y", Substitute.For<WebSocket>());
        _messageService.AddSession("z", Substitute.For<WebSocket>());

        var webSocket = Substitute.For<WebSocket>();
        webSocket.State.Returns(WebSocketState.Open);

        _messageService.AddSession("1", webSocket);

        var message = new MessageOut("blah", "u");
        _messageService.SingleSend("1", message);

        var raw = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        webSocket.Received().SendAsync(Arg.Is<ArraySegment<byte>>(
            buffer => buffer.ToArray().SequenceEqual(raw)),
            WebSocketMessageType.Text, true, CancellationToken.None);
    }

    [Test]
    public void SingleSend_Fail()
    {
        var webSocket = Substitute.For<WebSocket>();
        webSocket.State.Returns(WebSocketState.Closed);
        _messageService.AddSession("1", webSocket);

        _messageService.SingleSend("1", new MessageOut("blah", "u"));

        Assert.Zero(_messageService.GetSocketCollection().Count);
    }

    [Test]
    public void Broadcast_Success()
    {
        var webSocket1 = Substitute.For<WebSocket>();
        webSocket1.State.Returns(WebSocketState.Open);
        _messageService.AddSession("1", webSocket1);

        var webSocket2 = Substitute.For<WebSocket>();
        webSocket2.State.Returns(WebSocketState.Open);
        _messageService.AddSession("2", webSocket2);

        var message = new MessageOut("blah", "u");
        _messageService.Broadcast(message);

        var raw = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        webSocket1.Received().SendAsync(Arg.Is<ArraySegment<byte>>(
            buffer => buffer.ToArray().SequenceEqual(raw)),
            WebSocketMessageType.Text, true, CancellationToken.None);

        webSocket2.Received().SendAsync(Arg.Is<ArraySegment<byte>>(
            buffer => buffer.ToArray().SequenceEqual(raw)),
            WebSocketMessageType.Text, true, CancellationToken.None);
    }

    [Test]
    public void Broadcast_Partial()
    {
        var webSocket1 = Substitute.For<WebSocket>();
        webSocket1.State.Returns(WebSocketState.Open);
        _messageService.AddSession("1", webSocket1);

        var webSocket2 = Substitute.For<WebSocket>();
        webSocket2.State.Returns(WebSocketState.Closed);
        _messageService.AddSession("2", webSocket2);

        var message = new MessageOut("blah", "u");
        _messageService.Broadcast(message);

        var raw = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        webSocket1.Received().SendAsync(Arg.Is<ArraySegment<byte>>(
            buffer => buffer.ToArray().SequenceEqual(raw)),
            WebSocketMessageType.Text, true, CancellationToken.None);

        webSocket2.DidNotReceive().SendAsync(Arg.Any<ArraySegment<byte>>(),
            Arg.Any<WebSocketMessageType>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());

        Assert.AreEqual(_messageService.GetSocketCollection().Count, 1);
    }
}
