namespace PortfolioHoldingsAggregator.Prompt
{
    public readonly record struct Option<TValue>(TValue Value, string Text);
}