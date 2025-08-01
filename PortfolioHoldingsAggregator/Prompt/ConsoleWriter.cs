using System;
using System.Linq;
using System.Text;

namespace PortfolioHoldingsAggregator.Prompt
{
    public static class ConsoleWriter
    {
        public static void Write(string text)
        {
            Console.WriteLine(WrapText(text));
        }

        public static void WriteSuccess(string text)
        {
            WriteLine(text, DisplayKind.Success);
        }

        public static void WriteOk(string text)
        {
            WriteLine(text, DisplayKind.Ok);
        }

        public static void WriteInfo(string text)
        {
            WriteLine(text, DisplayKind.Info);
        }

        public static void WriteHint(string text)
        {
            WriteLine(text, DisplayKind.Hint);
        }

        public static void WriteWarn(string text)
        {
            WriteLine(text, DisplayKind.Warn);
        }

        public static void WriteFail(string text)
        {
            WriteLine(text, DisplayKind.Fail);
        }

        public static void WritePrompt(string text)
        {
            Console.WriteLine($"{Environment.NewLine}{WrapText(text)}");
            Console.Write("> ");
        }

        public static void WritePrompt<TValue>(string text, Option<TValue>[] options)
        {
            Console.WriteLine($"{Environment.NewLine}{WrapText(text)}");
            for (var i = 0; i < options.Length; i++)
            {
                Console.WriteLine(WrapText($"  {i}: {options[i].Text}"));
            }

            Console.Write("> ");
        }

        public static void WriteAlignedToDisplayKind(string text, DisplayKind displayKind, int indentLevel = 0, bool addProceedingNewLine = false)
        {
            var newLine = string.Empty;
            if (addProceedingNewLine)
            {
                newLine = Environment.NewLine;
            }

            var indent = new string(' ', displayKind.ToLabel().Length + indentLevel * 2);
            var wrappedText = WrapText(text, indent);
            Console.WriteLine($"{newLine}{wrappedText}");
        }

        private static void WriteLine(string text, DisplayKind displayKind)
        {
            var displayKindLabel = displayKind.ToLabel();
            var textIndent = new string(' ', displayKindLabel.Length);
            var wrappedText = WrapText(text, textIndent);
            wrappedText.Remove(0, textIndent.Length);
            wrappedText.Insert(0, displayKindLabel);

            Console.WriteLine($"{Environment.NewLine}{wrappedText}");
        }

        private static StringBuilder WrapText(string text, string textIndent = "")
        {
            var wrappedText = new StringBuilder();
            var lines = text.Split(Environment.NewLine);
            for (var i = 0; i < lines.Length; i++)
            {
                var wrappedLine = WrapLine(lines[i], textIndent);
                wrappedText.Append(wrappedLine);

                if (i < lines.Length - 1)
                {
                    wrappedText.AppendLine();
                }
            }

            return wrappedText;
        }

        private static StringBuilder WrapLine(string line, string textIndent)
        {
            var wrappedLine = new StringBuilder();
            wrappedLine.Append(textIndent);

            var originalLineIndent = line.TakeWhile(c => c == ' ').Count();
            var continuedLineIndent = new string(' ', originalLineIndent + textIndent.Length);
            var currentLineLength = wrappedLine.Length;
            var words = line.Split(' ');
            var lastIndex = words.Length - 1;
            for (var i = 0; i <= lastIndex; i++)
            {
                var word = i == lastIndex ? $"{words[i]}" : $"{words[i]} ";
                if (currentLineLength + word.Length > Console.WindowWidth)
                {
                    wrappedLine.AppendLine();
                    currentLineLength = 0;

                    wrappedLine.Append(continuedLineIndent);
                    currentLineLength += continuedLineIndent.Length;
                }

                wrappedLine.Append(word);
                currentLineLength += word.Length;
            }

            return wrappedLine;
        }

        private static string ToLabel(this DisplayKind kind)
        {
            return kind switch
            {
                DisplayKind.Success => "[SUCCESS] ",
                DisplayKind.Ok => "[OK] ",
                DisplayKind.Info => "[INFO] ",
                DisplayKind.Hint => "[HINT] ",
                DisplayKind.Warn => "[WARN] ",
                DisplayKind.Fail => "[FAIL] ",
                _ => $"[{kind.ToString().ToUpperInvariant()}] ",
            };
        }
    }
}