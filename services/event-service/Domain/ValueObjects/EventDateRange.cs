namespace NextHappen.Event.Domain.ValueObjects;

public record EventDateRange(DateTime StartDate, DateTime EndDate)
{
    public static EventDateRange Create(DateTime start, DateTime end)
    {
        if (end < start)
            throw new ArgumentException("La fecha final no puede ser menor que la inicial.");
        return new EventDateRange(start, end);
    }
}
