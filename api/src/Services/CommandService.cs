using System.Text.RegularExpressions;
using Api.Model;

namespace Api.Services;

public interface ICommandService
{
    void Execute(Request request);
}

public class CommandService : ICommandService
{
    private const string CMD_LOGIN = @"^/login\s+(\S+):(\S+)";
    private const string CMD_LOGOFF = @"^/logoff$";
    private const string CMD_STOCK = @"^/stock\s+(\S+)";
    private const string CMD_HELP = @"^/help";

    private readonly ILogger<CommandService> _logger;
    private IMessageService _messageService;
    private ISecurityService _securityService;
    private IStockService _stockService;

    public CommandService(ILogger<CommandService> logger, IStockService stockService, ISecurityService securityService, IMessageService messageService)
    {
        _logger = logger;
        _stockService = stockService;
        _securityService = securityService;
        _messageService = messageService;
    }

    public void Execute(Request request)
    {
        // HELP
        if (Regex.IsMatch(request.Message.Text, CMD_HELP))
        {
            var response = new MessageOut("Commands: /login user:token /logoff /stock xxx");

            _messageService.SingleSend(request.Session, response);

            return;
        }

        // LOGIN 
        var login = Regex.Matches(request.Message.Text, CMD_LOGIN);
        if (login?.Count > 0)
        {
            var id = login[0].Groups[1]?.Value;
            var token = login[0].Groups[2]?.Value;

            _logger.LogInformation($"Login {id}");

            var response =
                id is not null && token is not null && _securityService.Login(request.Session, id, token) ?
                new MessageOut(request.Message.Text) :
                new MessageOut("Access denied");

            _messageService.SingleSend(request.Session, response);

            return;
        }

        // CHECK LOGIN
        var user = _securityService.User(request.Session);
        if (user is null || request.Session == string.Empty)
        {
            _logger.LogWarning("Access denied");

            var response = new MessageOut("Access denied");

            _messageService.SingleSend(request.Session, response);

            return;
        }

        // LOGOFF
        if (Regex.IsMatch(request.Message.Text, CMD_LOGOFF))
        {
            _logger.LogInformation($"Logoff {user}");

            _securityService.Logoff(request.Session);

            var response = new MessageOut(request.Message.Text);

            _messageService.SingleSend(request.Session, response);

            return;
        }

        // STOCK
        var stock = Regex.Matches(request.Message.Text, CMD_STOCK);
        if (stock?.Count > 0)
        {
            var code = stock[0].Groups[1]?.Value;

            MessageOut response;
            if (code is null)
            {
                _logger.LogWarning($"Invalid Stock code {code}");

                response = new MessageOut("Invalid Stock Code");
            }
            else
            {
                _logger.LogInformation($"Enqueue Stock {code}");

                _stockService.Enqueue(code);

                response = new MessageOut($"Enqueue Stock {code}");
            }

            _messageService.SingleSend(request.Session, response);

            return;
        }

        // BROADCAST
        {
            var response = new MessageOut(request.Message.Text, user);

            _messageService.Broadcast(response);
        }
    }

}