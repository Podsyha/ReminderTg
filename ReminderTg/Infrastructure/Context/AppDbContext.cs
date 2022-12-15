using Microsoft.EntityFrameworkCore;
using ReminderTg.Infrastructure.Models;

namespace ReminderTg.Infrastructure.Context;

/// <summary>
/// Инициализация контекста бд
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        #region FluentAPI
         modelBuilder.Entity<ReminderModel>()
            .HasKey(x => x.Id);
         modelBuilder.Entity<ReminderModel>()
             .Property(x => x.IsSave)
             .IsRequired();
         modelBuilder.Entity<ReminderModel>()
             .Property(x => x.UserId)
             .IsRequired();
         #endregion
    }
    
    public virtual DbSet<ReminderModel> Reminder { get; set; }
}