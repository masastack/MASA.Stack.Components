
namespace Masa.Stack.Components.Infrastructure.Identity;

public class LogoutSessionManager
{
    private static readonly object _lock = new object();
    private readonly ILogger<LogoutSessionManager> _logger;
    readonly IDistributedCacheClient _distributedCacheClient;


    public LogoutSessionManager(ILoggerFactory loggerFactory, IDistributedCacheClient distributedCacheClient)
    {
        _distributedCacheClient = distributedCacheClient;
        _logger = loggerFactory.CreateLogger<LogoutSessionManager>();
    }

    public void Add(string sub, string sid)
    {
        _logger.LogWarning($"Backchannel add a logout to the session: sub: {sub}, sid: {sid}");
        //todo Masa DistributedLock
        lock (_lock)
        {
            var key = sub + sid;
            var logoutSession = new BackchannelLogoutSession(sub, sid);
            _logger.LogInformation($"Backchannel logoutSession: {logoutSession}");
            _distributedCacheClient.Set(key, logoutSession);
        }
    }

    public async Task<bool> IsLoggedOutAsync(string sub, string sid)
    {
        _logger.LogInformation($"Backchannel IsLoggedOutAsync: sub: {sub}, sid: {sid}");
        var key = sub + sid;
        var matches = false;
        var logoutSession = await _distributedCacheClient.GetAsync<BackchannelLogoutSession>(key);
        if (logoutSession != null)
        {
            matches = logoutSession.IsMatch(sub, sid);
            _logger.LogInformation($"Backchannel Logout session exists T/F {matches} : {sub}, sid: {sid}");
        }
        return matches;
    }
}

class BackchannelLogoutSession
{
    public string Sub { get; set; }

    public string Sid { get; set; }

    public BackchannelLogoutSession(string sub, string sid)
    {
        Sub = sub;
        Sid = sid;
    }

    public bool IsMatch(string sub, string sid)
    {
        return (Sid == sid && Sub == sub) ||
               (Sid == sid && Sub == null) ||
               (Sid == null && Sub == sub);
    }
}
