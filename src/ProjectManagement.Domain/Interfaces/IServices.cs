namespace ProjectManagement.Domain.Interfaces;

public interface ITokenService
{
    Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(Entities.ApplicationUser user);
    Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(string accessToken, string refreshToken);
}

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    Task SendOverdueTaskNotificationAsync(string to, string taskTitle, DateTime dueDate, CancellationToken cancellationToken = default);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAdmin { get; }
    bool IsProjectManager { get; }
}

public interface ITaskNotificationService
{
    Task NotifyTaskCreatedAsync(Guid taskId, Guid projectId, CancellationToken ct = default);
    Task NotifyTaskUpdatedAsync(Guid taskId, Guid projectId, CancellationToken ct = default);
    Task NotifyTaskDeletedAsync(Guid taskId, Guid projectId, CancellationToken ct = default);
    Task NotifyTaskStatusChangedAsync(Guid taskId, Guid projectId, Guid? epicId, Guid? sprintId, CancellationToken ct = default);
}
