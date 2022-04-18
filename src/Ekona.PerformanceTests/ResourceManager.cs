// Copyright (c) 2022 SceneGate

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
using SceneGate.Ekona.Security;
using YamlDotNet.Serialization;

namespace SceneGate.Ekona.PerformanceTests;

public static class ResourceManager
{
    public static string ResourceDirectory {
        get {
            string? envVar = Environment.GetEnvironmentVariable("SCENEGATE_TEST_DIR");
            if (string.IsNullOrEmpty(envVar)) {
                throw new InvalidOperationException("Missing environment variable");
            }

            return envVar;
        }
    }

    public static DsiKeyStore GetDsiKeyStore()
    {
        string keysPath = Path.Combine(ResourceDirectory, "dsi_keys.yml");
        if (!File.Exists(keysPath)) {
            throw new InvalidOperationException("Missing keys file");
        }

        string keysYaml = File.ReadAllText(keysPath);
        return new DeserializerBuilder()
            .Build()
            .Deserialize<DsiKeyStore>(keysYaml);
    }

    public static IEnumerable<FilePathInfo> GetRoms()
    {
        string containerDir = Path.Combine(ResourceDirectory, "Containers");
        string filePath = Path.Combine(containerDir, "rom.txt");
        if (!File.Exists(filePath)) {
            return Array.Empty<FilePathInfo>();
        }

        return File.ReadAllLines(filePath)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
            .Select(l => Path.Combine(containerDir, l.Split(",")[1]))
            .Select(p => new FilePathInfo(p));
    }
}
