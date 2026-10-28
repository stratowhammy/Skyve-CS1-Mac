using Extensions;

using Skyve.Domain.Systems;

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Skyve.Systems.CS1.Services;

public class MacPatchLoaderService
{
	private readonly ILocationManager _locationManager;
	private readonly ILogger _logger;

	public MacPatchLoaderService(ILocationManager locationManager, ILogger logger)
	{
		_locationManager = locationManager;
		_logger = logger;
	}

	public string GamePath => _locationManager.GamePath;
	public string LoaderScriptPath => CrossIO.Combine(GamePath, "Cities_Loader.sh");
	public string DoorstopDylibPath => CrossIO.Combine(GamePath, "doorstop.dylib");
	public string LauncherSettingsPath => CrossIO.Combine(GamePath, "Cities.app", "Contents", "Launcher", "launcher-settings.json");
	public string LauncherSettingsBackupPath => CrossIO.Combine(GamePath, "Cities.app", "Contents", "Launcher", "launcher-settings.json.bak");
	public string WorkshopPatchLoaderDll => CrossIO.Combine(_locationManager.WorkshopContentPath, "2041457644", "PatchLoader", "PatchLoader.dll");
	public string WorkshopSkyveFolder => CrossIO.Combine(_locationManager.WorkshopContentPath, "2881031511");
	public string WorkshopSkyveIpatch => CrossIO.Combine(WorkshopSkyveFolder, "Skyve-CS1.ipatch");

	public bool IsDuplicateWorkshopSkyvePresent => Directory.Exists(WorkshopSkyveFolder) && CrossIO.FileExists(WorkshopSkyveIpatch);

	public bool IsPatchLoaderModInstalled =>
		CrossIO.FileExists(WorkshopPatchLoaderDll) ||
		CrossIO.FileExists(CrossIO.Combine(_locationManager.ModsPath, "PatchLoader", "PatchLoader.dll")) ||
		CrossIO.FileExists(CrossIO.Combine(_locationManager.ModsPath, "PatchLoaderMod.dll"));

	public bool IsDoorstopInstalled => CrossIO.FileExists(DoorstopDylibPath);

	public bool IsLoaderScriptPresent => CrossIO.FileExists(LoaderScriptPath);

	public bool IsSystemVersionCompatConfigured
	{
		get
		{
			if (!IsLoaderScriptPresent) return false;
			try
			{
				var content = File.ReadAllText(LoaderScriptPath);
				return content.Contains("SYSTEM_VERSION_COMPAT=1");
			}
			catch { return false; }
		}
	}

	public bool IsLauncherSettingsPatched
	{
		get
		{
			if (!CrossIO.FileExists(LauncherSettingsPath)) return false;
			try
			{
				var content = File.ReadAllText(LauncherSettingsPath);
				return content.Contains("\"../../../Cities_Loader.sh\"");
			}
			catch { return false; }
		}
	}

