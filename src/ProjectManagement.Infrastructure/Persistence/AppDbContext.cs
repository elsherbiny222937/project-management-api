using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Domain.Common;

namespace ProjectManagement.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
    public DbSet<Epic> Epics => Set<Epic>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<SubTask> SubTasks => Set<SubTask>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Domain.Entities.TaskStatus> TaskStatuses => Set<Domain.Entities.TaskStatus>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Project
        builder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(250);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Budget).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Owner).WithMany().HasForeignKey(e => e.OwnerId).OnDelete(DeleteBehavior.SetNull);
        });

        // ProjectTask
        builder.Entity<ProjectTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => new { e.ProjectId, e.StatusId });
            entity.HasIndex(e => new { e.ProjectId, e.PriorityLevel });
            entity.HasOne(e => e.Project).WithMany(p => p.Tasks).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Status).WithMany().HasForeignKey(e => e.StatusId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Epic).WithMany(ep => ep.Tasks).HasForeignKey(e => e.EpicId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Sprint).WithMany(s => s.Tasks).HasForeignKey(e => e.SprintId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.AssignedTo).WithMany().HasForeignKey(e => e.AssignedToId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.RequestedBy).WithMany().HasForeignKey(e => e.RequestedById).OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(e => e.Tags).WithMany(t => t.Tasks).UsingEntity(j => j.ToTable("TaskTags"));
            
            // Task Dependencies (Blockers)
            entity.HasMany(e => e.Blocks)
                .WithMany(e => e.BlockedBy)
                .UsingEntity<Dictionary<string, object>>(
                    "TaskDependencies",
                    j => j.HasOne<ProjectTask>().WithMany().HasForeignKey("BlockedTaskId"),
                    j => j.HasOne<ProjectTask>().WithMany().HasForeignKey("BlockerTaskId"));
        });

        // Epic
        builder.Entity<Epic>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => new { e.ProjectId, e.Status });
            entity.HasOne(e => e.Project).WithMany(p => p.Epics).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Owner).WithMany().HasForeignKey(e => e.OwnerId).OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(e => e.Tags).WithMany(t => t.Epics).UsingEntity(j => j.ToTable("EpicTags"));
        });

        // Sprint
        builder.Entity<Sprint>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => new { e.ProjectId, e.StartsAt });
            entity.HasOne(e => e.Project).WithMany(p => p.Sprints).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        // SubTask
        builder.Entity<SubTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.HasOne(e => e.Task).WithMany(t => t.SubTasks).HasForeignKey(e => e.TaskId).OnDelete(DeleteBehavior.Cascade);
        });

        // Tag
        builder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // ProjectMember
        builder.Entity<ProjectMember>(entity =>
        {
            entity.HasKey(e => new { e.ProjectId, e.UserId });
            entity.HasOne(e => e.Project).WithMany(p => p.Members).HasForeignKey(e => e.ProjectId);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
        });

        // AuditLog
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.ChangedAt);
        });

        // TaskStatus
        builder.Entity<Domain.Entities.TaskStatus>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Project).WithMany().HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        // Comment
        builder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.HasOne(e => e.Task).WithMany(t => t.Comments).HasForeignKey(e => e.TaskId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Apply Global Query Filters for Soft Delete
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                builder.Entity(entityType.ClrType).HasQueryFilter(ConvertFilterExpression(entityType.ClrType));
            }
        }
    }

    private static System.Linq.Expressions.LambdaExpression ConvertFilterExpression(Type type)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(type, "e");
        var property = System.Linq.Expressions.Expression.Property(parameter, "IsDeleted");
        var falseConstant = System.Linq.Expressions.Expression.Constant(false);
        var body = System.Linq.Expressions.Expression.Equal(property, falseConstant);
        return System.Linq.Expressions.Expression.Lambda(body, parameter);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OnBeforeSaveChanges();
        var auditEntries = OnBeforeAuditSaveChanges();
        var result = await base.SaveChangesAsync(cancellationToken);
        if (auditEntries.Count > 0)
        {
            AuditLogs.AddRange(auditEntries);
            await base.SaveChangesAsync(cancellationToken);
        }
        return result;
    }

    private void OnBeforeSaveChanges()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.IsDeleted = false;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
    }

    private List<AuditLog> OnBeforeAuditSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            // Skip Identity entities to avoid "Id" property errors and audit bloat
            var entityType = entry.Entity.GetType().Name;
            if (entityType.StartsWith("Identity") || entityType.Contains("Role") || entityType.Contains("Claim"))
                continue;

            var idProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
            var entityId = idProp?.CurrentValue?.ToString() ?? "N/A";

            var audit = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = entry.State.ToString(),
                ChangedAt = DateTime.UtcNow
            };

            if (entry.State == EntityState.Modified)
            {
                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();
                foreach (var prop in entry.Properties.Where(p => p.IsModified))
                {
                    oldValues[prop.Metadata.Name] = prop.OriginalValue;
                    newValues[prop.Metadata.Name] = prop.CurrentValue;
                }
                audit.OldValues = System.Text.Json.JsonSerializer.Serialize(oldValues);
                audit.NewValues = System.Text.Json.JsonSerializer.Serialize(newValues);
            }

            auditEntries.Add(audit);
        }

        return auditEntries;
    }
}
