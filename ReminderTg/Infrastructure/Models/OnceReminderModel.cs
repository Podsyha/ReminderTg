namespace ReminderTg.Infrastructure.Models;

public sealed class OnceReminderModel
{
    public OnceReminderModel(long userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        IsSave = false;
    }

    public Guid Id { get; set; }
    public string? Title { get; set; }
    public DateTime ReminderDateTime { get; set; }
    public TimeZoneInfo TimeZoneInfo { get; set; }
    public long UserId { get; set; }
    public bool IsSave { get; set; }

    public override string ToString() =>
        $"Название: {Title}\n\nДата: {ReminderDateTime}\nЧасовой пояс: {TimeZoneInfo.DisplayName}";
}