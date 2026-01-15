namespace TeleworkApp.Data;

/// <summary>
/// Represents a work day record with the type of work location.
/// </summary>
public class WorkDay
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public WorkType Type { get; set; }
}
