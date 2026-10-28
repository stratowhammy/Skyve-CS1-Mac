using Avalonia;
using Skyve.Domain;
using Skyve.Systems.CS1.Services;
using System;

namespace Skyve.App.CS1.Mac;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length > 0 && (args[0] == "--fix-patch-loader" || args[0] == "--status" || args[0] == "--verify"))
        {
            App.InitializeServices();
            var patchService = ServiceCenter.Get<MacPatchLoaderService>();
            if (args[0] == "--fix-patch-loader")
            {
                var (success, msg) = patchService.FixAll();
                Console.WriteLine("FixAll Result: " + success + " - " + msg);
            }
            Console.WriteLine("PatchLoaderModInstalled: " + patchService.IsPatchLoaderModInstalled);
            Console.WriteLine("DoorstopInstalled: " + patchService.IsDoorstopInstalled);
            Console.WriteLine("LoaderScriptPresent: " + patchService.IsLoaderScriptPresent);
            Console.WriteLine("SystemVersionCompat: " + patchService.IsSystemVersionCompatConfigured);
            Console.WriteLine("LauncherSettingsPatched: " + patchService.IsLauncherSettingsPatched);
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
