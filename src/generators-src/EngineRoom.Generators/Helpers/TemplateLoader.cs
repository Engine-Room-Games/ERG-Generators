using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace EngineRoom.Generators.Helpers
{
    /// <summary>
    /// Loads code templates that are embedded into the generator dll as text resources.
    /// Templates use %%Name%% placeholders that are replaced via <see cref="LoadAndSubstitute"/>.
    /// </summary>
    internal static class TemplateLoader
    {
        private const string ResourceNamespace = "EngineRoom.Generators.Templates.";
        private const string ResourceExtension = ".cs.txt";

        private static readonly Assembly TemplateAssembly = typeof(TemplateLoader).Assembly;

        public static string Load(string templateName)
        {
            // Templates live in nested folders (e.g. "Singleton/SingletonAttribute"),
            // but embedded-resource manifest names use dots as separators.
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
                text = text.Replace("%%" + pair.Key + "%%", pair.Value);
            }

            return text;
        }
    }
}
