using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AlphaVantageBacktester.core;
using AlphaVantageBacktester.data;
using AlphaVantageBacktester.structure;

namespace AlphaVantageBacktester.Gui;

public partial class MainWindow : Window
{
    private static readonly string[] PopularTickers = ["AAPL", "MSFT", "NVDA", "TSLA"];

    private static readonly string ReportsDirectory = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "AlphaVantageBacktester", "reports"));

    private string? _lastTicker;
    private Timeframe _lastTimeframe;
    private Strategy? _lastStrategy;
    private List<PriceBar>? _lastBars;
    private BacktestResult? _lastResult;

    public MainWindow()
    {
        InitializeComponent();
        PopularTickerComboBox.ItemsSource = PopularTickers;
        PopularTickerComboBox.SelectedIndex = 0;

        var clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        clock.Tick += (_, _) => ClockText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        clock.Start();
        ClockText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        StyleEquityPlot();
    }

    private void StyleEquityPlot()
    {
        var plot = EquityPlot.Plot;
        var paper = new ScottPlot.Color("#F4F3EF");
        var ink = new ScottPlot.Color("#1C1C1C");
        var rule = new ScottPlot.Color("#C9C7BC");

        plot.FigureBackground.Color = paper;
        plot.DataBackground.Color = paper;
        plot.Axes.Color(ink);
        plot.Grid.MajorLineColor = rule;
        plot.Font.Set("Menlo");
    }

    private void PlotEquityCurve(List<PriceBar> equity, string ticker)
    {
        var plot = EquityPlot.Plot;
        plot.Clear();

        double[] xs = Enumerable.Range(0, equity.Count).Select(i => (double)i).ToArray();
        double[] ys = equity.Select(bar => bar.Close).ToArray();

        var line = plot.Add.Scatter(xs, ys);
        line.LineWidth = 1.5f;
        line.MarkerSize = 0;
        line.Color = new ScottPlot.Color("#1C1C1C");

        plot.Title($"{ticker} equity curve");
        plot.Axes.Left.Label.Text = "value";
        plot.Axes.Bottom.Label.Text = "bar #";
        plot.Axes.AutoScale();

        EquityPlot.Refresh();
    }

    private async void OnRunButtonClick(object? sender, RoutedEventArgs e)
    {
        string? ticker = ResolveTicker();
        if (ticker is null)
        {
            AppendLog("Invalid choice.");
            return;
        }

        RunButton.IsEnabled = false;
        try
        {
            await RunBacktest(ticker);
        }
        finally
        {
            RunButton.IsEnabled = true;
        }
    }

    private string? ResolveTicker()
    {
        string custom = CustomTickerTextBox.Text?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(custom))
        {
            return custom;
        }

        return PopularTickerComboBox.SelectedItem as string;
    }

    private StrategyType ReadStrategyType()
    {
        return StrategyComboBox.SelectedIndex switch
        {
            1 => StrategyType.Regression,
            2 => StrategyType.RsiMeanReversion,
            3 => StrategyType.Macd,
            4 => StrategyType.Breakout,
            5 => StrategyType.BollingerBands,
            _ => StrategyType.Ema
        };
    }

    private Timeframe ReadTimeframe()
    {
        return TimeframeComboBox.SelectedIndex switch
        {
            1 => Timeframe.Weekly,
            _ => Timeframe.Daily
        };
    }

    private (int ShortPeriod, int LongPeriod, double PositionSkew) ReadStrategyParameters()
    {
        int shortPeriod = TryParseInt(ShortPeriodTextBox.Text, 12);
        int longPeriod = TryParseInt(LongPeriodTextBox.Text, 26);
        double positionSkew = TryParseDouble(PositionSkewTextBox.Text, 0.5);
        return (shortPeriod, longPeriod, positionSkew);
    }

    private static int TryParseInt(string? input, int fallback) =>
        int.TryParse(input?.Trim(), out int value) ? value : fallback;

    private static double TryParseDouble(string? input, double fallback) =>
        double.TryParse(input?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            ? value
            : fallback;

    private static readonly IBrush InkBrush = new SolidColorBrush(Color.Parse("#1C1C1C"));
    private static readonly IBrush MutedBrush = new SolidColorBrush(Color.Parse("#8A8A82"));
    private static readonly IBrush GoodBrush = new SolidColorBrush(Color.Parse("#2E7D32"));
    private static readonly IBrush OkayBrush = new SolidColorBrush(Color.Parse("#9A6B00"));
    private static readonly IBrush BadBrush = new SolidColorBrush(Color.Parse("#B3261E"));

    private async Task RunBacktest(string ticker)
    {
        ResultsPanel.Children.Clear();

        try
        {
            Timeframe timeframe = ReadTimeframe();

            bool fetched;
            string consoleOutput;
            (fetched, consoleOutput) = await CaptureConsoleAsync(() => Data.Fetcher(ticker, timeframe));
            AppendLog(consoleOutput);

            if (!fetched)
            {
                return;
            }

            List<PriceBar> bars;
            (bars, consoleOutput) = CaptureConsole(() => Data.Parser(ticker, timeframe));
            AppendLog(consoleOutput);

            if (bars.Count < 27)
            {
                AppendLog("Not enough historical data for the available strategies.");
                return;
            }

            StrategyType strategyType = ReadStrategyType();
            (int shortPeriod, int longPeriod, double positionSkew) = ReadStrategyParameters();
            var strategy = new Strategy(strategyType, shortPeriod, longPeriod, positionSkew);
            BacktestResult result = strategy.Backtest(bars);
            List<PriceBar> equity = result.EquityCurve;

            try
            {
                PlotEquityCurve(equity, ticker);
            }
            catch (Exception plotException)
            {
                AppendLog($"Chart error: {plotException}");
            }

            AddLine($"{ticker}: {bars.Count} daily bars ({bars[0].Date:yyyy-MM-dd} to {bars[^1].Date:yyyy-MM-dd})");
            AddLine($"Strategy:       {strategyType} ({strategy.ShortPeriod}/{strategy.LongPeriod})");
            AddLine($"Position skew:  {strategy.PositionSkew:P0}, trades: {result.Trades}");
            AddLine($"Starting value: {equity[0].Close:N2}");
            AddLine($"Ending value:   {equity[^1].Close:N2}");
            AddLine(string.Empty);

            AddMetricLine("Cumulative return", Metrics.CumulativeReturn(equity), value => value > 0,
                value => value >= 0, "{0:P2}");
            AddMetricLine("Volatility", Metrics.Volatility(equity), value => value <= 0.2, value => value <= 0.4,
                "{0:F6}");
            AddMetricLine("Sharpe", Metrics.Sharpe(equity), value => value >= 1, value => value >= 0, "{0:F6}");
            AddMetricLine("Sortino", Metrics.Sortino(equity), value => value >= 1, value => value >= 0, "{0:F6}");
            AddMetricLine("Calmar", Metrics.Calmar(equity), value => value >= 1, value => value >= 0, "{0:F6}");
            AddMetricLine("Max drawdown", Metrics.MaximumDrawdown(equity), value => value <= 0.1, value => value <= 0.2,
                "{0:P2}");

            AddTradeLog(result.TradeLog);

            _lastTicker = ticker;
            _lastTimeframe = timeframe;
            _lastStrategy = strategy;
            _lastBars = bars;
            _lastResult = result;
            ExportButton.IsEnabled = true;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException)
        {
            AddLine($"Unable to backtest {ticker}: {exception.Message}", BadBrush);
        }
    }

    private void OnExportButtonClick(object? sender, RoutedEventArgs e)
    {
        if (_lastTicker is null || _lastStrategy is null || _lastBars is null || _lastResult is null)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(ReportsDirectory);
            string baseName = ReportGenerator.BuildFileName(_lastTicker, _lastStrategy.Type, "txt")
                .Replace(".txt", string.Empty);

            string report = ReportGenerator.Generate(_lastTicker, _lastTimeframe, _lastStrategy, _lastBars,
                _lastResult);
            string reportPath = Path.Combine(ReportsDirectory, $"{baseName}.txt");
            File.WriteAllText(reportPath, report);

            string chartPath = Path.Combine(ReportsDirectory, $"{baseName}.png");
            EquityPlot.Plot.SavePng(chartPath, 900, 500);

            AppendLog($"Report saved to {reportPath}");
            AppendLog($"Chart saved to {chartPath}");
        }
        catch (IOException exception)
        {
            AppendLog($"Export failed: {exception.Message}");
        }
    }

    private void AddLine(string text, IBrush? brush = null)
    {
        ResultsPanel.Children.Add(new TextBlock
        {
            Text = text,
            Foreground = brush ?? InkBrush,
            FontSize = 13
        });
    }

    private void AddMetricLine(string name, double value, Func<double, bool> good, Func<double, bool> okay,
        string format)
    {
        IBrush brush = good(value) ? GoodBrush : okay(value) ? OkayBrush : BadBrush;
        AddLine($"{name,-15}: {string.Format(format, value)}", brush);
    }

    private void AddTradeLog(List<Trade> tradeLog, int maxRows = 10)
    {
        int shown = Math.Min(maxRows, tradeLog.Count);
        AddLine(string.Empty);
        AddLine($"Trade log ({tradeLog.Count} trades, last {shown} shown):");
        AddLine($"{"Date",-12} {"Side",-5} {"Shares",10} {"Price",10} {"Value",10}");

        foreach (Trade trade in tradeLog.TakeLast(maxRows))
        {
            string side = trade.Shares >= 0 ? "BUY" : "SELL";
            IBrush brush = trade.Shares >= 0 ? GoodBrush : BadBrush;
            AddLine(
                $"{trade.Date:yyyy-MM-dd} {side,-5} {Math.Abs(trade.Shares),10:N2} {trade.Price,10:N2} {Math.Abs(trade.Value),10:N2}",
                brush);
        }
    }

    private void AppendLog(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        LogTextBlock.Text = string.IsNullOrEmpty(LogTextBlock.Text)
            ? text.TrimEnd()
            : $"{LogTextBlock.Text}\n{text.TrimEnd()}";
    }

    private static async Task<(T Result, string Output)> CaptureConsoleAsync<T>(Func<Task<T>> action)
    {
        TextWriter original = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            T result = await action();
            return (result, writer.ToString());
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    private static (T Result, string Output) CaptureConsole<T>(Func<T> action)
    {
        TextWriter original = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            T result = action();
            return (result, writer.ToString());
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}
