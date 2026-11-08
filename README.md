# Prerequisites: 
- C# NET 10.0+

# How to launch:
- Copy `./AlphaVantageBacktester/.env.example` and rename the copy to `.env`
- Put in your Alppha Vantage Api Key following the example's structure
- Compile through the entry point of the project

# How to use:
- Run the application and choose a ticker from the menu. Data is either fetched from AlphaVantage, or instantiated through cached data if not older than 24 hours.
- The menu supports 12/26 EMA crossover, linear-regression trend following, and RSI mean reversion.
- Each strategy starts with 10,000 and can take long or short exposure. Position sizing moves 50% toward
  the strategy's target exposure on each bar to reduce over-trading and account for the current position.

# Contact : [Email](mailto:pablo.ferreiraa10@gmail.com)
