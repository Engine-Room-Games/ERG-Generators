using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace EngineRoom.Generators.Helpers
{
    internal static class TemplateLoader
    {
        private const string ResourceNamespace = "EngineRoom.Generators.Templates.";
        private const string ResourceExtension = ".cs.txt";

        private static readonly Assembly TemplateAssembly = typeof(TemplateLoader).Assembly;

        public static string Load(string templateName)
        {
            // Embedded-resource manifest names use dots as separators, not slashes.
            var resourcePath = templateName.Replace('/', '.');
            var resourceName = ResourceNamespace + resourcePath + ResourceExtension;
            using var stream = TemplateAssembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                throw new InvalidOperationException($"Embedded template not found: {resourceName}");
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static string LoadAndSubstitute(string templateName, IReadOnlyDictionary<string, string> placeholders)
        {
            var text = Load(templateName);
            foreach (var pair in placeholders)
            {
                text = SubstitutePlaceholder(text, "%%" + pair.Key + "%%", pair.Value);
            }

            return text;
        }

        // Multi-line replacement values keep the placeholder line's leading
        // whitespace on every line, so the substituted block stays aligned with
        // its surroundings instead of dumping continuation lines at column 0.
        private static string SubstitutePlaceholder(string text, string token, string value)
        {
            var result = new StringBuilder(text.Length);
            var cursor = 0;

            while (true)
            {
                var index = text.IndexOf(token, cursor, StringComparison.Ordinal);
                if (index < 0)
                {
                    result.Append(text, cursor, text.Length - cursor);
                    return result.ToString();
                }

                result.Append(text, cursor, index - cursor);
                result.Append(IndentContinuationLines(value, GetLineIndent(text, index)));
                cursor = index + token.Length;
            }
        }

        private static string GetLineIndent(string text, int index)
        {
            var lineStart = index;
            while (lineStart > 0 && text[lineStart - 1] != '\n')
            {
                lineStart--;
            }

            var cursor = lineStart;
            while (cursor < index && (text[cursor] == ' ' || text[cursor] == '\t'))
            {
                cursor++;
            }

            return cursor == lineStart ? string.Empty : text.Substring(lineStart, cursor - lineStart);
        }

        private static string IndentContinuationLines(string value, string indent)
        {
            if (indent.Length == 0 || value.Length == 0)
            {
                return value;
            }

            var builder = new StringBuilder(value.Length + indent.Length);
            var cursor = 0;
            var first = true;

            while (cursor < value.Length)
            {
                var newline = value.IndexOf('\n', cursor);
                var lineEnd = newline < 0 ? value.Length : newline + 1;
                var lineLength = lineEnd - cursor;
                var contentLength = newline < 0
                    ? lineLength
                    : (newline > cursor && value[newline - 1] == '\r' ? lineLength - 2 : lineLength - 1);

                if (!first && contentLength > 0)
                {
                    builder.Append(indent);
                }

                builder.Append(value, cursor, lineLength);
                cursor = lineEnd;
                first = false;
            }

            return builder.ToString();
        }
    }
}
