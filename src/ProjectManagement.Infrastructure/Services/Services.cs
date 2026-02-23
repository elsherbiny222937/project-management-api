using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProjectManagement.Domain.Interfaces;

namespace ProjectManagement.Infrastructure.Services;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private static readonly HashSet<string> _keys = new();

    public CacheService(IMemoryCache cache) { _cache = cache; }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(5) };
        _cache.Set(key, value, options);
        _keys.Add(key);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.Remove(key);
        _keys.Remove(key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        var keysToRemove = _keys.Where(k => k.StartsWith(prefix)).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
            _keys.Remove(key);
        }
        return Task.CompletedTask;
    }
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger) { _logger = logger; }

    public Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("[Email] To: {To}, Subject: {Subject}, Body: {Body}", to, subject, body);
        return Task.CompletedTask;
    }

    public Task SendOverdueTaskNotificationAsync(string to, string taskTitle, DateTime dueDate, CancellationToken ct = default)
    {
        _logger.LogInformation("[Email] Overdue Task Notification - To: {To}, Task: {Title}, DueDate: {DueDate}",
            to, taskTitle, dueDate);
        return Task.CompletedTask;
    }
}

public class CurrentUserService : ICurrentUserService
{
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User
        ?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    public string? UserName => _httpContextAccessor.HttpContext?.User
        ?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

    public bool IsAdmin => _httpContextAccessor.HttpContext?.User
        ?.IsInRole("Admin") ?? false;

    public bool IsProjectManager => _httpContextAccessor.HttpContext?.User
        ?.IsInRole("ProjectManager") ?? false;
}
