using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using PortfolioHoldingsAggregator.Prompt;
using MissingFieldException = CsvHelper.MissingFieldException;

namespace PortfolioHoldingsAggregator.Import
{
    public class HoldingImporter
    {
        private static readonly Option<string>[] DelimiterOptions = [new(",", "Comma (,)"), new(";", "Semicolon (;)")];

        private static readonly Option<string>[] DecimalSeparatorOptions = [new(".", "Dot (.)"), new(",", "Comma (,)")];

        private static readonly Option<WeightType>[] WeightTypeOptions = [new(WeightType.Percent, "Percentage"), new(WeightType.Fraction, "Fraction")];

        private static readonly Option<bool>[] YesNoOptions = [new(false, "No"), new(true, "Yes")];

        private static readonly CultureInfo DotCulture = CultureInfo.InvariantCulture;

        private static readonly CultureInfo CommaCulture = new(CultureInfo.InvariantCulture.Name) { NumberFormat = { NumberDecimalSeparator = "," } };

        private readonly List<HoldingSource> _holdingSources = [];

        private CsvFormatSettings? _csvFormatSettings;

        private bool _reuseDecisionMade;

        public IReadOnlyList<IHoldingSource> Import()
        {
            while (true)
            {
                var addSource = true;
                var sourcePath = PromptForSourcePath();
                var existingHoldingSource = _holdingSources.FirstOrDefault(holdingSource => holdingSource.Path == sourcePath);
                if (existingHoldingSource != null)
                {
                    ConsoleWriter.WriteWarn($"Holding source '{sourcePath}' already added.");
                    var overwrite = OptionPrompt.PromptForChoice(YesNoOptions, "Would you like to overwrite the holding source?");
                    if (overwrite)
                    {
                        _holdingSources.Remove(existingHoldingSource);
                    }
                    else
                    {
                        addSource = false;
                        ConsoleWriter.WriteOk("Keeping the existing holding source.");
                    }
                }

                if (addSource)
                {
                    var holdingSource = TryLoadSourceData(sourcePath);
                    if (holdingSource == null)
                    {
                        continue;
                    }

                    _holdingSources.Add(holdingSource);
                    ConsoleWriter.WriteOk("Holding source added.");
                }

                addSource = OptionPrompt.PromptForChoice(YesNoOptions, "Do you want to add another holding source?");
                if (addSource)
                {
                    continue;
                }

                break;
            }

            AssignUniqueNamesToHoldingSources(_holdingSources);

            return _holdingSources;
        }

        public void ClearImportedData()
        {
            _holdingSources.Clear();
        }

        private HoldingSource? TryLoadSourceData(string sourcePath)
        {
            var inputtedValue = PromptForSourceValue();
            var actualValue = 0m;
            var reusableSettings = _csvFormatSettings ?? GetCsvFormatSettings();
            var cultureInfo = reusableSettings.DecimalSeparator == "." ? DotCulture : CommaCulture;

            HoldingSource.Holding[] holdings;
            try
            {
                using var reader = new StreamReader(sourcePath);
                var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false, Delimiter = reusableSettings.Delimiter });
                holdings = csv.GetRecords<HoldingSource.Holding>().ToArray();
                foreach (var holding in holdings)
                {
                    holding.WeightFraction = reusableSettings.WeightType == WeightType.Fraction ? decimal.Parse(holding.Weight, cultureInfo) :
                                                 decimal.Parse(holding.Weight.Replace("%", ""), cultureInfo) / 100.0m;
                    holding.Value = holding.WeightFraction * inputtedValue;
                    actualValue += holding.Value;
                }
            }
            catch (Exception exception)
            {
                HandleParsingError(exception, sourcePath, reusableSettings.Delimiter, reusableSettings.DecimalSeparator, reusableSettings.WeightType);
                return null;
            }

            var holdingSource = new HoldingSource
                                {
                                    Path = sourcePath,
                                    FileName = Path.GetFileNameWithoutExtension(sourcePath),
                                    InputtedValue = inputtedValue,
                                    ActualValue = actualValue,
                                    Holdings = holdings,
                                };

