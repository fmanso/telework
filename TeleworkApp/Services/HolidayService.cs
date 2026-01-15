using Microsoft.EntityFrameworkCore;
using TeleworkApp.Data;

namespace TeleworkApp.Services;

public class HolidayService
{
    private readonly AppDbContext _context;

    public HolidayService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Loads the Madrid holidays for a specific year.
    /// Includes national, regional (Community of Madrid), and local (Madrid city) holidays.
    /// </summary>
    public async Task LoadHolidaysAsync(int year)
    {
        var existingHolidays = await _context.Holidays
            .Where(h => h.Date.Year == year)
            .AnyAsync();

        if (existingHolidays)
            return;

        var holidays = GetMadridHolidays(year);

        foreach (var holiday in holidays)
        {
            // Only add if it doesn't exist yet
            if (!await _context.Holidays.AnyAsync(h => h.Date == holiday.Date))
            {
                _context.Holidays.Add(holiday);
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the list of Madrid holidays for a given year.
    /// Holiday names are kept in Spanish as they are Spanish-specific holidays.
    /// </summary>
    private List<Holiday> GetMadridHolidays(int year)
    {
        var holidays = new List<Holiday>
        {
            // National Holidays
            new() { Date = new DateOnly(year, 1, 1), Name = "Año Nuevo", Type = "National" },
            new() { Date = new DateOnly(year, 1, 6), Name = "Epifanía del Señor (Reyes)", Type = "National" },
            new() { Date = new DateOnly(year, 5, 1), Name = "Fiesta del Trabajo", Type = "National" },
            new() { Date = new DateOnly(year, 8, 15), Name = "Asunción de la Virgen", Type = "National" },
            new() { Date = new DateOnly(year, 10, 12), Name = "Fiesta Nacional de España", Type = "National" },
            new() { Date = new DateOnly(year, 11, 1), Name = "Todos los Santos", Type = "National" },
            new() { Date = new DateOnly(year, 12, 6), Name = "Día de la Constitución", Type = "National" },
            new() { Date = new DateOnly(year, 12, 8), Name = "Inmaculada Concepción", Type = "National" },
            new() { Date = new DateOnly(year, 12, 25), Name = "Navidad", Type = "National" },

            // Regional Holidays (Community of Madrid)
            new() { Date = new DateOnly(year, 5, 2), Name = "Fiesta de la Comunidad de Madrid", Type = "Regional" },

            // Local Holidays (Madrid city)
            new() { Date = new DateOnly(year, 5, 15), Name = "San Isidro", Type = "Local" },
            new() { Date = new DateOnly(year, 11, 9), Name = "Virgen de la Almudena", Type = "Local" },
        };

        // Add Easter holidays (variable dates based on year)
        var (maundyThursday, goodFriday) = CalculateEasterHolidays(year);
        holidays.Add(new Holiday { Date = maundyThursday, Name = "Jueves Santo", Type = "National" });
        holidays.Add(new Holiday { Date = goodFriday, Name = "Viernes Santo", Type = "National" });

        return holidays;
    }

    /// <summary>
    /// Calculates Maundy Thursday and Good Friday dates using the Computus algorithm.
    /// </summary>
    private (DateOnly maundyThursday, DateOnly goodFriday) CalculateEasterHolidays(int year)
    {
        // Computus algorithm to calculate Easter Sunday date
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31;
        int day = ((h + l - 7 * m + 114) % 31) + 1;

        var easterSunday = new DateOnly(year, month, day);
        var goodFriday = easterSunday.AddDays(-2);
        var maundyThursday = easterSunday.AddDays(-3);

        return (maundyThursday, goodFriday);
    }

    /// <summary>
    /// Checks if a date is a holiday.
    /// </summary>
    public async Task<bool> IsHolidayAsync(DateOnly date)
    {
        return await _context.Holidays.AnyAsync(h => h.Date == date);
    }

    /// <summary>
    /// Gets all holidays for a month.
    /// </summary>
    public async Task<List<Holiday>> GetMonthHolidaysAsync(int year, int month)
    {
        return await _context.Holidays
            .Where(h => h.Date.Year == year && h.Date.Month == month)
            .OrderBy(h => h.Date)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all holidays for a quarter.
    /// </summary>
    public async Task<List<Holiday>> GetQuarterHolidaysAsync(int year, int quarter)
    {
        int startMonth = (quarter - 1) * 3 + 1;
        int endMonth = startMonth + 2;

        return await _context.Holidays
            .Where(h => h.Date.Year == year && h.Date.Month >= startMonth && h.Date.Month <= endMonth)
            .OrderBy(h => h.Date)
            .ToListAsync();
    }
}
