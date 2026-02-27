using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Recurring;

namespace Kakeibo.Tests.Features.Recurring;

public sealed class RecurrenceCalculatorTests
{
    [Fact]
    public void Daily_AddsOneDay()
    {
        var date = new LocalDate(2026, 3, 1);
        var next = RecurrenceCalculator.CalculateNext(date, RecurrenceFrequency.Daily);
        Assert.Equal(new LocalDate(2026, 3, 2), next);
    }

    [Fact]
    public void Weekly_AddsSevenDays()
    {
        var date = new LocalDate(2026, 3, 1);
        var next = RecurrenceCalculator.CalculateNext(date, RecurrenceFrequency.Weekly);
        Assert.Equal(new LocalDate(2026, 3, 8), next);
    }

    [Fact]
    public void Biweekly_AddsFourteenDays()
    {
        var date = new LocalDate(2026, 3, 1);
        var next = RecurrenceCalculator.CalculateNext(date, RecurrenceFrequency.Biweekly);
        Assert.Equal(new LocalDate(2026, 3, 15), next);
    }

    [Fact]
    public void Monthly_AddsOneMonth()
    {
        var date = new LocalDate(2026, 3, 15);
        var next = RecurrenceCalculator.CalculateNext(date, RecurrenceFrequency.Monthly);
        Assert.Equal(new LocalDate(2026, 4, 15), next);
    }

    [Fact]
    public void Monthly_JanuaryThirtyFirst_ReturnsFebTwentyEight()
    {
        // NodaTime correctly clamps Jan 31 + 1 month to Feb 28 (non-leap year)
        var date = new LocalDate(2026, 1, 31);
        var next = RecurrenceCalculator.CalculateNext(date, RecurrenceFrequency.Monthly);
        Assert.Equal(new LocalDate(2026, 2, 28), next);
    }

    [Fact]
    public void Monthly_JanuaryThirtyFirst_LeapYear_ReturnsFebTwentyNine()
    {
        // 2028 is a leap year — Feb 29 exists
        var date = new LocalDate(2028, 1, 31);
        var next = RecurrenceCalculator.CalculateNext(date, RecurrenceFrequency.Monthly);
        Assert.Equal(new LocalDate(2028, 2, 29), next);
    }

    [Fact]
    public void Yearly_AddsOneYear()
    {
        var date = new LocalDate(2026, 3, 1);
        var next = RecurrenceCalculator.CalculateNext(date, RecurrenceFrequency.Yearly);
        Assert.Equal(new LocalDate(2027, 3, 1), next);
    }

    [Fact]
    public void InvalidFrequency_ThrowsArgumentOutOfRangeException()
    {
        var date = new LocalDate(2026, 3, 1);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => RecurrenceCalculator.CalculateNext(date, (RecurrenceFrequency)999));
    }
}
