using AlphaVantageBacktester.structure;

namespace AlphaVantageBacktester.data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using utilities;

public class Data
{
    private static readonly string DataDirectory = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data"));

    private static readonly TimeSpan CacheMaxAge = TimeSpan.FromHours(24);

    public static async Task<bool> Fetcher(string symbol, Timeframe timeframe = Timeframe.Daily)
    {
        symbol = NormalizeSymbol(symbol);
        string filePath = GetFilePath(symbol, timeframe);

        if (File.Exists(filePath) && DateTime.Now - File.GetLastWriteTime(filePath) < CacheMaxAge)
        {
            Console.WriteLine($"Using cached data for {symbol} ({filePath})");
            return true;
        }

        string apiKey = Env.GetApiKey();
        Directory.CreateDirectory(DataDirectory);

        // Construct the API URL
        string function = timeframe == Timeframe.Weekly ? "TIME_SERIES_WEEKLY" : "TIME_SERIES_DAILY";
        string url =
            $"https://www.alphavantage.co/query?function={function}&symbol={symbol}&apikey={apiKey}&datatype=csv";

        try
        {
            using (HttpClient client = new HttpClient()) 
            { 
                // Send the HTTP GET request 
                HttpResponseMessage response = await 
                    client.GetAsync(url); 
 
                if (response.IsSuccessStatusCode) 
                { 
                    // Get the response content as a string 
                    string csvData = await 
                        response.Content.ReadAsStringAsync(); 
 
                    // Save the CSV data to a file
                    if (string.IsNullOrWhiteSpace(csvData) || csvData.StartsWith("{", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("The API returned an error instead of CSV data.");
                    }

                    File.WriteAllText(filePath, csvData);
 
                    Console.WriteLine($"Data successfully fetched and saved to {filePath}"); 
                    return true;
                } 

                throw new HttpRequestException($"Unable to fetch data. Status Code: {response.StatusCode}");
            } 

        } 
        catch (Exception ex) 
        { 
            Console.WriteLine($"Exception: {ex.Message}"); 
            return false;
        } 
    }

    public static List<PriceBar> Parser(string symbol, Timeframe timeframe = Timeframe.Daily)
    {
        symbol = NormalizeSymbol(symbol);
        string filePath = GetFilePath(symbol, timeframe);
        var bars = new List<PriceBar>();

        try
        {
            // Read the CSV file line by line
            string[] lines = File.ReadAllLines(filePath);

            // Skip the header and process each line
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                string[] columns = lines[i].Split(',');
                if (columns.Length < 6)
                {
                    throw new FormatException($"Invalid CSV row at line {i + 1}.");
                }

                DateTime date = DateTime.Parse(columns[0], CultureInfo.InvariantCulture);
                double openPrice = double.Parse(columns[1], CultureInfo.InvariantCulture);
                double highPrice = double.Parse(columns[2], CultureInfo.InvariantCulture);
                double lowPrice = double.Parse(columns[3], CultureInfo.InvariantCulture);
                double closePrice = double.Parse(columns[4], CultureInfo.InvariantCulture);
                long volume = long.Parse(columns[5], CultureInfo.InvariantCulture);

                bars.Add(new PriceBar(date, openPrice, highPrice, lowPrice, closePrice, volume));
            }

            bars.Sort((left, right) => left.Date.CompareTo(right.Date));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }

        return bars;
    }

    private static string GetFilePath(string symbol, Timeframe timeframe)
    {
        string suffix = timeframe == Timeframe.Weekly ? "weekly" : "daily";
        return Path.Combine(DataDirectory, $"{symbol}_{suffix}_data.csv");
    }

    private static string NormalizeSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol) ||
            symbol.Any(character => !char.IsLetterOrDigit(character) && character != '.' && character != '-'))
        {
            throw new ArgumentException("Ticker symbols may only contain letters, numbers, '.', or '-'.", nameof(symbol));
        }

        return symbol.Trim().ToUpperInvariant();
    }
} 