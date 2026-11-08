using AlphaVantageBacktester.structure;

namespace AlphaVantageBacktester.core;

public enum StrategyType
{
    Ema,
    Regression,
    RsiMeanReversion
}

public sealed record BacktestResult(List<PriceBar> EquityCurve, int Trades);

public class Strategy
{
    public Strategy(StrategyType type, int shortPeriod = 12, int longPeriod = 26, double positionSkew = 0.5)
    {
        if (shortPeriod < 1 || longPeriod <= shortPeriod)
        {
            throw new ArgumentException("The long period must be greater than the short period.");
        }

        if (positionSkew is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(positionSkew), "Position skew must be between zero and one.");
        }

        Type = type;
        ShortPeriod = shortPeriod;
        LongPeriod = longPeriod;
        PositionSkew = positionSkew;
    }

    public StrategyType Type { get; }
    public int ShortPeriod { get; }
    public int LongPeriod { get; }
    public double PositionSkew { get; }

    public BacktestResult Backtest(IReadOnlyList<PriceBar> bars)
    {
        ArgumentNullException.ThrowIfNull(bars);
        int warmup = Type == StrategyType.RsiMeanReversion ? LongPeriod : LongPeriod + 1;
        if (bars.Count < warmup)
        {
            throw new ArgumentException($"At least {warmup} bars are required.", nameof(bars));
        }

        const double initialCapital = 10_000d;
        double cash = initialCapital;
        double shares = 0;
        int trades = 0;
        var equityCurve = new List<PriceBar>(bars.Count);

        for (int index = 0; index < bars.Count; index++)
        {
            double equity = cash + shares * bars[index].Close;
            if (index >= warmup - 1)
            {
                double desiredWeight = TargetWeight(bars, index);
                double currentWeight = equity == 0 ? 0 : shares * bars[index].Close / equity;
                double targetWeight = currentWeight + (desiredWeight - currentWeight) * PositionSkew;
                double targetShares = equity * targetWeight / bars[index].Close;
                double shareChange = targetShares - shares;

                if (Math.Abs(shareChange) > 0.000001)
                {
                    cash -= shareChange * bars[index].Close;
                    shares = targetShares;
                    trades++;
                    equity = cash + shares * bars[index].Close;
                }
            }

            equityCurve.Add(bars[index] with
            {
                Open = equity,
                High = equity,
                Low = equity,
                Close = equity
            });
        }

        return new BacktestResult(equityCurve, trades);
    }

    private double TargetWeight(IReadOnlyList<PriceBar> bars, int index)
    {
        return Type switch
        {
            StrategyType.Ema => EmaTargetWeight(bars, index),
            StrategyType.Regression => RegressionTargetWeight(bars, index),
            StrategyType.RsiMeanReversion => RsiTargetWeight(bars, index),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private double EmaTargetWeight(IReadOnlyList<PriceBar> bars, int index)
    {
        double shortEma = ExponentialMovingAverage(bars, index, ShortPeriod);
        double longEma = ExponentialMovingAverage(bars, index, LongPeriod);
        double strength = Math.Min(1, Math.Abs(shortEma - longEma) / longEma * 20);
        return Math.Sign(shortEma - longEma) * strength;
    }

    private double RegressionTargetWeight(IReadOnlyList<PriceBar> bars, int index)
    {
        double slope = RegressionSlope(bars, index, LongPeriod);
        double normalizedSlope = slope / bars[index].Close * LongPeriod;
        return Math.Clamp(normalizedSlope * 20, -1, 1);
    }

    private double RsiTargetWeight(IReadOnlyList<PriceBar> bars, int index)
    {
        double rsi = RelativeStrengthIndex(bars, index, ShortPeriod);
        return rsi < 30
            ? Math.Min(1, (30 - rsi) / 20)
            : rsi > 70 ? -Math.Min(1, (rsi - 70) / 20) : 0;
    }

    private static double ExponentialMovingAverage(IReadOnlyList<PriceBar> bars, int endIndex, int period)
    {
        int startIndex = endIndex - period + 1;
        double ema = bars[startIndex].Close;
        double multiplier = 2d / (period + 1);

        for (int index = startIndex + 1; index <= endIndex; index++)
        {
            ema += (bars[index].Close - ema) * multiplier;
        }

        return ema;
    }

    private static double RegressionSlope(IReadOnlyList<PriceBar> bars, int endIndex, int period)
    {
        double xMean = (period - 1) / 2d;
        double yMean = 0;
        for (int offset = 0; offset < period; offset++)
        {
            yMean += bars[endIndex - period + 1 + offset].Close;
        }
        yMean /= period;

        double numerator = 0;
        double denominator = 0;
        for (int offset = 0; offset < period; offset++)
        {
            double xDifference = offset - xMean;
            numerator += xDifference * (bars[endIndex - period + 1 + offset].Close - yMean);
            denominator += xDifference * xDifference;
        }

        return numerator / denominator;
    }

    private static double RelativeStrengthIndex(IReadOnlyList<PriceBar> bars, int endIndex, int period)
    {
        double gains = 0;
        double losses = 0;
        for (int index = endIndex - period + 1; index <= endIndex; index++)
        {
            double change = bars[index].Close - bars[index - 1].Close;
            if (change >= 0)
            {
                gains += change;
            }
            else
            {
                losses -= change;
            }
        }

        if (losses == 0)
        {
            return 100;
        }

        double relativeStrength = gains / losses;
        return 100 - 100 / (1 + relativeStrength);
    }
}
