using System.Text;
using Asp.Versioning;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProjectManagement.Api.Middleware;
using ProjectManagement.Api.Filters;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Domain.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using ProjectManagement.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Threading.RateLimiting;
using ProjectManagement.Infrastructure.BackgroundJobs;
using ProjectManagement.Infrastructure.DependencyInjection;
using ProjectManagement.Infrastructure.Identity;
using ProjectManagement.Infrastructure.Persistence;
using ProjectManagement.Infrastructure.Persistence.Repositories;
using ProjectManagement.Infrastructure.Persistence.Seed;
using ProjectManagement.Infrastructure.Services;
using ProjectManagement.Infrastructure.SignalR;

var builder = WebApplication.CreateBuilder(args);

// ── Autofac ──
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterType<UnitOfWork>().As<IUnitOfWork>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<ProjectRepository>().As<IProjectRepository>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<TaskRepository>().As<ITaskRepository>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<EpicRepository>().As<IEpicRepository>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<SprintRepository>().As<ISprintRepository>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<SubTaskRepository>().As<ISubTaskRepository>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<CommentRepository>().As<ICommentRepository>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<TaskStatusRepository>().As<ITaskStatusRepository>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<TokenService>().As<ITokenService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<EmailService>().As<IEmailService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<CacheService>().As<ICacheService>().SingleInstance();
    containerBuilder.RegisterType<CurrentUserService>().As<ICurrentUserService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<TaskNotificationService>().As<ITaskNotificationService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<IdempotencyFilter>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<OverdueTaskNotificationJob>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<SprintStateTransitionJob>().InstancePerLifetimeScope();
});

// ── Database ──
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=ProjectManagement.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString, b => b.MigrationsAssembly("ProjectManagement.Infrastructure")));

// ── Identity ──
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ── JWT Authentication ──
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ProjectManagementApi",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "ProjectManagementApi",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };

    // SignalR token support
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) &&
                context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// ── Application Services (MediatR, FluentValidation, AutoMapper) ──
builder.Services.AddApplicationServices();

// ── Controllers ──
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// ── API Versioning ──
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ── Swagger ──
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Project Management API",
        Version = "v1",
        Description = "A comprehensive project management API with Clean Architecture, CQRS, and real-time features."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Enter 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── SignalR ──
builder.Services.AddSignalR();

// ── Hangfire ──
builder.Services.AddHangfire(config =>
    config.UseMemoryStorage());
builder.Services.AddHangfireServer();

// ── Caching ──
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// ── CORS ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ── Health Checks ──
builder.Services.AddHealthChecks();

// ── Authorization ──
builder.Services.AddScoped<IAuthorizationHandler, ProjectAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ProjectMember", policy => 
        policy.Requirements.Add(new ProjectMembershipRequirement()));
});

// ── Rate Limiting ──
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthPolicy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
        opt.QueueLimit = 0;
    });
});

// ── Global Exception Handling ──
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// ── Middleware Pipeline ──
app.UseSecurityHeaders();
app.UseExceptionHandler();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Project Management API V1");
        options.RoutePrefix = "swagger";
    });
}

if (!app.Environment.IsDevelopment()) 
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TaskHub>("/hubs/tasks");

// Health checks
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false 
});
app.MapHealthChecks("/health/ready");

// Hangfire dashboard
app.MapHangfireDashboard("/hangfire");

// Configure Hangfire recurring jobs
RecurringJob.AddOrUpdate<SprintStateTransitionJob>(
    "sprint-state-transition", job => job.Execute(), Cron.Hourly);

RecurringJob.AddOrUpdate<OverdueTaskNotificationJob>(
    "overdue-task-notification", job => job.Execute(), Cron.Daily);

// Seed data
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

app.Run();

