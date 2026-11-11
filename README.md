# Prerequisites
- .NET 10.0 SDK or later

# Configuration
- Create both environment files from their examples:
  - Copy `AlphaVantageBacktester/.env.example` to `AlphaVantageBacktester/.env`.
  - Copy `AlphaVantageBacktester.Gui/.env.example` to `AlphaVantageBacktester.Gui/.env`.
- Fill in `ALPHA_VANTAGE_API_KEY` with your Alpha Vantage API key in both `.env` files.

# How to launch

From the repository root, choose either application:

## CLI

```bash
dotnet run --project AlphaVantageBacktester
```

## GUI

```bash
dotnet run --project AlphaVantageBacktester.Gui
```

# How to use
- Run the application and choose a ticker from the menu. Data is either fetched from AlphaVantage, or instantiated through cached data if not older than 24 hours.
- The menu supports 12/26 EMA crossover, linear-regression trend following, and RSI mean reversion, MAGD momentum following, Breakout and Bolliunger Bands.
- Each strategy starts with 10,000 and can take long or short exposure. Position sizing moves 50% toward
  the strategy's target exposure on each bar to reduce over-trading and account for the current position.
- You can add hyperparameters twoard short / long bias, as well as skew bias.

# Contact : [Email](mailto:pablo.ferreiraa10@gmail.com)
