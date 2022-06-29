namespace Api.Services;

public interface ISecurityService
{
    bool Login(string session, string user, string token);
    void Logoff(string session);
    string? User(string session);
}

public class SecurityService : ISecurityService
{
    protected IDictionary<string, string> _userCollection;
    protected ReaderWriterLock _locker;
    protected const int LOCK_TIMEOUT_MILLISECS = 3000;

    public SecurityService()
    {
        _userCollection = new Dictionary<string, string>();
        _locker = new ReaderWriterLock();
    }
    public bool Login(string session, string user, string token)
    {
        try
        {
            _locker.AcquireWriterLock(LOCK_TIMEOUT_MILLISECS);

            _userCollection.Add(session, user);
        }
        finally
        {
            _locker.ReleaseWriterLock();
        }

        return true;
    }

    public void Logoff(string session)
    {
        try
        {
            _locker.AcquireWriterLock(LOCK_TIMEOUT_MILLISECS);

            _userCollection.Remove(session);
        }
        finally
        {
            _locker.ReleaseWriterLock();
        }
    }

    public string? User(string session)
    {
        try
        {
            _locker.AcquireReaderLock(LOCK_TIMEOUT_MILLISECS);

            return _userCollection[session];
        }
        catch {
            return null;
        }
        finally
        {
            _locker.ReleaseReaderLock();
        }
    }
}