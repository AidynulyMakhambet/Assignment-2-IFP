using System;
using System.Threading;

Console.WriteLine("Main program ready.");

public readonly record struct CallRecord
{
    public string RecordId { get; }
    public string DestinationCountry { get; }
    public double DurationMinutes { get; }
    public bool IsRoaming { get; }

    public CallRecord(string recordId, string destinationCountry, double durationMinutes, bool isRoaming)
    {
        // Explicit constructor validation
        if (string.IsNullOrWhiteSpace(recordId))    
            throw new ArgumentException("RecordId cannot be null or empty.");
        if (string.IsNullOrWhiteSpace(destinationCountry)) 
            throw new ArgumentException("DestinationCountry cannot be null or empty.");
        if (double.IsNaN(durationMinutes) || double.IsInfinity(durationMinutes) || durationMinutes < 0 || durationMinutes > 10000)
            throw new ArgumentException("Invalid call duration.");

        RecordId = recordId;
        DestinationCountry = destinationCountry;
        DurationMinutes = durationMinutes;
        IsRoaming = isRoaming;
    }
}

public static class CallPricing
{
    public static decimal CalculateCost(in CallRecord record)
    {
        // Defensive check against default(CallRecord) which bypasses the constructor validation
        if (string.IsNullOrWhiteSpace(record.RecordId) ||
            string.IsNullOrWhiteSpace(record.DestinationCountry) ||
            double.IsNaN(record.DurationMinutes) ||
            double.IsInfinity(record.DurationMinutes) ||
            record.DurationMinutes < 0 ||
            record.DurationMinutes > 10000)
        {
            throw new ArgumentException("Invalid record state (possibly initialized via default).");
        }

        // Single pure switch expression for tariff selection
        decimal cost = record switch
        {
            { IsRoaming: true, DestinationCountry: "KZ", DurationMinutes: < 1.0 } => 50.00m,
            { IsRoaming: false, DestinationCountry: "KZ" } => 15.00m * (decimal)record.DurationMinutes,
            { IsRoaming: true, DurationMinutes: >= 10.0 } => 120.00m * (decimal)record.DurationMinutes,
            _ => 45.00m * (decimal)record.DurationMinutes
        };

        return Math.Round(cost, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal ProcessCallsSequential(CallRecord[] records)
    {
        if (records == null) throw new ArgumentNullException(nameof(records));
        decimal total = 0;
        foreach (var record in records)
        {
            total += CalculateCost(in record);
        }
        return total;
    }

    public static decimal ProcessCallsParallel(CallRecord[] records)
    {
        if (records == null || records.Length % 2 != 0)
            throw new ArgumentException("Array must be non-null and have an even length.");
        if (records.Length == 0) return 0;

        int mid = records.Length / 2;
        
        // Splitting the array using C# range syntax
        CallRecord[] part1 = records[..mid];
        CallRecord[] part2 = records[mid..];

        // Isolated output arrays to prevent shared mutable state
        decimal[] out1 = new decimal[part1.Length];
        decimal[] out2 = new decimal[part2.Length];

        Exception? ex1 = null;
        Exception? ex2 = null;

        // Creating exactly two distinct thread instances
        Thread t1 = new Thread(() =>
        {
            try { for (int i = 0; i < part1.Length; i++) out1[i] = CalculateCost(in part1[i]); }
            catch (Exception ex) { ex1 = ex; }
        });

        Thread t2 = new Thread(() =>
        {
            try { for (int i = 0; i < part2.Length; i++) out2[i] = CalculateCost(in part2[i]); }
            catch (Exception ex) { ex2 = ex; }
        });

        t1.Start(); t2.Start();
        t1.Join(); t2.Join();

        if (ex1 != null || ex2 != null)
        {
            throw new AggregateException("One or both worker threads failed.", 
                ex1 ?? new Exception(), ex2 ?? new Exception());
        }

        // Sequential aggregation after workers have joined
        decimal total = 0;
        foreach (var val in out1) total += val;
        foreach (var val in out2) total += val;

        return total;
    }
}