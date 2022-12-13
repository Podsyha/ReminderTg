namespace ReminderTg.Models;

/// <summary>
/// Модель напоминания
/// </summary>
public sealed class ReminderModel
{
    public ReminderModel(long userId)
    {
        ReminderId = Guid.NewGuid();
        UserId = userId;
        IsSave = false;
    }
    
    public Guid ReminderId { get; set; }
    public string Title { get; set; }
    public TimeOnly RimenderTime { get; set; }
    public DayOfWeek[] ReminderDays { get; set; }
    public long UserId { get; set; }
    public bool IsSave { get; set; }
}