
using Api.Model;
using Api.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace api_ut;

public class CommandServiceExtended : CommandService
{
    public CommandServiceExtended(IStockService stockService, ISecurityService securityService, IMessageService messageService)
        : base(Substitute.For<ILogger<CommandService>>(), stockService, securityService, messageService)
    {
    }
}

public class CommandServiceTests
{
    private IMessageService _messageService;
    private ISecurityService _securityService;
    private IStockService _stockService;

    private CommandServiceExtended _commandService;

    public CommandServiceTests()
    {
        _stockService = Substitute.For<IStockService>();
        _securityService = Substitute.For<ISecurityService>();
        _messageService = Substitute.For<IMessageService>();

        _commandService = new CommandServiceExtended(_stockService, _securityService, _messageService);
    }

    [SetUp]
    public void Setup()
    {
        _stockService.ClearReceivedCalls();
        _securityService.ClearReceivedCalls();
        _messageService.ClearReceivedCalls();
    }

    [Test]
    public void ExecHelp()
    {
        var request = new Request("1", new MessageIn("1", "/help"));
        _commandService.Execute(request);

        _messageService.Received().SingleSend("1",
            Arg.Is<MessageOut>(message => message.User == "(QSECOFR)" && message.Text == "Commands: /login user:token /logoff /stock xxx")
        );
    }

    [Test]
    public void ExecLogin_Sucess()
    {
        _securityService.Login("1", "user", "token").Returns(x => true);

        var request = new Request("1", new MessageIn("1", "/login user:token"));
        _commandService.Execute(request);

        _messageService.Received().SingleSend("1",
            Arg.Is<MessageOut>(message => message.User == "(QSECOFR)" && message.Text == "/login user:token")
        );
    }

    [Test]
    public void ExecLogin_Fail()
    {
        _securityService.Login("1", "user", "token").Returns(x => false);

        var request = new Request("1", new MessageIn("1", "/login user:token"));
        _commandService.Execute(request);

        _messageService.Received().SingleSend("1",
            Arg.Is<MessageOut>(message => message.User == "(QSECOFR)" && message.Text == "Access denied")
        );
    }

    [Test]
    public void NotAutorized()
    {
        _securityService.User("1").Returns(x => null);

        var request = new Request("1", new MessageIn("1", "foo"));
        _commandService.Execute(request);

        _messageService.Received().SingleSend("1",
            Arg.Is<MessageOut>(message => message.User == "(QSECOFR)" && message.Text == "Access denied")
        );
    }

    [Test]
    public void Logoff()
    {
        _securityService.User("1").Returns(x => "user");

        var request = new Request("1", new MessageIn("1", "/logoff"));
        _commandService.Execute(request);

        _securityService.Received().Logoff("1");

        _messageService.Received().SingleSend("1",
            Arg.Is<MessageOut>(message => message.User == "(QSECOFR)" && message.Text == "/logoff")
        );
    }

    [Test]
    public void Stock()
    {
        _securityService.User("1").Returns(x => "user");

        var request = new Request("1", new MessageIn("1", "/stock aapl.us"));
        _commandService.Execute(request);

        _stockService.Received().Enqueue("aapl.us");

        _messageService.Received().SingleSend("1",
            Arg.Is<MessageOut>(message => message.User == "(QSECOFR)" && message.Text == "Enqueue Stock aapl.us")
        );
    }

    [Test]
    public void Broadcast()
    {
        _securityService.User("1").Returns(x => "user");

        var request = new Request("1", new MessageIn("1", "blah blah blah"));
        _commandService.Execute(request);

        _messageService.Received().Broadcast(
            Arg.Is<MessageOut>(message => message.User == "user" && message.Text == "blah blah blah")
        );
    }

}