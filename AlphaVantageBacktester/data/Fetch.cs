namespace AlphaVantageBacktester.data;
using System; 
using System.IO; 
using System.Net.Http; 
using System.Threading.Tasks; 
using utilities;
public class Fetch
{ 
    static async Task Fetcher(string symbol) 
    { 
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
                    string filePath = $"{symbol}_daily_data.csv"; 
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
} 