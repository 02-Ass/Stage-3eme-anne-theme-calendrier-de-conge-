using VacationPlanner.Api.Services;
using Xunit;

namespace VacationPlanner.Api.Tests.Unit;

public class VacationOverlapCheckerTests
{
    [Fact]
    public void Overlaps_IdenticalRanges_ReturnsTrue()
    {
        var start = new DateOnly(2026, 7, 1);
        var end = new DateOnly(2026, 7, 10);

        Assert.True(VacationOverlapChecker.Overlaps(start, end, start, end));
    }

    [Fact]
    public void Overlaps_CompletelySeparateRanges_ReturnsFalse()
    {
        var a = (start: new DateOnly(2026, 7, 1), end: new DateOnly(2026, 7, 10));
        var b = (start: new DateOnly(2026, 8, 1), end: new DateOnly(2026, 8, 10));

        Assert.False(VacationOverlapChecker.Overlaps(a.start, a.end, b.start, b.end));
    }

    [Fact]
    public void Overlaps_AdjacentButNotTouching_ReturnsFalse()
    {
        // Отпуск А заканчивается 10-го, Б начинается 11-го — не пересекаются.
        var a = (start: new DateOnly(2026, 7, 1), end: new DateOnly(2026, 7, 10));
        var b = (start: new DateOnly(2026, 7, 11), end: new DateOnly(2026, 7, 20));

        Assert.False(VacationOverlapChecker.Overlaps(a.start, a.end, b.start, b.end));
    }

    [Fact]
    public void Overlaps_SharedBoundaryDay_ReturnsTrue()
    {
        // Отпуск А заканчивается 10-го, Б начинается ТОЖЕ 10-го — граничный
        // день общий, это пересечение (границы включительные).
        var a = (start: new DateOnly(2026, 7, 1), end: new DateOnly(2026, 7, 10));
        var b = (start: new DateOnly(2026, 7, 10), end: new DateOnly(2026, 7, 20));

        Assert.True(VacationOverlapChecker.Overlaps(a.start, a.end, b.start, b.end));
    }

    [Fact]
    public void Overlaps_OneRangeFullyInsideAnother_ReturnsTrue()
    {
        var outer = (start: new DateOnly(2026, 7, 1), end: new DateOnly(2026, 7, 31));
        var inner = (start: new DateOnly(2026, 7, 10), end: new DateOnly(2026, 7, 15));

        Assert.True(VacationOverlapChecker.Overlaps(outer.start, outer.end, inner.start, inner.end));
        Assert.True(VacationOverlapChecker.Overlaps(inner.start, inner.end, outer.start, outer.end));
    }

    [Fact]
    public void Overlaps_PartialOverlapAtStart_ReturnsTrue()
    {
        var a = (start: new DateOnly(2026, 7, 5), end: new DateOnly(2026, 7, 15));
        var b = (start: new DateOnly(2026, 7, 1), end: new DateOnly(2026, 7, 6));

        Assert.True(VacationOverlapChecker.Overlaps(a.start, a.end, b.start, b.end));
    }

    [Fact]
    public void Overlaps_SingleDayVacations_SameDay_ReturnsTrue()
    {
        var day = new DateOnly(2026, 7, 1);

        Assert.True(VacationOverlapChecker.Overlaps(day, day, day, day));
    }
}
