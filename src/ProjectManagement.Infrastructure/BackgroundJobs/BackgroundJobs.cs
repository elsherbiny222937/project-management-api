using Microsoft.Extensions.Logging;
using ProjectManagement.Domain.Interfaces;

namespace ProjectManagement.Infrastructure.BackgroundJobs;

public class OverdueTaskNotificationJob
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _emailService;
    private readonly ILogger<OverdueTaskNotificationJob> _logger;

    public OverdueTaskNotificationJob(IUnitOfWork uow, IEmailService emailService,
        ILogger<OverdueTaskNotificationJob> logger)
    { _uow = uow; _emailService = emailService; _logger = logger; }

    public async Task Execute()
    {
        _logger.LogInformation("Running overdue task notification job...");
        var overdueTasks = await _uow.Tasks.GetOverdueTasksAsync();

        foreach (var task in overdueTasks)
        {
            if (task.AssignedTo?.Email != null && task.DueDate.HasValue)
            {
                await _emailService.SendOverdueTaskNotificationAsync(
                    task.AssignedTo.Email, task.Title, task.DueDate.Value);
            }
        }

        _logger.LogInformation("Overdue task notification job completed. {Count} notifications sent.", overdueTasks.Count);
    }
}

// Maps from CELERYBEAT_SCHEDULE → sprints.tasks.update_state (hourly job)
public class SprintStateTransitionJob
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SprintStateTransitionJob> _logger;

    public SprintStateTransitionJob(IUnitOfWork uow, ILogger<SprintStateTransitionJob> logger)
    { _uow = uow; _logger = logger; }

    public async Task Execute()
    {
        _logger.LogInformation("Running sprint state transition job...");
        await _uow.Sprints.AutoTransitionStatesAsync();
        await _uow.SaveChangesAsync();
        _logger.LogInformation("Sprint state transition job completed.");
    }
}
