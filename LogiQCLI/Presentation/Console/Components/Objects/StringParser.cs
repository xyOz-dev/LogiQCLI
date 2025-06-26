using System.Collections.Generic;

namespace LogiQCLI.Presentation.Console.Components.Objects
{
    internal static class StringParser
    {
        public static List<string> ExtractQuotedStrings(string input)
        {
            var results = new List<string>();
            var inQuotes = false;
            var currentString = string.Empty;
            var quoteChar = '"';

            for (int i = 0; i < input.Length; i++)
            {
                var c = input[i];

                if (!inQuotes && (c == '"' || c == '\''))
                {
                    inQuotes = true;
                    quoteChar = c;
                    currentString = string.Empty;
                }
                else if (inQuotes && c == quoteChar)
                {
                    inQuotes = false;
                    results.Add(currentString);
                    currentString = string.Empty;
                }
                else if (inQuotes)
                {
                    currentString += c;
                }
            }

            return results;
        }
    }
}
