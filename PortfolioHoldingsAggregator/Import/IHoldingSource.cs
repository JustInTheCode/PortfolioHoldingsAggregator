using System.Collections.Generic;

namespace PortfolioHoldingsAggregator.Import
{
    public interface IHoldingSource
    {
        string UniqueName { get; }

        decimal InputtedValue { get; }

        decimal ActualValue { get; }

        IReadOnlyList<IHolding> Holdings { get; }

        public interface IHolding
        {
            string Name { get; }

            string Symbol { get; }

            decimal WeightFraction { get; }

            decimal Value { get; }
        }
    }
}