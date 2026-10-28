using Extensions;

using Microsoft.Win32;

using Skyve.Domain;
using Skyve.Domain.CS1.Notifications;
using Skyve.Domain.Systems;

using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Skyve.Systems.CS1.Managers;
internal class LocationManager : ILocationManager
{
	internal const string LOCAL_APP_DATA_PATH = "%LOCALAPPDATA%";
	internal const string CITIES_PATH = "%CITIES%";
	internal const string WS_CONTENT_PATH = "%WORKSHOP%";

	private readonly ILogger _logger;
	private readonly ISettings _settings;

	// Base Folders
	public string GamePath { get; set; }
	public string AppDataPath { get; set; }
	public string SteamPath { get; set; }

	public string DataPath => CrossIO.CurrentPlatform == Platform.MacOSX
		? CrossIO.Combine(GamePath, "Cities.app", "Contents", "Resources", "Data")
		: CrossIO.Combine(GamePath, "Cities_Data");

	public string ManagedDLL => CrossIO.Combine(DataPath, "Managed");

	public string MonoPath => CrossIO.CurrentPlatform == Platform.MacOSX
		? CrossIO.Combine(GamePath, "Cities.app", "Contents", "Frameworks", "MonoEmbedRuntime", "osx", "libmono.0.dylib")
		: CrossIO.Combine(DataPath, "Mono");
	public string AddonsPath => CrossIO.Combine(AppDataPath, "Addons");
	public string SkyveAppDataPath => CrossIO.Combine(AppDataPath, "Skyve");
	public string SkyvePlaysetsAppDataPath => CrossIO.Combine(SkyveAppDataPath, "Profiles");
	public string ModsPath => CrossIO.Combine(AddonsPath, "Mods");
	public string AssetsPath => CrossIO.Combine(AddonsPath, "Assets");
	public string MapThemesPath => CrossIO.Combine(AddonsPath, "MapThemes");
	public string MapsPath => CrossIO.Combine(AppDataPath, "Maps");
	public string StylesPath => CrossIO.Combine(AddonsPath, "Styles");

	public string WorkshopContentPath
	{
		get
		{
			if (string.IsNullOrWhiteSpace(GamePath))
			{
				return string.Empty;
			}

			var parent = Path.GetDirectoryName(Path.GetDirectoryName(GamePath));

			if (string.IsNullOrEmpty(parent))
			{
				return string.Empty;
			}

			return CrossIO.Combine(parent, "workshop", "content", "255710").FormatPath();
		}
	}

	public string GameContentPath
	{
		get
		{
			if (CrossIO.CurrentPlatform == Platform.MacOSX)
			{
				return CrossIO.Combine(GamePath, "Cities.app", "Contents", "Resources", "Files");
			}

			return CrossIO.Combine(GamePath, "Files");
		}
	}

	public string SteamPathWithExe => CrossIO.Combine(SteamPath, SteamExe);

	public string CitiesPathWithExe => CrossIO.Combine(GamePath, CitiesExe);

	private string CitiesExe => CrossIO.CurrentPlatform switch
	{
		Platform.MacOSX => "Cities_Loader.sh",
		Platform.Linux => "Cities.x64",
		Platform.Windows or _ => "Cities.exe",
	};

	private string SteamExe => CrossIO.CurrentPlatform switch
	{
		Platform.MacOSX => "steam_osx",
		Platform.Linux => string.Empty,
		Platform.Windows or _ => "Steam.exe",
	};

