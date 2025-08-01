using System.Collections.Generic;
using System.Linq;
using System.Text;
using PortfolioHoldingsAggregator.Import;
using PortfolioHoldingsAggregator.Prompt;

namespace PortfolioHoldingsAggregator.Aggregate
{
    public static class HoldingAggregator
    {
        public static AggregationResult Aggregate(IReadOnlyList<IHoldingSource> holdingSources)
        {
            ConsoleWriter.WriteInfo("Aggregating holding sources...");

            var duplicateSymbols = GetDuplicateSymbols(holdingSources);
            var aggregationResult = new AggregationResult { TotalValue = holdingSources.Sum(holdingSource => holdingSource.ActualValue) };
            Dictionary<string, Dictionary<string, List<string>>> duplicateHoldingsBySymbol = [];
            foreach (var holdingSource in holdingSources)
            {
                foreach (var holding in holdingSource.Holdings)
                {
                    var aggregatedHolding = aggregationResult.Holdings.TryGetValue(holding.Symbol, out var existing) ? existing :
                                                aggregationResult.Holdings[holding.Symbol] = new AggregationResult.Holding { Name = holding.Name, Symbol = holding.Symbol };
                    aggregatedHolding.Value += holding.Value;
                    aggregatedHolding.WeightFraction = aggregatedHolding.Value / aggregationResult.TotalValue;

                    AddSourceContribution(aggregatedHolding, holdingSource.UniqueName, holding);
                    if (duplicateSymbols.Contains(holding.Symbol))
                    {
                        AddHoldingToDuplicates(duplicateHoldingsBySymbol, holding.Symbol, holding.Name, holdingSource.UniqueName);
                    }
                }

                if (holdingSource.InputtedValue != holdingSource.ActualValue)
                {
                    ConsoleWriter.WriteWarn($"""
                                             The inputted value for holding source '{holdingSource.UniqueName}' does not match the actual value.
                                             Inputted: {holdingSource.InputtedValue}, Actual: {holdingSource.ActualValue}
                                             See README section "Inputted vs Actual Value Discrepancy" for details.
                                             """);
                }
            }

            if (duplicateHoldingsBySymbol.Count > 0)
            {
                HandleDuplicateHoldings(duplicateHoldingsBySymbol, aggregationResult);
            }

            ConsoleWriter.WriteSuccess($"Aggregated {aggregationResult.Holdings.Count} holdings.");

            return aggregationResult;
        }

        // Identifies symbols that occur more than once within a single holding source.
        // Symbols appearing in multiple sources are not considered duplicates.
        private static HashSet<string> GetDuplicateSymbols(IReadOnlyList<IHoldingSource> holdingSources)
        {
            HashSet<string> duplicateSymbols = [];
            foreach (var holdingSource in holdingSources)
            {
                HashSet<string> symbols = [];
                foreach (var holding in holdingSource.Holdings)
                {
                    if (!symbols.Add(holding.Symbol))
                    {
                        duplicateSymbols.Add(holding.Symbol);
                    }
                }
            }

            return duplicateSymbols;
        }

        // If a source contribution already exists for the current holding source, it means the symbol appeared multiple times within that source and must be merged.
        private static void AddSourceContribution(AggregationResult.Holding aggregatedHolding, string holdingSourceName, IHoldingSource.IHolding holding)
        {
            var value = holding.Value;
            var weightFraction = holding.WeightFraction;
            if (aggregatedHolding.SourceContributions.TryGetValue(holdingSourceName, out var sourceContribution))
            {
                value += sourceContribution.Value;
                weightFraction += sourceContribution.WeightFraction;
            }

            aggregatedHolding.SourceContributions[holdingSourceName] = new AggregationResult.Holding.SourceContribution(value, weightFraction);
        }

        private static void AddHoldingToDuplicates(Dictionary<string, Dictionary<string, List<string>>> duplicateHoldingsBySymbol,
                                                   string symbol,
                                                   string holdingName,
                                                   string sourceName)
        {
            if (!duplicateHoldingsBySymbol.TryGetValue(symbol, out var sourcesByHoldingName))
            {
                sourcesByHoldingName = new Dictionary<string, List<string>> { { holdingName, [] } };
                duplicateHoldingsBySymbol[symbol] = sourcesByHoldingName;
            }

            if (!sourcesByHoldingName.TryGetValue(holdingName, out var sources))
            {
                sources = [];
                sourcesByHoldingName[holdingName] = sources;
            }

            sources.Add(sourceName);
        }

        private static void HandleDuplicateHoldings(Dictionary<string, Dictionary<string, List<string>>> duplicateHoldingsBySymbol, AggregationResult aggregationResult)
        {
            ConsoleWriter.WriteWarn("""
                                    Some Symbols appeared multiple times within the same holding source.
                                    Their values and weights are combined into a single row for that source.
                                    See README section "Handling Duplicate Symbols" for details.

                                    Affected Symbols:
                                    """);

            foreach (var (symbol, sourcesByHoldingName) in duplicateHoldingsBySymbol)
            {
                ConsoleWriter.WriteAlignedToDisplayKind($"- {symbol}", DisplayKind.Warn);

                var i = 0;
                var mergedName = new StringBuilder();
                foreach (var (holdingName, sources) in sourcesByHoldingName)
                {
                    mergedName.Append($"{holdingName} ({string.Join(", ", sources)})");
                    if (i++ < sourcesByHoldingName.Count - 1)
                    {
                        mergedName.Append(", ");
                    }
                }

                aggregationResult.Holdings[symbol].Name = mergedName.ToString();
            }
        }
    }
}