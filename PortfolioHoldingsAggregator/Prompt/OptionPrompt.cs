using System;

namespace PortfolioHoldingsAggregator.Prompt
{
    public static class OptionPrompt
    {
        public static TValue PromptForChoice<TValue>(Option<TValue>[] options, string promptMessage)
            where TValue : notnull
        {
            while (true)
            {
                ConsoleWriter.WritePrompt(promptMessage, options);

                var input = Console.ReadLine() ?? string.Empty;
                if (int.TryParse(input, out var parsedInput) && parsedInput >= 0 && parsedInput < options.Length)
                {
                    return options[parsedInput].Value;
                }

                ConsoleWriter.WriteFail($"Invalid input please enter a number between 0 and {options.Length - 1}. Please try again.");
            }
        }
    }
}