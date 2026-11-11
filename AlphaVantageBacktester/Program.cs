using System.Globalization;
using AlphaVantageBacktester.core;
using AlphaVantageBacktester.data;
using AlphaVantageBacktester.structure;

namespace AlphaVantageBacktester;

class Program
{
    public static async Task Main(string[] args)
    {
        string[] popularTickers = ["AAPL", "MSFT", "NVDA", "TSLA"];

        while (true)
        {
            Console.WriteLine("\n=== Alpha Vantage Backtester ===");
            for (int index = 0; index < popularTickers.Length; index++)
            {
                Console.WriteLine($"{index + 1}. {popularTickers[index]}");
            }
            Console.WriteLine("5. Enter another ticker");
            Console.WriteLine("0. Exit");
            Console.Write("Choose a ticker: ");

            string? choice = Console.ReadLine()?.Trim();
            if (choice == "0")
            {
                return;
            }

            string? ticker = choice switch
            {
                "1" or "2" or "3" or "4" => popularTickers[int.Parse(choice) - 1],
                "5" => ReadTicker(),
                _ => null
            };

            if (ticker is null)
            {
                Console.WriteLine("Invalid choice.");
                continue;
            }

            Timeframe timeframe = ReadTimeframe();
            await RunBacktest(ticker, timeframe);
        }
    }

    private static Timeframe ReadTimeframe()
    {
        Console.WriteLine("\nChoose a timeframe:");
        Console.WriteLine("1. Daily");
        Console.WriteLine("2. Weekly");
        Console.Write("Choice [1]: ");

        return Console.ReadLine()?.Trim() switch
        {
            "2" => Timeframe.Weekly,
            _ => Timeframe.Daily
        };
    }

    private static string? ReadTicker()
    {
        Console.Write("Enter ticker: ");
        string ticker = Console.ReadLine()?.Trim().ToUpperInvariant() ?? string.Empty;
        return string.IsNullOrWhiteSpace(ticker) ? null : ticker;
    }

    private static async Task RunBacktest(string ticker, Timeframe timeframe)
    {
        try
        {
            if (!await Data.Fetcher(ticker, timeframe))
            {
                return;
            }

            List<PriceBar> bars = Data.Parser(ticker, timeframe);
            if (bars.Count < 27)
            {
                Console.WriteLine("Not enough historical data for the available strategies.");
                return;
            }

            StrategyType strategyType = ReadStrategyType();
            (int shortPeriod, int longPeriod, double positionSkew) = ReadStrategyParameters();
            var strategy = new Strategy(strategyType, shortPeriod, longPeriod, positionSkew);
            BacktestResult result = strategy.Backtest(bars);
            List<PriceBar> equity = result.EquityCurve;

            Console.WriteLine($"\n{ticker}: {bars.Count} daily bars ({bars[0].Date:yyyy-MM-dd} to {bars[^1].Date:yyyy-MM-dd})");
            Console.WriteLine($"Strategy:       {strategyType} ({strategy.ShortPeriod}/{strategy.LongPeriod})");
            Console.WriteLine($"Position skew:  {strategy.PositionSkew:P0}, trades: {result.Trades}");
            Console.WriteLine($"Starting value: {equity[0].Close:N2}");
            Console.WriteLine($"Ending value:   {equity[^1].Close:N2}");
            PrintMetric("Cumulative return", Metrics.CumulativeReturn(equity), value => value > 0, value => value >= 0,
                "{0:P2}");
            PrintMetric("Volatility", Metrics.Volatility(equity), value => value <= 0.2, value => value <= 0.4,
                "{0:F6}");
            PrintMetric("Sharpe", Metrics.Sharpe(equity), value => value >= 1, value => value >= 0, "{0:F6}");
            PrintMetric("Sortino", Metrics.Sortino(equity), value => value >= 1, value => value >= 0, "{0:F6}");
            PrintMetric("Calmar", Metrics.Calmar(equity), value => value >= 1, value => value >= 0, "{0:F6}");
            PrintMetric("Max drawdown", Metrics.MaximumDrawdown(equity), value => value <= 0.1, value => value <= 0.2,
                "{0:P2}");

            PrintTradeLog(result.TradeLog);

            SaveReport(ticker, timeframe, strategy, bars, result);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException)
        {
            Console.WriteLine($"Unable to backtest {ticker}: {exception.Message}");
        }
    }

