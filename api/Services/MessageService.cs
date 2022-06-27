using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Api.Model;

namespace Api.Services;

public interface IMessageService
{
    public void AddSession(string session, WebSocket webSocket);
    public void RemoveSession(string session);
    public void SingleSend(string session, MessageOut message);
    public void Broadcast(MessageOut message);
}

public class MessageService: IMessageService
{
    private readonly ILogger<MessageService> _logger;
    private IDictionary<string, WebSocket> _socketCollection;
    private ReaderWriterLock _locker;
    private const int LOCK_TIMEOUT_MILLISECS = 3000;
    private ISecurityService _securityService;

    public MessageService(ILogger<MessageService> logger, ISecurityService securityService)
    {
        _logger = logger;
        _locker = new ReaderWriterLock();
        _socketCollection = new Dictionary<string, WebSocket>();
        _securityService = securityService;
    }

    public void AddSession(string session, WebSocket webSocket)
    {
        try
        {
            _locker.AcquireWriterLock(LOCK_TIMEOUT_MILLISECS);

            _socketCollection.Add(session, webSocket);
        }
        finally
        {
            _locker.ReleaseWriterLock();
        }
    }

    public void RemoveSession(string session)
    {
        try
        {
            _locker.AcquireWriterLock(LOCK_TIMEOUT_MILLISECS);

            _socketCollection.Remove(session);
        }
        finally
        {
            _locker.ReleaseWriterLock();

            _securityService.Logoff( session );
        }
    }

    public async void SingleSend(string session, MessageOut message)
    {
        try
        {
            _locker.AcquireReaderLock(LOCK_TIMEOUT_MILLISECS);

            var webSocket = _socketCollection[session];

            if (webSocket?.State == WebSocketState.Open)
            {
                var raw = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                await webSocket.SendAsync(new ArraySegment<byte>(raw, 0, raw.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else
            {
                _logger.LogWarning($"Closed socket was found {session}");

                LockCookie lockCookie = _locker.UpgradeToWriterLock(LOCK_TIMEOUT_MILLISECS);

                _socketCollection.Remove(session);

                _locker.DowngradeFromWriterLock(ref lockCookie);
            }
        }
        finally
        {
            _locker.ReleaseReaderLock();
        }
    }

    public async void Broadcast(MessageOut message)
    {
        try
        {
            _locker.AcquireReaderLock(LOCK_TIMEOUT_MILLISECS);

            foreach (var (session, webSocket) in _socketCollection)
            {
                if (webSocket.State == WebSocketState.Open)
                {

                    var raw = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                    await webSocket.SendAsync(new ArraySegment<byte>(raw, 0, raw.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                else
                {
                    _logger.LogWarning($"Closed socket was found {session}");

                    LockCookie lockCookie = _locker.UpgradeToWriterLock(LOCK_TIMEOUT_MILLISECS);

                    _socketCollection.Remove(session);

                    _locker.DowngradeFromWriterLock(ref lockCookie);
                }
            }
        }
        finally
        {
            _locker.ReleaseReaderLock();
        }
    }
}