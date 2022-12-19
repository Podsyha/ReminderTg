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
         modelBuilder.Entity<RepeatReminderModel>()
            .HasKey(x => x.Id);
         modelBuilder.Entity<RepeatReminderModel>()
             .Property(x => x.IsSave)
             .IsRequired();
         modelBuilder.Entity<RepeatReminderModel>()
             .Property(x => x.UserId)
             .IsRequired();

         modelBuilder.Entity<OnceReminderModel>()
             .HasKey(x => x.Id);
         modelBuilder.Entity<OnceReminderModel>()
             .Property(x => x.IsSave)
             .IsRequired();
         modelBuilder.Entity<OnceReminderModel>()
             .Property(x => x.UserId)
             .IsRequired();
         #endregion
    }
    
    public virtual DbSet<RepeatReminderModel> RepeatReminder { get; set; }
    public virtual DbSet<OnceReminderModel> OnceReminder { get; set; }
}