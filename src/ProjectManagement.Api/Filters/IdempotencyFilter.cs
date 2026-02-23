using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProjectManagement.Domain.Interfaces;

namespace ProjectManagement.Api.Filters;

public class IdempotencyFilter : ActionFilterAttribute
{
    private const string IdempotencyKeyHeader = "X-Idempotency-Key";

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(IdempotencyKeyHeader, out var IdempotencyKey))
        {
            await next();
            return;
        }

        var cache = context.HttpContext.RequestServices.GetRequiredService<ICacheService>();
        var cacheKey = $"Idempotency_{IdempotencyKey}";

        if (await cache.GetAsync<bool>(cacheKey))
        {
            context.Result = new ConflictObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Duplicate Request",
                Detail = "This request has already been processed."
            });
            return;
        }

        // Add to cache with a short TTL (e.g., 2 minutes) to prevent accidental double-processing
        await cache.SetAsync(cacheKey, true, TimeSpan.FromMinutes(2));

        await next();
    }
}
