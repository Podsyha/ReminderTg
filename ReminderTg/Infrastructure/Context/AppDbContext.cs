using Microsoft.EntityFrameworkCore;
using ReminderTg.Infrastructure.Models;

namespace ReminderTg.Infrastructure.Context;

/// <summary>
/// Инициализация контекста бд
/// </summary>
public abstract class AppDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(databaseName: "ReminderDb");
    }

    public DbSet<ReminderModel> Reminder { get; set; }
    public DbSet<CreationStage> CreationStage { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        #region FluentAPI
         modelBuilder.Entity<ReminderModel>()
            .HasKey(x => x.Id);
         modelBuilder.Entity<ReminderModel>()
             .Property(x => x.IsSave)
             .IsRequired();
         #endregion
    }
}