    private static readonly string ReportsDirectory = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "reports"));

    private static void SaveReport(string ticker, Timeframe timeframe, Strategy strategy, List<PriceBar> bars,
        BacktestResult result)
    {
        Directory.CreateDirectory(ReportsDirectory);
        string report = ReportGenerator.Generate(ticker, timeframe, strategy, bars, result);
        string fileName = ReportGenerator.BuildFileName(ticker, strategy.Type, "txt");
        string filePath = Path.Combine(ReportsDirectory, fileName);
        File.WriteAllText(filePath, report);
        Console.WriteLine($"\nReport saved to {filePath}");
    }

    private static StrategyType ReadStrategyType()
    {
        Console.WriteLine("\nChoose a strategy:");
        Console.WriteLine("1. EMA crossover (trend following)");
        Console.WriteLine("2. Linear regression (trend following)");
        Console.WriteLine("3. RSI mean reversion");
        Console.WriteLine("4. MACD momentum");
        Console.WriteLine("5. Breakout (support/resistance)");
        Console.WriteLine("6. Bollinger Bands mean reversion");
        Console.Write("Choice [1]: ");

        return Console.ReadLine()?.Trim() switch
        {
            "2" => StrategyType.Regression,
            "3" => StrategyType.RsiMeanReversion,
            "4" => StrategyType.Macd,
            "5" => StrategyType.Breakout,
            "6" => StrategyType.BollingerBands,
            _ => StrategyType.Ema
        };
    }

    private static (int ShortPeriod, int LongPeriod, double PositionSkew) ReadStrategyParameters()
    {
        Console.WriteLine("\nCustomize strategy parameters (press Enter to keep the default):");

        Console.Write("Short period [12]: ");
        int shortPeriod = TryParseInt(Console.ReadLine(), 12);

        Console.Write("Long period [26]: ");
        int longPeriod = TryParseInt(Console.ReadLine(), 26);

        Console.Write("Position skew, 0-1 [0.5]: ");
        double positionSkew = TryParseDouble(Console.ReadLine(), 0.5);

        return (shortPeriod, longPeriod, positionSkew);
    }

    private static int TryParseInt(string? input, int fallback) =>
        int.TryParse(input?.Trim(), out int value) ? value : fallback;

    private static double TryParseDouble(string? input, double fallback) =>
        double.TryParse(input?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            ? value
            : fallback;

    private static void PrintTradeLog(List<Trade> tradeLog, int maxRows = 10)
    {
        Console.WriteLine($"\nTrade log ({tradeLog.Count} trades, last {Math.Min(maxRows, tradeLog.Count)} shown):");
        Console.WriteLine($"{"Date",-12} {"Side",-5} {"Shares",12} {"Price",10} {"Value",12}");

        foreach (Trade trade in tradeLog.TakeLast(maxRows))
        {
            string side = trade.Shares >= 0 ? "BUY" : "SELL";
            Console.WriteLine(
                $"{trade.Date:yyyy-MM-dd} {side,-5} {Math.Abs(trade.Shares),12:N2} {trade.Price,10:N2} {Math.Abs(trade.Value),12:N2}");
        }
    }

    private static void PrintMetric(string name, double value, Func<double, bool> good, Func<double, bool> okay,
        string format)
    {
        ConsoleColor color = good(value)
            ? ConsoleColor.Green
            : okay(value) ? ConsoleColor.DarkYellow : ConsoleColor.Red;

        Console.ForegroundColor = color;
        Console.WriteLine($"{name,-15}: {string.Format(format, value)}");
        Console.ResetColor();
    }
}
