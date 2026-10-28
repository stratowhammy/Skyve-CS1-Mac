using Extensions;
using Skyve.Domain.Systems;
using Skyve.Systems.CS1.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Skyve.App.CS1.Mac.ViewModels;

public class HelpLogsViewModel : ViewModelBase
{
	private readonly MacPatchLoaderService _patchLoaderService;
	private readonly ILocationManager _locationManager;
	private readonly ILogUtil _logUtil;
	private readonly ILogger _logger;

	private string _patchStatusMessage = string.Empty;
	private string _playerLogExcerpt = string.Empty;

	public HelpLogsViewModel(
		MacPatchLoaderService patchLoaderService,
		ILocationManager locationManager,
		ILogUtil logUtil,
		ILogger logger)
	{
		_patchLoaderService = patchLoaderService;
		_locationManager = locationManager;
		_logUtil = logUtil;
		_logger = logger;

		RefreshAll();
	}

	public string GamePath => _locationManager.GamePath;
	public string AppDataPath => _locationManager.AppDataPath;
	public string SteamPath => _locationManager.SteamPath;
	public string WorkshopPath => _locationManager.WorkshopContentPath;

	public bool IsPatchLoaderModInstalled => _patchLoaderService.IsPatchLoaderModInstalled;
	public bool IsDoorstopInstalled => _patchLoaderService.IsDoorstopInstalled;
	public bool IsLoaderScriptPresent => _patchLoaderService.IsLoaderScriptPresent;
	public bool IsSystemVersionCompatConfigured => _patchLoaderService.IsSystemVersionCompatConfigured;
	public bool IsLauncherSettingsPatched => _patchLoaderService.IsLauncherSettingsPatched;
	public bool IsDuplicateWorkshopSkyvePresent => _patchLoaderService.IsDuplicateWorkshopSkyvePresent;

	public string PatchStatusMessage
	{
		get => _patchStatusMessage;
		set => SetField(ref _patchStatusMessage, value);
	}

	public string PlayerLogExcerpt
	{
		get => _playerLogExcerpt;
		set => SetField(ref _playerLogExcerpt, value);
	}

	public void RefreshAll()
	{
		OnPropertyChanged(nameof(GamePath));
		OnPropertyChanged(nameof(AppDataPath));
		OnPropertyChanged(nameof(SteamPath));
		OnPropertyChanged(nameof(WorkshopPath));
		OnPropertyChanged(nameof(IsPatchLoaderModInstalled));
		OnPropertyChanged(nameof(IsDoorstopInstalled));
		OnPropertyChanged(nameof(IsLoaderScriptPresent));
		OnPropertyChanged(nameof(IsSystemVersionCompatConfigured));
		OnPropertyChanged(nameof(IsLauncherSettingsPatched));
		OnPropertyChanged(nameof(IsDuplicateWorkshopSkyvePresent));

		LoadPlayerLog();
	}

	public void FixMacPatchLoader()
	{
		try
		{
			var (success, message) = _patchLoaderService.FixAll();
			PatchStatusMessage = message;
			RefreshAll();
		}
		catch (Exception ex)
		{
			PatchStatusMessage = "Error fixing Mac loader: " + ex.Message;
		}
	}

	public void ExportLogZip()
	{
		try
		{
			var zip = _logUtil.CreateZipFileAndSetToClipboard();
			PatchStatusMessage = $"Log report exported to: {zip} (Copied to clipboard)";
		}
		catch (Exception ex)
		{
			PatchStatusMessage = "Error exporting logs: " + ex.Message;
		}
	}

	public void LoadPlayerLog()
	{
		try
		{
			var logPath = _logUtil.GameLogFile;
			if (File.Exists(logPath))
			{
				var lines = System.Linq.Enumerable.TakeLast(File.ReadLines(logPath), 200);
				PlayerLogExcerpt = string.Join("\n", lines);
			}
			else
			{
				PlayerLogExcerpt = $"Player.log not found at: {logPath}";
			}
		}
		catch (Exception ex)
		{
			PlayerLogExcerpt = $"Failed to read Player.log: {ex.Message}";
		}
	}

	public void OpenFolder(string? path)
	{
		try
		{
			if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
			{
				Process.Start("/usr/bin/open", new[] { path });
			}
		}
		catch { }
	}
}
