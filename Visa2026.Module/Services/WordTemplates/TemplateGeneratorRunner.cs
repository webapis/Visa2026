using System;
using System.IO;
using System.Reflection;

namespace Visa2026.Module.Services.WordTemplates
{
    /// <summary>
    /// One-shot runner: call GenerateAll() from a unit test, a startup hook (DEBUG only),
    /// or a throwaway console app to emit all Word templates into Resources/.
    ///
    /// Usage from a test or scratch file:
    ///   TemplateGeneratorRunner.GenerateAll(outputDir: @"path\to\Visa2026.Module\Resources");
    ///
    /// After running, register each generated file in Visa2026.Module.csproj as EmbeddedResource.
    /// </summary>
    public static class TemplateGeneratorRunner
    {
        public static void GenerateAll(string outputDir)
        {
            Directory.CreateDirectory(outputDir);

            Write(outputDir, "BusinessTrip_Sanawy.docx",
                BusinessTripSanawyTemplateGenerator.Generate());

            Console.WriteLine($"[TemplateGeneratorRunner] All templates written to: {outputDir}");
        }

        private static void Write(string dir, string fileName, byte[] bytes)
        {
            var path = Path.Combine(dir, fileName);
            File.WriteAllBytes(path, bytes);
            Console.WriteLine($"  ✓ {fileName}  ({bytes.Length:N0} bytes)");
        }
    }
}
