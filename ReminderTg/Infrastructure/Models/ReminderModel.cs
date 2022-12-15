using System.Globalization;

namespace ReminderTg.Infrastructure.Models;

/// <summary>
/// Модель напоминания
/// </summary>
public sealed class ReminderModel
{
    public ReminderModel(long userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        IsSave = false;
        ReminderDays = new();
    }

    //TODO добавить и учитыватьч асовой пояс
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public TimeOnly? ReminderTime { get; set; }
    public List<DayOfWeek> ReminderDays { get; set; }
    public long UserId { get; set; }
    public bool IsSave { get; set; }

    public override string ToString()
    {
        var days = string.Empty;
        foreach (var day in ReminderDays)
        {
            days += "\n";
            days += CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetDayName(day);
        }

        return $"Название: {Title}\n\nДни: {days}\n\nВремя: {ReminderTime}";
    }

    /// <summary>
    /// Проверить можно ли добавить день недели
    /// </summary>
    /// <param name="dayOfWeek">День недели</param>
    /// <returns>True - можно добавить. False - Нельзя, день уже добавлен</returns>
    public bool CheckAvailableDay(DayOfWeek dayOfWeek) => ReminderDays.All(day => day != dayOfWeek);
}