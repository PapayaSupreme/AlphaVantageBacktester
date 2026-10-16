using System;
using System.Collections.Generic;
using AlphaVantageBacktester.core;
using AlphaVantageBacktester.data;
using AlphaVantageBacktester.structure;

namespace AlphaVantageBacktester;

class Program
{
    public static async Task Main(string[] args)
    {
        await Data.Fetcher("NVDA");
        List<PriceBar> bars = Data.Parser("NVDA");
        Console.WriteLine($"Parsed {bars.Count} bars. Latest: {bars[^1]}");
        Console.WriteLine($"Volatility: {Metrics.Volatility(bars):F6}");
        Console.WriteLine($"Sharpe: {Metrics.Sharpe(bars):F6}");
        Console.WriteLine($"Sortino: {Metrics.Sortino(bars):F6}");
        Console.WriteLine($"Calmar: {Metrics.Calmar(bars):F6}");
    }
}
