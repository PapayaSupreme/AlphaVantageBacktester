namespace AlphaVantageBacktester.data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using utilities;

public record PriceBar(DateTime Date, double Open, double High, double Low, double Close, long Volume);

public class Data
{
    private static readonly string DataDirectory = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data"));

    private static readonly TimeSpan CacheMaxAge = TimeSpan.FromHours(24);

    public static async Task Fetcher(string symbol)
    {
        string filePath = Path.Combine(DataDirectory, $"{symbol}_daily_data.csv");

        if (File.Exists(filePath) && DateTime.Now - File.GetLastWriteTime(filePath) < CacheMaxAge)
        {
            Console.WriteLine($"Using cached data for {symbol} ({filePath})");
            return;
        }

        string apiKey = Env.GetApiKey();

        // Construct the API URL
        string url =
            $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={apiKey}&datatype=csv";

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
                    File.WriteAllText(filePath, csvData);
 
                    Console.WriteLine($"Data successfully fetched and saved to {filePath}"); 
                } 
                else 
                { 
                    Console.WriteLine($"Error: Unable to fetch data. Status Code: {response.StatusCode}"); 
                } 
            } 

        } 
        catch (Exception ex) 
        { 
            Console.WriteLine($"Exception: {ex.Message}"); 
        } 
    }

    public static List<PriceBar> Parser(string symbol)
    {
        string filePath = Path.Combine(DataDirectory, $"{symbol}_daily_data.csv");
        var bars = new List<PriceBar>();

        try
        {
            // Read the CSV file line by line
            string[] lines = File.ReadAllLines(filePath);

            // Skip the header and process each line
            for (int i = 1; i < lines.Length; i++)
            {
                string[] columns = lines[i].Split(',');

                DateTime date = DateTime.Parse(columns[0], CultureInfo.InvariantCulture);
                double openPrice = double.Parse(columns[1], CultureInfo.InvariantCulture);
                double highPrice = double.Parse(columns[2], CultureInfo.InvariantCulture);
                double lowPrice = double.Parse(columns[3], CultureInfo.InvariantCulture);
                double closePrice = double.Parse(columns[4], CultureInfo.InvariantCulture);
                long volume = long.Parse(columns[5], CultureInfo.InvariantCulture);

                bars.Add(new PriceBar(date, openPrice, highPrice, lowPrice, closePrice, volume));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }

        return bars;
    }
} 