using Microsoft.EntityFrameworkCore;
using TeleworkApp.Data;

namespace TeleworkApp.Services;

public class TeleworkService
{
    private readonly AppDbContext _context;
    private readonly HolidayService _holidayService;

    public TeleworkService(AppDbContext context, HolidayService holidayService)
    {
        _context = context;
        _holidayService = holidayService;
    }

    /// <summary>
    /// Gets a work day for a specific date.
    /// If no record exists, it's considered Office by default.
    /// </summary>
    public async Task<WorkDay?> GetDayAsync(DateOnly date)
    {
        return await _context.WorkDays.FirstOrDefaultAsync(d => d.Date == date);
    }

    /// <summary>
    /// Toggles the work type for a day.
    /// Cycle: No record (Office by default) -> Home -> Absence -> No record (back to Office)
    /// Only days marked as Home or Absence are saved to the database.
    /// </summary>
    public async Task<WorkDay?> ToggleDayAsync(DateOnly date)
    {
        // Don't allow marking weekends
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            return null;

        // Don't allow marking holidays
        if (await _holidayService.IsHolidayAsync(date))
            return null;

        var existingDay = await _context.WorkDays.FirstOrDefaultAsync(d => d.Date == date);

        if (existingDay == null)
        {
            // No record (Office by default) -> Home
            var newDay = new WorkDay { Date = date, Type = WorkType.Home };
            _context.WorkDays.Add(newDay);
            await _context.SaveChangesAsync();
            return newDay;
        }
        else if (existingDay.Type == WorkType.Home)
        {
            // Home -> Absence
            existingDay.Type = WorkType.Absence;
            await _context.SaveChangesAsync();
            return existingDay;
        }
        else
        {
            // Absence -> Delete (back to Office by default)
            _context.WorkDays.Remove(existingDay);
            await _context.SaveChangesAsync();
            return null;
        }
    }

    /// <summary>
    /// Gets all work days for a month (only those with database records).
    /// </summary>
    public async Task<List<WorkDay>> GetMonthDaysAsync(int year, int month)
    {
        return await _context.WorkDays
            .Where(d => d.Date.Year == year && d.Date.Month == month)
            .OrderBy(d => d.Date)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all work days for a quarter (only those with database records).
    /// </summary>
    public async Task<List<WorkDay>> GetQuarterDaysAsync(int year, int quarter)
    {
        int startMonth = (quarter - 1) * 3 + 1;
        int endMonth = startMonth + 2;

        var startDate = new DateOnly(year, startMonth, 1);
        var endDate = new DateOnly(year, endMonth, DateTime.DaysInMonth(year, endMonth));

        return await _context.WorkDays
            .Where(d => d.Date >= startDate && d.Date <= endDate)
            .OrderBy(d => d.Date)
            .ToListAsync();
    }

    /// <summary>
    /// Gets the statistics for a quarter.
    /// Days without database records are considered Office.
    /// Absence days don't count towards the percentage calculation.
    /// </summary>
    public async Task<QuarterStatistics> GetQuarterStatisticsAsync(int year, int quarter)
    {
        int startMonth = (quarter - 1) * 3 + 1;
        int endMonth = startMonth + 2;

        var startDate = new DateOnly(year, startMonth, 1);
        var endDate = new DateOnly(year, endMonth, DateTime.DaysInMonth(year, endMonth));

        // Get holidays for the quarter
        var holidays = await _holidayService.GetQuarterHolidaysAsync(year, quarter);
        var holidayDates = holidays.Select(h => h.Date).ToHashSet();

        // Calculate working days (excluding weekends and holidays)
        int workingDays = 0;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && 
                date.DayOfWeek != DayOfWeek.Sunday &&
                !holidayDates.Contains(date))
            {
                workingDays++;
            }
        }

        // Get recorded days from database
        var recordedDays = await _context.WorkDays
            .Where(d => d.Date >= startDate && d.Date <= endDate)
            .ToListAsync();

        int homeDays = recordedDays.Count(d => d.Type == WorkType.Home);
        int absenceDays = recordedDays.Count(d => d.Type == WorkType.Absence);
        
        // Effective days = working days - absences (those that count for percentage)
        int effectiveDays = workingDays - absenceDays;
        
        // Office days = effective days - home days (everything not marked is office)
        int officeDays = effectiveDays - homeDays;

        // Calculate percentages based on effective days
        double officePercentage = effectiveDays > 0 ? (double)officeDays / effectiveDays * 100 : 0;
        double homePercentage = effectiveDays > 0 ? (double)homeDays / effectiveDays * 100 : 0;

        // Calculate target days based on effective days
        int officeTarget = (int)Math.Ceiling(effectiveDays * 0.60);
        int homeTarget = (int)Math.Floor(effectiveDays * 0.40);

        // Calculate remaining days to meet target
        int officeRemainingDays = Math.Max(0, officeTarget - officeDays);
        int homeAvailableDays = Math.Max(0, homeTarget - homeDays);

        return new QuarterStatistics
        {
            Year = year,
            Quarter = quarter,
            StartDate = startDate,
            EndDate = endDate,
            WorkingDays = workingDays,
            EffectiveDays = effectiveDays,
            OfficeDays = officeDays,
            HomeDays = homeDays,
            AbsenceDays = absenceDays,
            OfficePercentage = officePercentage,
            HomePercentage = homePercentage,
            OfficeTarget = officeTarget,
            HomeTarget = homeTarget,
            OfficeRemainingDays = officeRemainingDays,
            HomeAvailableDays = homeAvailableDays,
            HolidayCount = holidays.Count
        };
    }

    /// <summary>
    /// Gets the current quarter.
    /// </summary>
    public static int GetCurrentQuarter()
    {
        return (DateTime.Today.Month - 1) / 3 + 1;
    }

    /// <summary>
    /// Gets the quarter name.
    /// </summary>
    public static string GetQuarterName(int quarter)
    {
        return quarter switch
        {
            1 => "Q1 (January - March)",
            2 => "Q2 (April - June)",
            3 => "Q3 (July - September)",
            4 => "Q4 (October - December)",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets the months of a quarter.
    /// </summary>
    public static (int Month1, int Month2, int Month3) GetQuarterMonths(int quarter)
    {
        int startMonth = (quarter - 1) * 3 + 1;
        return (startMonth, startMonth + 1, startMonth + 2);
    }
}

public class QuarterStatistics
{
    public int Year { get; set; }
    public int Quarter { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int WorkingDays { get; set; }
    public int EffectiveDays { get; set; }
    public int OfficeDays { get; set; }
    public int HomeDays { get; set; }
    public int AbsenceDays { get; set; }
    public double OfficePercentage { get; set; }
    public double HomePercentage { get; set; }
    public int OfficeTarget { get; set; }
    public int HomeTarget { get; set; }
    public int OfficeRemainingDays { get; set; }
    public int HomeAvailableDays { get; set; }
    public int HolidayCount { get; set; }

    public string QuarterName => TeleworkService.GetQuarterName(Quarter);
    public bool GoalAchieved => OfficePercentage >= 60 || EffectiveDays == 0;
}
