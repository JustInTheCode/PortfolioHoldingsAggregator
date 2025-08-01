using System.Collections.Generic;
using CsvHelper.Configuration.Attributes;

namespace PortfolioHoldingsAggregator.Import
{
    public class HoldingSource : IHoldingSource
    {
        public required string Path { get; init; }

        public required string FileName { get; init; }

        public string UniqueName { get; set; } = string.Empty;

        public required IReadOnlyList<IHoldingSource.IHolding> Holdings { get; init; }

        public required decimal InputtedValue { get; init; }

        public decimal ActualValue { get; init; }

        public class Holding : IHoldingSource.IHolding
        {
            [Index(2)]
            public required string Weight { get; set; }

            [Index(0)]
            public required string Name { get; set; }

            [Index(1)]
            public required string Symbol { get; set; }

            [Ignore]
            public decimal WeightFraction { get; set; }

            [Ignore]
            public decimal Value { get; set; }
        }
    }
}