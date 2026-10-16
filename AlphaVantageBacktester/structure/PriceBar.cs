namespace AlphaVantageBacktester.structure;

public record PriceBar(DateTime Date, double Open, double High, double Low, double Close, long Volume);