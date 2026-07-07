using CampusFlow.Domain.Common;
using CampusFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace CampusFlow.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    private readonly Guid? _currentInstitutionId;
    private readonly string _currentUser;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        Guid? currentInstitutionId,
        string currentUser = "system")
        : base(options)
    {
        _currentInstitutionId = currentInstitutionId;
        _currentUser = currentUser;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(SetTenantFilter),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { builder });
            }
        }
    }

    private void SetTenantFilter<TEntity>(ModelBuilder builder) where TEntity : class, ITenantEntity
    {
        builder.Entity<TEntity>()
            .HasQueryFilter(e => e.InstitutionId == _currentInstitutionId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                entry.Entity.CreatedBy = _currentUser;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAtUtc = DateTime.UtcNow;
                entry.Entity.ModifiedBy = _currentUser;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}