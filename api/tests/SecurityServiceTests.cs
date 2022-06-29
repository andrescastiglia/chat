using Api.Services;
using NUnit.Framework;

namespace api_ut;

public class SecurityServiceExtended : SecurityService
{
    public int LockTimeoutMillisecs { get { return SecurityService.LOCK_TIMEOUT_MILLISECS; } }
    public IDictionary<string, string> GetUsers() { return _userCollection; }
    public ReaderWriterLock GetLocker() { return _locker; }
}

public class SecurityServiceTests
{
    private SecurityServiceExtended _securityService = new SecurityServiceExtended();

    [SetUp]
    public void Setup()
    {
        _securityService.GetUsers().Clear();
    }

    [Test]
    public void Login_Success()
    {
        _securityService.Login("1", "u", "t");
        Assert.AreEqual(_securityService.GetUsers().Count, 1);
    }

    [Test]
    public void Logoff_Success()
    {
        _securityService.Login("1", "u", "t");
        Assert.AreEqual(_securityService.GetUsers().Count, 1);

        _securityService.Logoff("1");
        Assert.Zero(_securityService.GetUsers().Count);
    }

    [Test]
    public void GetUser_Success()
    {
        _securityService.Login("1", "u", "t");
        Assert.AreEqual(_securityService.GetUsers().Count, 1);

        var user = _securityService.User("1");
        Assert.AreEqual(user, "u");
    }

    
    [Test]
    public void GetUser_Fail()
    {
        _securityService.Login("1", "u", "t");
        Assert.AreEqual(_securityService.GetUsers().Count, 1);

        var user = _securityService.User("2");
        Assert.IsNull(user);
    }
}