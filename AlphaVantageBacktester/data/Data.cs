namespace AlphaVantageBacktester.data;
using System; 
using System.IO; 
using System.Net.Http; 
using System.Threading.Tasks; 
using utilities;
public class Data
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

    static void Parser(string symbol)
    {
        {
            string filePath = $"{symbol}_daily_data.csv"; // Path to the CSV file 

            try
            {
                // Read the CSV file line by line 
                string[] lines = File.ReadAllLines(filePath);

                // Skip the header and process each line 
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] columns = lines[i].Split(',');

                    string date = columns[0];
                    double openPrice = double.Parse(columns[1]);
                    double highPrice = double.Parse(columns[2]);
                    double lowPrice = double.Parse(columns[3]);
                    double closePrice = double.Parse(columns[4]);
                    long volume = long.Parse(columns[5]);

                    Console.WriteLine(
                        $"Date: {date}, Open: {openPrice}, High: {highPrice}, Low: {lowPrice}, Close: {closePrice}");
                    // Perform calculations or add to your backtesting logic here 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
    }
} 