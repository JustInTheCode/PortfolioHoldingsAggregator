using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using PortfolioHoldingsAggregator.Aggregate;
using PortfolioHoldingsAggregator.Import;
using PortfolioHoldingsAggregator.Prompt;

namespace PortfolioHoldingsAggregator.Export
{
    public static class HoldingExporter
    {
        private static readonly Option<bool>[] YesNoOptions = [new(false, "No"), new(true, "Yes")];

        public static void ToCsv(IReadOnlyList<IHoldingSource> holdingSources, AggregationResult aggregationResult)
        {
            var fileName = PromptForCsvFileName();
            var filePath = GetUniqueFilePath(AppContext.BaseDirectory, fileName, ".csv");
            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            var writeDetailed = OptionPrompt.PromptForChoice(YesNoOptions, "Would you like a detailed breakdown showing the value and percentage of each holding per source?");
            ConsoleWriter.WriteInfo("Writing results to CSV file...");
            if (writeDetailed)
            {
                WriteDetailedCsv(holdingSources, aggregationResult, csv);
            }
            else
            {
                WriteCompactCsv(aggregationResult, csv);
            }

            ConsoleWriter.WriteSuccess($"Results written to '{Path.GetFileName(filePath)}' in the same folder as the executable.");
        }

        private static string PromptForCsvFileName()
        {
            const string defaultFileName = "aggregated_portfolio";
            while (true)
            {
                ConsoleWriter.WritePrompt("Enter a name (without the file extension) for the CSV file to save the aggregated results to (leave blank to use the default):");

                var fileNameInput = Console.ReadLine()?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(fileNameInput))
                {
                    ConsoleWriter.WriteOk($"Using the default file name: '{defaultFileName}.csv'");
                    return defaultFileName;
                }

                if (fileNameInput.Contains(Path.DirectorySeparatorChar) || fileNameInput.Contains(Path.AltDirectorySeparatorChar))
                {
                    ConsoleWriter.WriteFail("The file name must not contain directory separators. Please enter a valid file name.");
                    continue;
                }

                var extension = Path.GetExtension(fileNameInput);
                if (!string.IsNullOrEmpty(extension) && !extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    ConsoleWriter.WriteWarn("Only CSV files are supported. The extension you entered has been removed.");
                }

                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileNameInput);
                if (string.IsNullOrEmpty(fileNameWithoutExtension))
                {
                    ConsoleWriter.WriteFail("The input only contained a file extension. Please provide a valid file name.");
                    continue;
                }

                var invalidChars = Path.GetInvalidFileNameChars();
                var sanitizedFileName = string.Concat(fileNameWithoutExtension.Where(c => !invalidChars.Contains(c)));
                if (sanitizedFileName != fileNameWithoutExtension)
                {
                    ConsoleWriter.WriteWarn($"The file name '{fileNameWithoutExtension}' contained invalid characters, which have been removed.");
                }

                sanitizedFileName = sanitizedFileName.Trim();
                if (!string.IsNullOrEmpty(sanitizedFileName))
                {
                    ConsoleWriter.WriteOk($"File name set to: '{sanitizedFileName}.csv'");
                    return sanitizedFileName;
                }

                ConsoleWriter.WriteFail("The file name is empty after sanitization. Please enter a valid file name.");
            }
        }

        private static string GetUniqueFilePath(string directory, string baseName, string extension)
        {
            var filePath = Path.Combine(directory, $"{baseName}{extension}");
            if (!File.Exists(filePath))
            {
                return filePath;
            }

            var count = 0;
            do
            {
                filePath = Path.Combine(directory, $"{baseName}_{++count}{extension}");
            }
            while (File.Exists(filePath));

            ConsoleWriter.WriteWarn($"A file named '{baseName}{extension}' already exists. The name has been changed to '{baseName}_{count}{extension}' to avoid overwriting.");

            return filePath;
        }

        private static void WriteDetailedCsv(IReadOnlyList<IHoldingSource> holdingSources, AggregationResult aggregationResult, CsvWriter csv)
        {
            WriteHeaderRow();
            WriteValueRows();
            WriteTotalRow();

            return;

            void WriteHeaderRow()
            {
                csv.WriteField("Name");
                csv.WriteField("Symbol");
                foreach (var holdingSource in holdingSources)
                {
                    csv.WriteField($"Weight {holdingSource.UniqueName}");
                    csv.WriteField($"Value {holdingSource.UniqueName}");
                }

                csv.WriteField("Total Weight");
                csv.WriteField("Total Value");
                csv.NextRecord();
            }

            void WriteValueRows()
            {
                foreach (var holding in aggregationResult.Holdings.Values.OrderByDescending(h => h.Value))
                {
                    csv.WriteField(holding.Name);
                    csv.WriteField(holding.Symbol);
                    foreach (var holdingSource in holdingSources)
                    {
                        if (holding.SourceContributions.TryGetValue(holdingSource.UniqueName, out var contribution))
                        {
                            csv.WriteField(contribution.WeightFraction);
                            csv.WriteField(contribution.Value);
                        }
                        else
                        {
                            csv.WriteField(0);
                            csv.WriteField(0);
                        }
                    }

                    csv.WriteField(holding.WeightFraction);
                    csv.WriteField(holding.Value);
                    csv.NextRecord();
                }
            }

            void WriteTotalRow()
            {
                csv.WriteField("Total");
                csv.WriteField("N/A");
                foreach (var holdingSource in holdingSources)
                {
                    csv.WriteField(holdingSource.ActualValue / aggregationResult.TotalValue);
                    csv.WriteField(holdingSource.ActualValue);
                }

                csv.WriteField(1);
                csv.WriteField(aggregationResult.TotalValue);
            }
        }

        private static void WriteCompactCsv(AggregationResult aggregationResult, CsvWriter csv)
        {
            WriteHeaderRow();
            WriteValueRows();
            WriteTotalRow();

            return;

            void WriteHeaderRow()
            {
                csv.WriteField("Name");
                csv.WriteField("Symbol");
                csv.WriteField("Weight");
                csv.WriteField("Value");
                csv.NextRecord();
            }

            void WriteValueRows()
            {
                foreach (var holding in aggregationResult.Holdings.Values.OrderByDescending(h => h.Value))
                {
                    csv.WriteField(holding.Name);
                    csv.WriteField(holding.Symbol);
                    csv.WriteField(holding.WeightFraction);
                    csv.WriteField(holding.Value);
                    csv.NextRecord();
                }
            }

            void WriteTotalRow()
            {
                csv.WriteField("Total");
                csv.WriteField("N/A");
                csv.WriteField(1);
                csv.WriteField(aggregationResult.TotalValue);
            }
        }
    }
}