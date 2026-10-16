using System;
using System.Collections.Generic;
using AlphaVantageBacktester.data;

namespace AlphaVantageBacktester;

class Program
{
    public static async Task Main(string[] args)
    {
        await Data.Fetcher("NVDA");
        List<PriceBar> bars = Data.Parser("NVDA");
        Console.WriteLine($"Parsed {bars.Count} bars. Latest: {bars[^1]}");
    }
}

