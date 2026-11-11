using System.Text;
using AlphaVantageBacktester.structure;

namespace AlphaVantageBacktester.core;

public static class ReportGenerator
{
    public static string Generate(string ticker, Timeframe timeframe, Strategy strategy, List<PriceBar> bars,
        BacktestResult result)
    {
        List<PriceBar> equity = result.EquityCurve;
        var builder = new StringBuilder();

        builder.AppendLine("=== Alpha Vantage Backtester Report ===");
        builder.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine();
        builder.AppendLine($"Ticker:         {ticker}");
        builder.AppendLine($"Timeframe:      {timeframe}");
        builder.AppendLine($"Strategy:       {strategy.Type} ({strategy.ShortPeriod}/{strategy.LongPeriod})");
        builder.AppendLine($"Position skew:  {strategy.PositionSkew:P0}");
        builder.AppendLine($"Bars:           {bars.Count} ({bars[0].Date:yyyy-MM-dd} to {bars[^1].Date:yyyy-MM-dd})");
        builder.AppendLine($"Trades:         {result.Trades}");
        builder.AppendLine();

        builder.AppendLine("--- Performance ---");
        builder.AppendLine($"Starting value:     {equity[0].Close,12:N2}");
        builder.AppendLine($"Ending value:       {equity[^1].Close,12:N2}");
        builder.AppendLine($"Cumulative return:  {Metrics.CumulativeReturn(equity),12:P2}");
        builder.AppendLine($"Volatility:         {Metrics.Volatility(equity),12:F6}");
        builder.AppendLine($"Sharpe:             {Metrics.Sharpe(equity),12:F6}");
        builder.AppendLine($"Sortino:            {Metrics.Sortino(equity),12:F6}");
        builder.AppendLine($"Calmar:             {Metrics.Calmar(equity),12:F6}");
        builder.AppendLine($"Max drawdown:       {Metrics.MaximumDrawdown(equity),12:P2}");
        builder.AppendLine();

        builder.AppendLine($"--- Trade log ({result.TradeLog.Count} trades) ---");
        builder.AppendLine($"{"Date",-12} {"Side",-5} {"Shares",12} {"Price",10} {"Value",12}");
        foreach (Trade trade in result.TradeLog)
        {
            string side = trade.Shares >= 0 ? "BUY" : "SELL";
            builder.AppendLine(
                $"{trade.Date:yyyy-MM-dd} {side,-5} {Math.Abs(trade.Shares),12:N2} {trade.Price,10:N2} {Math.Abs(trade.Value),12:N2}");
        }

        return builder.ToString();
    }

    public static string BuildFileName(string ticker, StrategyType strategyType, string extension)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"{ticker}_{strategyType}_{timestamp}.{extension}";
    }
}