            return holdingSource;
        }

        private CsvFormatSettings GetCsvFormatSettings()
        {
            if (_csvFormatSettings != null)
            {
                return _csvFormatSettings;
            }

            var delimiter = OptionPrompt.PromptForChoice(DelimiterOptions, "Enter the delimiter used in the CSV file(s): ");
            var decimalSeparator = OptionPrompt.PromptForChoice(DecimalSeparatorOptions, "Enter the decimal separator used in the CSV file(s): ");
            var weightType = OptionPrompt.PromptForChoice(WeightTypeOptions, "Enter how the weights are specified in the CSV file(s): ");
            var csvFormatSettings = new CsvFormatSettings(delimiter, decimalSeparator, weightType);

            if (_reuseDecisionMade)
            {
                return csvFormatSettings;
            }

            _reuseDecisionMade = true;
            var reuse = OptionPrompt.PromptForChoice(YesNoOptions,
                                                     """
                                                     Would you like to reuse these settings — delimiter, decimal separator, and weight type — for all CSV files?
                                                     You won’t be able to change them later unless an error occurs and you choose to clear the settings.
                                                     """);
            if (reuse)
            {
                _csvFormatSettings = csvFormatSettings;
            }

            return csvFormatSettings;
        }

        private static string PromptForSourcePath()
        {
            while (true)
            {
                ConsoleWriter.WritePrompt("Enter the path to the CSV file containing the holdings:");

                var filePath = Console.ReadLine()?.Trim().Trim('"') ?? string.Empty;
                if (!File.Exists(filePath))
                {
                    ConsoleWriter.WriteFail("Invalid file path. Please try again.");
                    continue;
                }

                if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    return filePath;
                }

                ConsoleWriter.WriteFail("The file must be a CSV file. Please try again.");
            }
        }

        private static decimal PromptForSourceValue()
        {
            while (true)
            {
                ConsoleWriter.WritePrompt("Enter the total value of all holdings listed in the file:");

                var value = Console.ReadLine() ?? string.Empty;
                if (decimal.TryParse(value, out var parsedValue) && parsedValue > 0)
                {
                    return parsedValue;
                }

                ConsoleWriter.WriteFail("The value must be a valid number and greater than zero. Please try again.");
            }
        }

        private void HandleParsingError(Exception exception, string sourcePath, string delimiter, string decimalSeparator, WeightType weightType)
        {
            switch (exception)
            {
                case MissingFieldException:
                    ConsoleWriter.WriteFail($"""
                                             Failed to read '{sourcePath}' — the file is missing fields.
                                             Make sure:
                                               - That each row has all three fields: Name, Symbol, and Weight (e.g., "Apple Inc,AAPL,7.58%")
                                               - The file uses the specified delimiter: '{delimiter}'
                                             """);
                    break;
                case FormatException:
                {
                    ConsoleWriter.WriteFail($"""
                                             Failed to read '{sourcePath}' — the file format is incorrect..
                                             Make sure:
                                               - The file uses the specified decimal separator: '{decimalSeparator}'
                                               - The file uses the specified weight type: '{weightType}'
                                             """);
                    break;
                }
                case IOException:
                    ConsoleWriter.WriteFail($"""
                                             Could not open '{sourcePath}' — the file may be in use or locked by another process.
                                             Please ensure the file is closed in other programs before trying again.
                                             """);
                    return;
                default:
                    ConsoleWriter.WriteFail($"An unexpected error occurred while processing '{sourcePath}': {exception.Message}");
                    return;
            }

            ConsoleWriter.WriteAlignedToDisplayKind("""
                                                    To fix this, you can:
                                                      1. Correct the source file to match the expected format and add it again.
                                                    """,
                                                    DisplayKind.Fail,
                                                    addProceedingNewLine: true);

            if (_csvFormatSettings == null)
            {
                ConsoleWriter.WriteAlignedToDisplayKind("2. Add the source file and choose the correct settings.", DisplayKind.Fail, 1);
            }
            else
            {
                ConsoleWriter.WriteAlignedToDisplayKind("2. Clear the delimiter, decimal separator, and weight type settings, then add the source file again.",
                                                        DisplayKind.Fail,
                                                        1);
                var clearSettings = OptionPrompt.PromptForChoice(YesNoOptions, "Would you like to clear the delimiter, decimal separator, and weight type settings?");
                if (clearSettings)
                {
                    _csvFormatSettings = null;
                    ConsoleWriter.WriteOk("Settings cleared.");
                }
                else
                {
                    ConsoleWriter.WriteOk("Keeping settings.");
                }
            }
        }

        private static void AssignUniqueNamesToHoldingSources(List<HoldingSource> holdingSources)
        {
            HashSet<string> duplicateFileNames = [];
            HashSet<string> fileNames = [];
            foreach (var holdingSource in holdingSources.Where(holdingSource => !fileNames.Add(holdingSource.FileName)))
            {
                duplicateFileNames.Add(holdingSource.FileName);
            }

            foreach (var holdingSource in holdingSources)
            {
                var dirName = Path.GetFileName(Path.GetDirectoryName(holdingSource.Path));
                if (string.IsNullOrEmpty(dirName))
                {
                    dirName = OperatingSystem.IsWindows() ? holdingSource.Path[..2] : "/";
                }

                holdingSource.UniqueName = duplicateFileNames.Contains(holdingSource.FileName) ? $"{holdingSource.FileName} ({dirName})" : holdingSource.FileName;
            }
        }
    }
}