	public LocationManager(ILogger logger, ISettings settings, INotificationsService notificationsService)
	{
		_logger = logger;
		_settings = settings;

		GamePath = _settings.FolderSettings.GamePath?.FormatPath() ?? string.Empty;
		AppDataPath = _settings.FolderSettings.AppDataPath?.FormatPath() ?? string.Empty;
		SteamPath = _settings.FolderSettings.SteamPath?.FormatPath() ?? string.Empty;

		if ((string.IsNullOrEmpty(GamePath) || !Directory.Exists(GamePath)) && CrossIO.CurrentPlatform == Platform.MacOSX)
		{
			SteamPath = FindSteamPath(_settings.FolderSettings);
			GamePath = FindGamePath(SteamPath);
			AppDataPath = GetDefaultAppDataPath();

			if (!string.IsNullOrEmpty(GamePath) && Directory.Exists(GamePath))
			{
				_settings.FolderSettings.GamePath = GamePath;
				_settings.FolderSettings.AppDataPath = AppDataPath;
				_settings.FolderSettings.SteamPath = SteamPath;
				_settings.FolderSettings.Save();
			}
		}

		if (_settings.FolderSettings.GamePath is null)
		{
			GamePath = string.Empty;
			AppDataPath = string.Empty;
			SteamPath = string.Empty;
			return;
		}

		SetCorrectPathSeparator();

		if (_settings.SessionSettings.FirstTimeSetupCompleted)
		{
			if (!CrossIO.FileExists(CitiesPathWithExe) || !Directory.Exists(AppDataPath) || (!string.IsNullOrEmpty(SteamPath) && !CrossIO.FileExists(SteamPathWithExe)))
			{
				notificationsService.SendNotification(new IncorrectLocationSettingsNotification());
			}
		}

		_logger.Info("Folder Settings:\r\n" +
			$"Platform: {CrossIO.CurrentPlatform}\r\n" +
			$"GamePath: {GamePath}\r\n" +
			$"AppDataPath: {AppDataPath}\r\n" +
			$"GameContentPath: {GameContentPath}\r\n" +
			$"SteamPath: {SteamPath}\r\n" +
			$"WorkshopContentPath: {WorkshopContentPath}");

		try
		{
			if (Directory.Exists(CrossIO.Combine(AppDataPath, "LoadOrderTwo")))
			{
				CrossIO.MoveFolder(CrossIO.Combine(AppDataPath, "LoadOrderTwo"), SkyveAppDataPath, false);

				if (CrossIO.FileExists(CrossIO.Combine(SkyveAppDataPath, "LoadOrderConfig.xml")) && !CrossIO.FileExists(CrossIO.Combine(SkyveAppDataPath, "SkyveConfig.xml")))
				{
					File.Move(CrossIO.Combine(SkyveAppDataPath, "LoadOrderConfig.xml"), CrossIO.Combine(SkyveAppDataPath, "SkyveConfig.xml"));
				}

				CrossIO.DeleteFolder(CrossIO.Combine(AppDataPath, "LoadOrderTwo"));

				if (CrossIO.CurrentPlatform is Platform.Windows)
				{
					CreateShortcut();
				}
			}
		}
		catch (Exception ex) { _logger.Exception(ex, "Failed to copy previous settings over"); }
	}

	public void RunFirstTimeSetup()
	{
		var settings = _settings.FolderSettings;

		var platformStr = ConfigurationManager.AppSettings[nameof(Platform)];
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			settings.Platform = Platform.MacOSX;
		}
		else if (Enum.TryParse(platformStr, out Platform platform))
		{
			settings.Platform = platform;
		}
		else
		{
			settings.Platform = CrossIO.CurrentPlatform;
		}

		CrossIO.CurrentPlatform = settings.Platform;

		settings.GamePath = ConfigurationManager.AppSettings[nameof(GamePath)] ?? settings.GamePath ?? string.Empty;
		settings.AppDataPath = ConfigurationManager.AppSettings[nameof(AppDataPath)] ?? settings.AppDataPath ?? string.Empty;
		settings.SteamPath = ConfigurationManager.AppSettings[nameof(SteamPath)] ?? settings.SteamPath ?? string.Empty;

		if (settings.Platform is Platform.MacOSX)
		{
			_logger.Info("Matching macOS Paths");

			if (string.IsNullOrEmpty(settings.SteamPath) || !Directory.Exists(settings.SteamPath))
			{
				settings.SteamPath = FindSteamPath(settings);
			}

			if (string.IsNullOrEmpty(settings.AppDataPath) || !Directory.Exists(settings.AppDataPath))
			{
				settings.AppDataPath = GetDefaultAppDataPath();
			}

			if (string.IsNullOrEmpty(settings.GamePath) || !Directory.Exists(settings.GamePath))
			{
				settings.GamePath = FindGamePath(settings.SteamPath);
			}
			else if (settings.GamePath.Contains("Cities.app"))
			{
				var p = settings.GamePath.Replace('\\', '/');
				var idx = p.IndexOf("/Cities.app");
				if (idx > 0)
				{
					settings.GamePath = p.Substring(0, idx);
				}
			}
		}

		_logger.Info("FTS Folder Settings:\r\n" +
			$"Platform: {settings.Platform}\r\n" +
			$"GamePath: {settings.GamePath}\r\n" +
			$"AppDataPath: {settings.AppDataPath}\r\n" +
			$"SteamPath: {settings.SteamPath}");

		try
		{
			if (string.IsNullOrEmpty(settings.SteamPath))
			{
				settings.SteamPath = FindSteamPath(settings);
			}
		}
		catch (Exception ex) { _logger.Exception(ex, "Failed to find steam's installation folder"); }

		if (settings.Platform is not Platform.Windows)
		{
			settings.GamePath = settings.GamePath?.Replace('\\', '/') ?? string.Empty;
			settings.AppDataPath = settings.AppDataPath?.Replace('\\', '/') ?? string.Empty;
			settings.SteamPath = settings.SteamPath?.Replace('\\', '/') ?? string.Empty;
		}

		settings.Save();

		GamePath = settings.GamePath.TrimEnd('/', '\\');
		AppDataPath = settings.AppDataPath.TrimEnd('/', '\\');
		SteamPath = settings.SteamPath.TrimEnd('/', '\\');

