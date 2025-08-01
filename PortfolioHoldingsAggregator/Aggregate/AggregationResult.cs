using System.Collections.Generic;
using CsvHelper.Configuration.Attributes;

namespace PortfolioHoldingsAggregator.Aggregate
{
    public class AggregationResult
    {
        public decimal TotalValue { get; init; }

        public Dictionary<string, Holding> Holdings { get; } = [];

        public class Holding
        {
            public required string Name { get; set; }

            public required string Symbol { get; init; }

            [Name("Weight")]
            public decimal WeightFraction { get; set; }

            public decimal Value { get; set; }

            [Ignore]
            public Dictionary<string, SourceContribution> SourceContributions { get; } = [];

            public readonly record struct SourceContribution(decimal Value, decimal WeightFraction);
        }
    }
}