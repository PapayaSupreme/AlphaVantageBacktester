namespace AlphaVantageBacktester.utilities;

public class Env
{
    public static string GetApiKey()
    {
        const string variableName = "ALPHA_VANTAGE_API_KEY";
        string? apiKey = Environment.GetEnvironmentVariable(variableName);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            DirectoryInfo? directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory is not null)
            {
                string envPath = Path.Combine(directory.FullName, ".env");

                if (File.Exists(envPath))
                {
                    apiKey = File.ReadLines(envPath)
                        .Where(line => line.StartsWith($"{variableName}=", StringComparison.Ordinal))
                        .Select(line => line[(variableName.Length + 1)..].Trim())
                        .FirstOrDefault();
                    break;
                }

                directory = directory.Parent;
            }
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"The {variableName} value was not found in the environment or .env file.");
        }

        return apiKey;
    }
}