	public (bool Success, string Message) PatchLoaderScript()
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			return (false, "Not on macOS");
		}

		try
		{
			if (!CrossIO.FileExists(LoaderScriptPath))
			{
				var patchLoaderDll = CrossIO.FileExists(WorkshopPatchLoaderDll)
					? WorkshopPatchLoaderDll
					: CrossIO.Combine(_locationManager.ModsPath, "PatchLoader", "PatchLoader.dll");

				var defaultScript =
					"#!/bin/sh\n" +
					"doorstop_libname=\"doorstop.dylib\"\n" +
					"doorstop_dir=$PWD\n" +
					"export DYLD_LIBRARY_PATH=${doorstop_dir}:${DYLD_LIBRARY_PATH};\n" +
					"export DYLD_INSERT_LIBRARIES=$doorstop_libname;\n" +
					"export DOORSTOP_ENABLE=TRUE;\n" +
					$"export DOORSTOP_INVOKE_DLL_PATH=\"{patchLoaderDll}\";\n" +
					"export SYSTEM_VERSION_COMPAT=1;\n" +
					"./Cities.app/Contents/MacOS/Cities $@\n";

				File.WriteAllText(LoaderScriptPath, defaultScript);
				_logger.Info("Created Cities_Loader.sh");
			}
			else
			{
				var content = File.ReadAllText(LoaderScriptPath);
				if (!content.Contains("SYSTEM_VERSION_COMPAT=1"))
				{
					if (content.Contains("./Cities.app/Contents/MacOS/Cities"))
					{
						content = content.Replace("./Cities.app/Contents/MacOS/Cities", "export SYSTEM_VERSION_COMPAT=1;\n./Cities.app/Contents/MacOS/Cities");
					}
					else
					{
						content += "\nexport SYSTEM_VERSION_COMPAT=1;\n";
					}
					File.WriteAllText(LoaderScriptPath, content);
					_logger.Info("Patched SYSTEM_VERSION_COMPAT=1 into Cities_Loader.sh");
				}
			}

			MakeExecutable(LoaderScriptPath);

			StripQuarantine(LoaderScriptPath);
			if (CrossIO.FileExists(DoorstopDylibPath))
			{
				StripQuarantine(DoorstopDylibPath);
			}

			return (true, "Cities_Loader.sh configured successfully with SYSTEM_VERSION_COMPAT=1.");
		}
		catch (Exception ex)
		{
			_logger.Exception(ex, "Failed to patch Cities_Loader.sh");
			return (false, ex.Message);
		}
	}

	public (bool Success, string Message) PatchLauncherSettings()
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			return (false, "Not on macOS");
		}

		try
		{
			if (!CrossIO.FileExists(LauncherSettingsPath))
			{
				return (false, "launcher-settings.json not found.");
			}

			var content = File.ReadAllText(LauncherSettingsPath);

			if (!CrossIO.FileExists(LauncherSettingsBackupPath))
			{
				File.Copy(LauncherSettingsPath, LauncherSettingsBackupPath);
				_logger.Info("Backed up launcher-settings.json");
			}

			var updated = Regex.Replace(content, "\"exePath\"\\s*:\\s*\"[^\"]+\"", "\"exePath\": \"../../../Cities_Loader.sh\"");
			File.WriteAllText(LauncherSettingsPath, updated);
			_logger.Info("Patched exePath in launcher-settings.json");

			return (true, "launcher-settings.json patched successfully to route through Cities_Loader.sh.");
		}
		catch (Exception ex)
		{
			_logger.Exception(ex, "Failed to patch launcher-settings.json");
			return (false, ex.Message);
		}
	}

	public (bool Success, string Message) RestoreLauncherSettings()
	{
		try
		{
			if (CrossIO.FileExists(LauncherSettingsBackupPath))
			{
				File.Copy(LauncherSettingsBackupPath, LauncherSettingsPath, true);
				return (true, "launcher-settings.json restored from backup.");
			}
			return (false, "No backup file found.");
		}
		catch (Exception ex)
		{
			return (false, ex.Message);
		}
	}

	public void StripQuarantine(string path)
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "/usr/bin/xattr",
				Arguments = $"-dr com.apple.quarantine \"{path}\"",
				UseShellExecute = false,
				CreateNoWindow = true
			})?.WaitForExit(3000);
		}
		catch { }
	}

	public void MakeExecutable(string path)
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;
		try
		{
			var mode = File.GetUnixFileMode(path);
			File.SetUnixFileMode(path, mode | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
		}
		catch
		{
			try
			{
				Process.Start("/bin/chmod", $"+x \"{path}\"")?.WaitForExit(3000);
			}
			catch { }
		}
	}

	public (bool Success, string Message) FixAll()
	{
		var loaderResult = PatchLoaderScript();
		var launcherResult = PatchLauncherSettings();

		var skyveFolder = _locationManager.SkyveAppDataPath;
		if (Directory.Exists(skyveFolder))
		{
			StripQuarantine(skyveFolder);
		}
		var modFolder = CrossIO.Combine(_locationManager.ModsPath, "Skyve");
		if (Directory.Exists(modFolder))
		{
			StripQuarantine(modFolder);
		}

		if (CrossIO.FileExists(WorkshopSkyveIpatch))
		{
			try
			{
				var disabledIpatch = CrossIO.Combine(WorkshopSkyveFolder, "Skyve-CS1.ipatch.disabled");
				if (CrossIO.FileExists(disabledIpatch))
				{
					File.Delete(disabledIpatch);
				}
				File.Move(WorkshopSkyveIpatch, disabledIpatch);
				_logger.Info("Disabled duplicate Workshop Skyve-CS1.ipatch to prevent Patch Loader collision.");
			}
			catch (Exception ex)
			{
				_logger.Exception(ex, "Failed to disable duplicate Workshop Skyve-CS1.ipatch");
			}
		}

		if (loaderResult.Success && launcherResult.Success)
		{
			return (true, "All Mac Doorstop & Patch Loader fixes applied successfully!");
		}

		return (false, $"{loaderResult.Message}\n{launcherResult.Message}");
	}
}
