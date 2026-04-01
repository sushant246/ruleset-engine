using Microsoft.EntityFrameworkCore;
using RulesetEngine.Domain.Entities;

namespace RulesetEngine.Infrastructure.Database;

public class RulesetDbContext : DbContext
{
    public RulesetDbContext(DbContextOptions<RulesetDbContext> options) : base(options) { }

    public DbSet<Ruleset> Rulesets => Set<Ruleset>();
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<Condition> Conditions => Set<Condition>();
    public DbSet<RuleResult> RuleResults => Set<RuleResult>();
    public DbSet<EvaluationLog> EvaluationLogs => Set<EvaluationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Ruleset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasMany(e => e.Rules)
                  .WithOne(r => r.Ruleset)
                  .HasForeignKey(r => r.RulesetId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Conditions)
                  .WithOne(c => c.Ruleset)
                  .HasForeignKey(c => c.RulesetId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Rule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.Result)
                  .WithOne(r => r.Rule)
                  .HasForeignKey<RuleResult>(r => r.RuleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Condition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Field).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Operator).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(500);
            entity.HasOne(e => e.Rule)
                  .WithMany(r => r.Conditions)
                  .HasForeignKey(e => e.RuleId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Ruleset)
                  .WithMany(r => r.Conditions)
                  .HasForeignKey(e => e.RulesetId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RuleResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductionPlant).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<EvaluationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Reason).HasMaxLength(500);
        });
    }
}
