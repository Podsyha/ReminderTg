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
    public TimeOnly ReminderTime { get; set; }
    public List<DayOfWeek> ReminderDays { get; set; }
    public long UserId { get; set; }
    public bool IsSave { get; set; }
}