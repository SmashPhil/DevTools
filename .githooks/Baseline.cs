#:property OutputType=Exe
#:property PublishAot=false
#:property UseRimWorldRefs=false

using System.Diagnostics;
using System.Xml.Linq;

// Runs DevTools' tests with only Harmony, Core, the installed DLCs and DevTools active, from a
// throwaway save data folder, so the mods enabled in your own profile can't change the result.
// From the repo root: dotnet run .githooks/Baseline.cs [path to the RimWorld executable]
// Without a path it uses DevTools.RimWorldExePath from the mod folder's .runsettings.

string[] dlcs = ["Royalty", "Ideology", "Biotech", "Anomaly", "Odyssey"];
TimeSpan timeout = TimeSpan.FromMinutes(5);
string modDir = Path.GetFullPath("..");
string settings = Path.Combine(modDir, ".runsettings");

string? exe = args.FirstOrDefault();
if (exe == null && File.Exists(settings))
{
  exe = XDocument.Load(settings).Descendants("Parameter")
    .FirstOrDefault(p => p.Attribute("name")?.Value == "DevTools.RimWorldExePath")?.Attribute("value")?.Value;
}
if (string.IsNullOrWhiteSpace(exe))
{
  Console.Error.WriteLine($"Pass the RimWorld executable, or set DevTools.RimWorldExePath in {settings}");
  return 2;
}
exe = Path.GetFullPath(Environment.ExpandEnvironmentVariables(exe.Trim()));

// Version.txt sits in the game folder: beside the exe on Windows, above RimWorld.app on macOS.
string? root = Path.GetDirectoryName(exe);
while (root != null && !File.Exists(Path.Combine(root, "Version.txt")))
  root = Path.GetDirectoryName(root);
if (root == null)
{
  Console.Error.WriteLine($"No Version.txt above {exe}");
  return 2;
}

// The build goes into this mod folder, so the game has to load DevTools from it.
string mods = Path.Combine(root, "Mods");
if (!Directory.Exists(mods) || !Directory.EnumerateDirectories(mods).Any(dir => string.Equals(
  new DirectoryInfo(dir).ResolveLinkTarget(true)?.FullName ?? dir, modDir, StringComparison.OrdinalIgnoreCase)))
{
  Console.Error.WriteLine($"{mods} doesn't hold {modDir}, so the game would test another DevTools");
  return 2;
}

string[] expansions = dlcs.Where(dlc => Directory.Exists(Path.Combine(root, "Data", dlc)))
  .Select(dlc => $"ludeon.rimworld.{dlc.ToLowerInvariant()}").ToArray();
string[] active = ["brrainz.harmony", "ludeon.rimworld", .. expansions, "smashphil.devtools"];

DirectoryInfo profile = Directory.CreateTempSubdirectory("devtools-baseline-");
try
{
  Directory.CreateDirectory(Path.Combine(profile.FullName, "Config"));
  new XElement("ModsConfigData",
    new XElement("version", File.ReadAllText(Path.Combine(root, "Version.txt")).Trim()),
    new XElement("activeMods", active.Select(mod => new XElement("li", mod))),
    new XElement("knownExpansions", expansions.Select(mod => new XElement("li", mod))))
    .Save(Path.Combine(profile.FullName, "Config", "ModsConfig.xml"));

  ProcessStartInfo startInfo = new(exe,
    ["--pid", "SmashPhil.DevTools", "--test", "-e", "-Headless", $"-savedatafolder={profile.FullName}"])
  {
    WorkingDirectory = root,
    CreateNoWindow = true,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
  };

  Console.WriteLine($"Testing with {string.Join(", ", active)}");
  using Process game = Process.Start(startInfo)!;
  game.BeginOutputReadLine();
  game.BeginErrorReadLine();
  if (!game.WaitForExit(timeout))
  {
    game.Kill(entireProcessTree: true);
    game.WaitForExit();
    Console.Error.WriteLine($"Still running after {timeout.TotalMinutes} minutes, stopped");
    return 1;
  }
  Console.WriteLine(game.ExitCode switch
  {
    0 => "Passed",
    1 => "Failed, see Test.log",
    2 => "No tests ran",
    int code => $"Exited with {code}, see Player.log",
  });
  return game.ExitCode is 0 or 1 or 2 ? game.ExitCode : 1;
}
finally
{
  try
  {
    profile.Delete(recursive: true);
  }
  catch (IOException)
  {
  }
}