		try
		{
			var externalConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			var appSettings = externalConfig.AppSettings;
			if (appSettings?.Settings != null)
			{
				if (appSettings.Settings[nameof(GamePath)] != null) appSettings.Settings[nameof(GamePath)].Value = string.Empty;
				if (appSettings.Settings[nameof(AppDataPath)] != null) appSettings.Settings[nameof(AppDataPath)].Value = string.Empty;
				if (appSettings.Settings[nameof(SteamPath)] != null) appSettings.Settings[nameof(SteamPath)].Value = string.Empty;
				externalConfig.Save();
			}
		}
		catch { }

		SetCorrectPathSeparator();

		Directory.CreateDirectory(SkyveAppDataPath);

		if (File.Exists(CrossIO.Combine(AppDataPath, "LoadOrder", "LoadOrderConfig.xml")) && !File.Exists(CrossIO.Combine(SkyveAppDataPath, "SkyveConfig.xml")))
		{
			CrossIO.CopyFile(CrossIO.Combine(AppDataPath, "LoadOrder", "LoadOrderConfig.xml"), CrossIO.Combine(SkyveAppDataPath, "SkyveConfig.xml"), true);
		}
	}

	public static string GetDefaultAppDataPath()
	{
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Colossal Order", "Cities_Skylines").FormatPath();
	}

	public static string FindGamePath(string steamPath)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(steamPath))
			{
				steamPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Steam");
			}

			var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
			if (File.Exists(vdfPath))
			{
				var lines = File.ReadAllLines(vdfPath);
				string? currentPath = null;
				foreach (var line in lines)
				{
					var trimmed = line.Trim();
					if (trimmed.StartsWith("\"path\"", StringComparison.OrdinalIgnoreCase))
					{
						var parts = trimmed.Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries);
						if (parts.Length >= 2)
						{
							currentPath = parts[parts.Length - 1].Replace("\\\\", "/").Replace('\\', '/');
						}
					}
					else if (trimmed.StartsWith("\"255710\"", StringComparison.OrdinalIgnoreCase) && currentPath != null)
					{
						var candidate = Path.Combine(currentPath, "steamapps", "common", "Cities_Skylines");
						if (Directory.Exists(candidate))
						{
							return candidate.FormatPath();
						}
					}
				}
			}

			var defaultPath = Path.Combine(steamPath, "steamapps", "common", "Cities_Skylines");
			if (Directory.Exists(defaultPath))
			{
				return defaultPath.FormatPath();
			}
		}
		catch { }

		return string.Empty;
	}

	private string FindSteamPath(IFolderSettings settings)
	{
		_logger.Info("Finding steam's path");

		if (settings.Platform is Platform.Windows)
		{
			const string steamPathSubKey_ = @"Software\Valve\Steam";
			const string steamPathKey_ = "SteamPath";

			using var key = Registry.CurrentUser.OpenSubKey(steamPathSubKey_);
			var path = key?.GetValue(steamPathKey_) as string;

			if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
			{
				return path![0].ToString().ToUpper() + path!.FormatPath().Substring(1);
			}
		}

		if (settings.Platform is Platform.MacOSX || CrossIO.CurrentPlatform is Platform.MacOSX)
		{
			var userSteam = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Steam");
			if (Directory.Exists(userSteam))
			{
				return userSteam.FormatPath();
			}

			var basePath = "/Applications/Steam.app";
			if (Directory.Exists(basePath))
			{
				return basePath.FormatPath();
			}
		}

		return settings.SteamPath.FormatPath();
	}

	public void SetPaths(string gamePath, string appDataPath, string steamPath)
	{
		_settings.FolderSettings.GamePath = gamePath;
		_settings.FolderSettings.AppDataPath = appDataPath;
		_settings.FolderSettings.SteamPath = steamPath;

		_settings.FolderSettings.Save();
	}

	private void SetCorrectPathSeparator()
	{
	}

	public void CreateShortcut()
	{
		try
		{
			if (CrossIO.CurrentPlatform is Platform.Windows)
			{
				ExtensionClass.CreateShortcut(CrossIO.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Skyve CS-I.lnk"), AppContext.BaseDirectory);
			}
			else if (CrossIO.CurrentPlatform is Platform.MacOSX)
			{
				var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				var target = "/Applications/Skyve.app";
				if (Directory.Exists(target))
				{
					CrossIO.CreateSymbolicLink(CrossIO.Combine(desktop, "Skyve"), target);
				}
			}
		}
		catch (Exception ex)
		{
			_logger.Exception(ex, "Failed to create shortcut");
		}
	}

	public string ToRelativePath(string? localPath)
	{
		if (localPath is null or "")
		{
			return string.Empty;
		}

		return localPath
			.Replace(AppDataPath, LOCAL_APP_DATA_PATH)
			.Replace(GamePath, CITIES_PATH)
			.Replace(WorkshopContentPath, WS_CONTENT_PATH)
			.FormatPath();
	}

	public string ToLocalPath(string? relativePath)
	{
		if (relativePath is null or "")
		{
			return string.Empty;
		}

		return relativePath
			.Replace(LOCAL_APP_DATA_PATH, AppDataPath)
			.Replace(CITIES_PATH, GamePath)
			.Replace(WS_CONTENT_PATH, WorkshopContentPath)
			.FormatPath();
	}
}
