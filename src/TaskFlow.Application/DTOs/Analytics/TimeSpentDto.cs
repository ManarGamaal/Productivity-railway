namespace TaskFlow.Application.DTOs.Analytics;

public sealed record TimeSpentDto(int Hours, int Minutes, double TotalMinutes)
{
    public static TimeSpentDto FromMinutes(double totalMinutes)
    {
        var rounded = (int)Math.Round(totalMinutes, MidpointRounding.AwayFromZero);
        return new TimeSpentDto(rounded / 60, rounded % 60, Math.Round(totalMinutes, 2));
    }
}
