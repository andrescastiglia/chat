using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Api.Services;
using Api.Model;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private IMessageService _messageService;
    private ICommandService _commandService;

    public ChatController(ILogger<ChatController> logger, IMessageService messageService, ICommandService commandService)
    {
        _logger = logger;
        _messageService = messageService;
        _commandService = commandService;
    }

    [HttpGet("/chat")]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            string? session = null;
            MessageIn? message = null;
            var buffer = new byte[0x0FFF];
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync(new WebSocketAcceptContext { DangerousEnableCompression = true });

            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.Count > 0)
            {
                message = JsonSerializer.Deserialize<MessageIn>(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (message is not null)
                {
                    session = message.Session;
                    _messageService.AddSession(session, webSocket);
                    _commandService.Execute(new Request(session, message));
                }
            }

            while (!result.CloseStatus.HasValue)
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.Count < 1) continue;

                message = JsonSerializer.Deserialize<MessageIn>(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (message is not null)
                {
                    if (session is null)
                    {
                        session = message.Session;
                        _messageService.AddSession(session, webSocket);
                    }

                    if (session == message.Session)
                    {
                        _commandService.Execute(new Request(session, message));
                    }
                    else
                    {
                        _logger.LogWarning("Violation security");

                        _messageService.RemoveSession(session);
                        await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, null, CancellationToken.None);
                        return;
                    }
                }
            }

            if (session is not null) _messageService.RemoveSession(session);
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
        else
        {
            _logger.LogWarning("Bad Request");

            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}
