using System;
using PortfolioHoldingsAggregator.Aggregate;
using PortfolioHoldingsAggregator.Export;
using PortfolioHoldingsAggregator.Import;
using PortfolioHoldingsAggregator.Prompt;

namespace PortfolioHoldingsAggregator
{
    internal static class Program
    {
        private const string IntroText = """
                                         Portfolio Holdings Aggregator
                                         -----------------------------

                                         This tool aggregates holdings from one or more CSV files.

                                         Format
                                           Each file must contain the following fields (no header row):
                                             - Name of the holding (e.g., Apple Inc)
                                             - Symbol (e.g., AAPL)
                                             - Weight — either:
                                               - A percentage like 7.8%
                                               - Or a fraction like 0.078

                                         Notes
                                           - To update an existing source, enter the same file path again and choose to overwrite it.
                                         """;

        private static readonly Option<int>[] NextActionOptions = [new(0, "Exit"), new(1, "Rerun (clear all holding sources)"), new(2, "Rerun (keep existing holding sources)")];

        public static void Main(string[] _)
        {
            ConsoleWriter.Write(IntroText);

            var importer = new HoldingImporter();
            while (true)
            {
                try
                {
                    var holdingSources = importer.Import();
                    var aggregationResult = HoldingAggregator.Aggregate(holdingSources);
                    HoldingExporter.ToCsv(holdingSources, aggregationResult);
                }
                catch (Exception exception)
                {
                    ConsoleWriter.WriteFail($"An unexpected error occured: {exception.Message}");
                }

                var nextAction = OptionPrompt.PromptForChoice(NextActionOptions, "What would you like to do next?");
                switch (nextAction)
                {
                    case 0:
                        return;
                    case 1:
                        importer.ClearImportedData();
                        ConsoleWriter.WriteOk("All holding sources have been cleared.");
                        break;
                    case 2:
                        ConsoleWriter.WriteOk("Existing holding sources will be kept.");
                        ConsoleWriter.WriteHint("""
                                                If you're rerunning due to changes made to a source file and don't actually want to add new files
                                                or modify previously added settings or values, you'll still need to overwrite the existing holding source.
                                                This is because data from the file is only re-imported when the file is added again.
                                                To re-import updated data, simply re-add the file and choose to overwrite it.
                                                """);
                        break;
                }
            }
        }
    }
}