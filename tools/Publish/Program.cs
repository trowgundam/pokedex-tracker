using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

if (args.Length != 2) throw new ArgumentException("Usage: Publish <published wwwroot> <base path>");
string root = Path.GetFullPath(args[0]);
string basePath = args[1];
if (!basePath.StartsWith('/') || !basePath.EndsWith('/') || basePath.Contains('"') || basePath.Contains('<') || basePath.Contains('>'))
    throw new ArgumentException("The base path must start and end with a slash and contain no HTML markup.");
string indexPath = Path.Combine(root, "index.html");
string index = File.ReadAllText(indexPath);
if (!index.Contains("<base href=\"/\" />", StringComparison.Ordinal) && !index.Contains($"<base href=\"{basePath}\" />", StringComparison.Ordinal))
    throw new InvalidDataException("The published base placeholder is missing or already uses a different path.");
File.WriteAllText(indexPath, index.Replace("<base href=\"/\" />", $"<base href=\"{basePath}\" />", StringComparison.Ordinal), new UTF8Encoding(false));
File.WriteAllText(Path.Combine(root, ".nojekyll"), "");
File.Copy(indexPath, Path.Combine(root, "404.html"), true);

// Changing index.html after publication must update its service worker integrity hash.
string manifestPath = Path.Combine(root, "service-worker-assets.js");
string manifest = File.ReadAllText(manifestPath);
int start = manifest.IndexOf('{'), end = manifest.LastIndexOf('}');
JsonObject json = JsonNode.Parse(manifest[start..(end + 1)])!.AsObject();
JsonArray assets = json["assets"]!.AsArray();
foreach (JsonNode? node in assets)
    if (node!["url"]!.GetValue<string>() == "index.html")
        node["hash"] = "sha256-" + Convert.ToBase64String(SHA256.HashData(File.ReadAllBytes(indexPath)));
json["version"] = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(assets.ToJsonString())))[..16];
File.WriteAllText(manifestPath, "self.assetsManifest = " + json.ToJsonString() + ";\n", new UTF8Encoding(false));

// Existing installations also need a changed worker and an uncached manifest URL.
string workerPath = Path.Combine(root, "service-worker.js");
string worker = File.ReadAllText(workerPath);
Regex import = new(@"self\.importScripts\('\./service-worker-assets\.js(?:\?v=[a-f0-9]+)?'\);");
if (!import.IsMatch(worker)) throw new InvalidDataException("The service worker manifest import is missing.");
File.WriteAllText(workerPath, import.Replace(worker, $"self.importScripts('./service-worker-assets.js?v={json["version"]!.GetValue<string>()}');", 1), new UTF8Encoding(false));
foreach (JsonNode? node in assets)
{
    string url = node!["url"]!.GetValue<string>();
    string actual = "sha256-" + Convert.ToBase64String(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, url))));
    if (actual != node["hash"]!.GetValue<string>())
        throw new InvalidDataException($"The published offline asset hash does not match: {url}");
}
Console.WriteLine($"Prepared {basePath} and verified {assets.Count} offline asset hashes.");
