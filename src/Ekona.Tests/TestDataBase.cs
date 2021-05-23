using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace SceneGate.Ekona.Tests
{
    public static class TestDataBase
    {
        public static string RootFromOutputPath {
            get {
                string envVar = Environment.GetEnvironmentVariable("SCENEGATE_TEST_DIR");
                if (!string.IsNullOrEmpty(envVar)) {
                    return envVar;
                }

                string programDir = AppDomain.CurrentDomain.BaseDirectory;
                string path = Path.Combine(programDir, "Resources");
                return Path.GetFullPath(path);
            }
        }

        public static void IgnoreIfFileDoesNotExist(string file)
        {
            if (!File.Exists(file)) {
                string msg = $"[{TestContext.CurrentContext.Test.ClassName}] Missing resource file: {file}";
                TestContext.Progress.WriteLine(msg);
                Assert.Ignore(msg);
            }
        }

        public static IEnumerable<string> ReadTestListFile(string filePath)
        {
            if (!File.Exists(filePath)) {
                return Array.Empty<string>();
            }

            return File.ReadAllLines(filePath)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'));
        }
    }
}
