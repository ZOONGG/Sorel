using Sorel.Services;
using Velopack;
using Velopack.Sources;
using Velopack.Locators;
using Sorel.Infrastructure;
using Sorel.Models;

if (args[1] is "seed" or "verify") {
    var paths = new AppPaths(args[0]);
    paths.EnsureUserDirectories();
    var settings = new AppSettingsService(paths.SettingsPath);
    var library = new LocalPackageLibrary(paths, new JarMetadataParser());
    library.Load();
    if (args[1] == "seed") {
        settings.Save(new SorelSettings { Language = "ru" });
        var imported = library.Import(args[2], "update-smoke");
        if (imported.Package is null) throw new Exception("Import failed.");
        library.SetEnabled(imported.Package, true);
    } else {
        if (settings.Load().Language != "ru" || library.Packages.Count != 1 || !library.Packages[0].IsEnabled)
            throw new Exception("Settings/library selection were not preserved.");
        var package = PackageCatalogService.LoadBundledCatalog().Single(p => p.Id == "veyra");
        if (package.VersionLabel != "1.0.1") throw new Exception("Old Veyra catalog.");
        Console.WriteLine("PASS settings and enabled library reloaded; new Veyra catalog available");
    }
    return;
}

// Run only from current/ of a disposable, genuinely installed Velopack package.
// The default Windows locator reads its installed manifest; no mock locator is used.
IUpdateSource source = args[0].StartsWith("https://", StringComparison.Ordinal)
    ? new GithubSource(args[0], accessToken: null, prerelease: true)
    : new SimpleFileSource(new DirectoryInfo(args[0]));
var manager = new UpdateManager(source,
    new UpdateOptions { ExplicitChannel = "stable", AllowVersionDowngrade = false }, new WindowsVelopackLocator(null, null));
if (!manager.IsInstalled || manager.IsPortable) throw new Exception("Real installation required.");
var service = new UpdateService(manager, new UpdateActivityGate(), () => false);
Console.WriteLine($"Installed: {manager.CurrentVersion}; pending: {service.PendingRestart?.Version}");
if (args[1] == "none") {
    if (await service.CheckAsync() is not null || service.PendingRestart is not null)
        throw new Exception("Unexpected update/pending package.");
    Console.WriteLine("PASS no newer update");
    return;
}
if (args[1] == "bad") {
    try {
        await service.CheckAndDownloadAsync(true);
    } catch (Exception e) {
        if (service.PendingRestart is not null) throw new Exception("Corrupt download staged.");
        Console.WriteLine($"PASS download rejected: {e.GetType().Name}");
        return;
    }
    throw new Exception("Expected download failure.");
}
var update = await service.CheckAndDownloadAsync(true, u => Console.WriteLine($"Found: {u.TargetFullRelease.Version}"));
if (update is null || service.PendingRestart?.Version.ToString() != "1.0.1")
    throw new Exception("Expected 1.0.1 staged.");
Console.WriteLine("PASS real download and pending restart");
if (args[1] == "apply") {
    Console.WriteLine("Applying through Sorel UpdateService and native Update.exe");
    if (!service.TryApplyAndRestart()) throw new Exception(service.RestartBlockReason);
}
