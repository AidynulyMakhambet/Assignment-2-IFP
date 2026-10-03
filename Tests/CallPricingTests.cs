using System;
using Xunit;

public class CallPricingTests
{
    // Group A: Tariff correctness
    [Theory]
    [InlineData("KZ", false, 4.0, 60.00)]
    [InlineData("KZ", true, 0.5, 50.00)]
    [InlineData("US", true, 10.0, 1200.00)]
    [InlineData("DE", false, 3.0, 135.00)]
    [InlineData("XX", false, 2.0, 90.00)]
    [InlineData("KZ", true, 1.0, 45.00)]
    public void CalculateCost_ValidTariffs_ReturnsExpected(string country, bool roaming, double duration, decimal expectedCost)
    {
        var record = new CallRecord("ID1", country, duration, roaming);
        decimal cost = CallPricing.CalculateCost(in record);
        Assert.Equal(expectedCost, cost);
    }

    // Group B: Invalid inputs and boundaries
    [Fact]
    public void Constructor_InvalidInputs_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new CallRecord("", "KZ", 5.0, false));
        Assert.Throws<ArgumentException>(() => new CallRecord("ID", "", 5.0, false));
        Assert.Throws<ArgumentException>(() => new CallRecord("ID", "KZ", -1.0, false));
        Assert.Throws<ArgumentException>(() => new CallRecord("ID", "KZ", double.NaN, false));
        Assert.Throws<ArgumentException>(() => new CallRecord("ID", "KZ", double.PositiveInfinity, false));
        Assert.Throws<ArgumentException>(() => new CallRecord("ID", "KZ", 10001.0, false));
    }

    [Fact]
    public void CalculateCost_DefaultStruct_ThrowsArgumentException()
    {
        CallRecord defaultRecord = default;
        Assert.Throws<ArgumentException>(() => CallPricing.CalculateCost(in defaultRecord));
    }

    [Fact]
    public void CalculateCost_BoundaryAndZeroDuration_AppliesCorrectly()
    {
        var zeroDurationRoamingKZ = new CallRecord("ID", "KZ", 0.0, true);
        Assert.Equal(50.00m, CallPricing.CalculateCost(in zeroDurationRoamingKZ)); // Boundary < 1.0

        var closeToBoundary = new CallRecord("ID", "KZ", 0.9999, true);
        Assert.Equal(50.00m, CallPricing.CalculateCost(in closeToBoundary));
    }

    // Group C: Sequential-parallel agreement
    [Fact]
    public void Parallel_MatchesSequential_ForLargeArray()
    {
        var records = new CallRecord[1000];
        for (int i = 0; i < records.Length; i++)
        {
            records[i] = new CallRecord($"ID{i}", i % 2 == 0 ? "KZ" : "US", i % 15, i % 3 == 0);
        }

        decimal sequentialTotal = CallPricing.ProcessCallsSequential(records);

        for (int i = 0; i < 100; i++)
        {
            decimal parallelTotal = CallPricing.ProcessCallsParallel(records);
            Assert.Equal(sequentialTotal, parallelTotal); // Decimal equality check
        }
    }
}