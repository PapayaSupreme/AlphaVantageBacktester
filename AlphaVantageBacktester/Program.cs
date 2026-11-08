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

            await RunBacktest(ticker);
        }
    }

    private static string? ReadTicker()
    {
        Console.Write("Enter ticker: ");
        string ticker = Console.ReadLine()?.Trim().ToUpperInvariant() ?? string.Empty;
        return string.IsNullOrWhiteSpace(ticker) ? null : ticker;
    }

    private static async Task RunBacktest(string ticker)
    {
        try
        {
            if (!await Data.Fetcher(ticker))
            {
                return;
            }

            List<PriceBar> bars = Data.Parser(ticker);
            if (bars.Count < 27)
            {
                Console.WriteLine("Not enough historical data for the available strategies.");
                return;
            }

            StrategyType strategyType = ReadStrategyType();
            var strategy = new Strategy(strategyType);
            BacktestResult result = strategy.Backtest(bars);
            List<PriceBar> equity = result.EquityCurve;

            Console.WriteLine($"\n{ticker}: {bars.Count} daily bars ({bars[0].Date:yyyy-MM-dd} to {bars[^1].Date:yyyy-MM-dd})");
            Console.WriteLine($"Strategy:       {strategyType} ({strategy.ShortPeriod}/{strategy.LongPeriod})");
            Console.WriteLine($"Position skew:  {strategy.PositionSkew:P0}, trades: {result.Trades}");
            Console.WriteLine($"Starting value: {equity[0].Close:N2}");
            Console.WriteLine($"Ending value:   {equity[^1].Close:N2}");
            PrintMetric("Volatility", Metrics.Volatility(equity), value => value <= 0.2, value => value <= 0.4,
                "{0:F6}");
            PrintMetric("Sharpe", Metrics.Sharpe(equity), value => value >= 1, value => value >= 0, "{0:F6}");
            PrintMetric("Sortino", Metrics.Sortino(equity), value => value >= 1, value => value >= 0, "{0:F6}");
            PrintMetric("Calmar", Metrics.Calmar(equity), value => value >= 1, value => value >= 0, "{0:F6}");
            PrintMetric("Max drawdown", Metrics.MaximumDrawdown(equity), value => value <= 0.1, value => value <= 0.2,
                "{0:P2}");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException)
        {
            Console.WriteLine($"Unable to backtest {ticker}: {exception.Message}");
        }
    }

    private static StrategyType ReadStrategyType()
    {
        Console.WriteLine("\nChoose a strategy:");
        Console.WriteLine("1. EMA crossover (trend following)");
        Console.WriteLine("2. Linear regression (trend following)");
        Console.WriteLine("3. RSI mean reversion");
        Console.Write("Choice [1]: ");

        return Console.ReadLine()?.Trim() switch
        {
            "2" => StrategyType.Regression,
            "3" => StrategyType.RsiMeanReversion,
            _ => StrategyType.Ema
        };
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
