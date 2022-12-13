namespace ReminderTg.Models;

/// <summary>
/// Этап создания напоминания
/// </summary>
public sealed class CreationStage
{
    public CreationStage(long userId, Guid reminderId)
    {
        UserId = userId;
        StageType = Stages.Title;
        ReminderId = reminderId;
    }
    
    public long UserId { get; set; }
    public Guid ReminderId { get; set; }
    public Stages StageType { get; set; }

    public enum Stages
    {
        Title = 0, 
        Time = 1, 
        Day = 2
    }
}