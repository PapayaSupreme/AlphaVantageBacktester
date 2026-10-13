using System;
using AlphaVantageBacktester.data;

namespace AlphaVantageBacktester;

class Program
{
    public static async Task Main(string[] args)
    {
        await Data.Fetcher("AAPL");
        Data.Parser("AAPL");
    }
}

