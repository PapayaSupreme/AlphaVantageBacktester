namespace AlphaVantageBacktester.core;

using AlphaVantageBacktester.structure;

public static class Metrics
{
    private const double TradingDaysPerYear = 252d;

    public static double Volatility(List<PriceBar> bars)
    {
        var returns = GetReturns(bars);
        return StandardDeviation(returns) * Math.Sqrt(TradingDaysPerYear);
    }

    public static double Sharpe(List<PriceBar> bars)
    {
        var returns = GetReturns(bars);
        var standardDeviation = StandardDeviation(returns);

        return standardDeviation == 0
            ? (returns.Average() > 0 ? double.PositiveInfinity : 0)
            : returns.Average() / standardDeviation * Math.Sqrt(TradingDaysPerYear);
    }

    public static double Sortino(List<PriceBar> bars)
    {
        var returns = GetReturns(bars);
        var downsideDeviation = Math.Sqrt(returns.Select(returnValue =>
            returnValue < 0 ? returnValue * returnValue : 0).Average());

        return downsideDeviation == 0
            ? (returns.Average() > 0 ? double.PositiveInfinity : 0)
            : returns.Average() / downsideDeviation * Math.Sqrt(TradingDaysPerYear);
    }

    public static double Calmar(List<PriceBar> bars)
    {
        ValidateBars(bars);

        var firstBar = bars[0];
        var lastBar = bars[^1];
        var elapsedYears = (lastBar.Date - firstBar.Date).TotalDays / TradingDaysPerYear;
        var annualizedReturn = elapsedYears > 0
            ? Math.Pow(lastBar.Close / firstBar.Close, 1 / elapsedYears) - 1
            : 0;
        var maximumDrawdown = MaximumDrawdown(bars);

        return maximumDrawdown == 0
            ? (annualizedReturn > 0 ? double.PositiveInfinity : 0)
            : annualizedReturn / maximumDrawdown;
    }

    private static List<double> GetReturns(List<PriceBar> bars)
    {
        ValidateBars(bars);

        var returns = new List<double>(bars.Count - 1);
        for (var i = 1; i < bars.Count; i++)
        {
            returns.Add(bars[i].Close / bars[i - 1].Close - 1);
        }

        return returns;
    }

    private static double MaximumDrawdown(List<PriceBar> bars)
    {
        var peak = bars[0].Close;
        var maximumDrawdown = 0d;

        foreach (var bar in bars)
        {
            peak = Math.Max(peak, bar.Close);
            maximumDrawdown = Math.Max(maximumDrawdown, (peak - bar.Close) / peak);
        }

        return maximumDrawdown;
    }

    private static double StandardDeviation(IReadOnlyList<double> values)
    {
        var mean = values.Average();
        return Math.Sqrt(values.Sum(value => Math.Pow(value - mean, 2)) / values.Count);
    }

    private static void ValidateBars(List<PriceBar> bars)
    {
        ArgumentNullException.ThrowIfNull(bars);
        if (bars.Count < 2)
        {
            throw new ArgumentException("At least two price bars are required.", nameof(bars));
        }

        if (bars.Any(bar => !double.IsFinite(bar.Close) || bar.Close <= 0))
        {
            throw new ArgumentException("All closing prices must be finite and greater than zero.", nameof(bars));
        }
    }
}