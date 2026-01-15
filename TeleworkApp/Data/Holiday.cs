namespace TeleworkApp.Data;

/// <summary>
/// Represents a holiday (public holiday).
/// </summary>
public class Holiday
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // National, Regional, Local
}
