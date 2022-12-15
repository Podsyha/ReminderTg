namespace ReminderTg.Infrastructure.Models;

/// <summary>
/// Модель напоминания
/// </summary>
public sealed class ReminderModel
{
    public ReminderModel(long userId)
    {
        UserId = userId;
        IsSave = false;
        ReminderDays = new();
    }
    
    public Guid Id { get; set; }
    public string Title { get; set; }
    public TimeOnly ReminderTime { get; set; }
    public List<DayOfWeek> ReminderDays { get; set; }
    public long UserId { get; set; }
    public bool IsSave { get; set; }

    public override string ToString()
    {
        return $"Title: {Title}\n Days: {ReminderDays}\n Time: {ReminderTime}";
    }
}