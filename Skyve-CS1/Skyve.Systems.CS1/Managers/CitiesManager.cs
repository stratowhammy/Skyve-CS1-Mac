using Extensions;

using Skyve.Domain;
using Skyve.Domain.CS1;
using Skyve.Domain.CS1.Utilities;
using Skyve.Domain.Systems;
using Skyve.Systems.CS1.Utilities;
using Skyve.Systems.CS1.Utilities.IO;

using SlickControls;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;

namespace Skyve.Systems.CS1.Managers;

internal class CitiesManager : ICitiesManager
{
	private readonly ILogger _logger;
	private readonly ILocationManager _locationManager;
	private readonly IPlaysetManager _profileManager;
	private readonly IPackageManager _contentManager;
	private readonly IModUtil _modUtil;
	private readonly ISettings _settings;
	private readonly IIOUtil _iOUtil;
	private readonly ColossalOrderUtil _colossalOrderUtil;

	public event MonitorTickDelegate? MonitorTick;

	public event Action<bool>? LaunchingStatusChanged;

	public CitiesManager(ILogger logger, ILocationManager locationManager, IPlaysetManager profileManager, ISettings settings, IPackageManager contentManager, IIOUtil iOUtil, IModUtil modUtil, ColossalOrderUtil colossalOrderUtil)
	{
		_logger = logger;
		_locationManager = locationManager;
		_profileManager = profileManager;
		_settings = settings;
		_contentManager = contentManager;
		_iOUtil = iOUtil;
		_modUtil = modUtil;
		_colossalOrderUtil = colossalOrderUtil;

		var citiesMonitorTimer = new Timer(1000);

		//if (CrossIO.CurrentPlatform is Platform.Windows)
		{
			citiesMonitorTimer.Elapsed += CitiesMonitorTimer_Elapsed;
			citiesMonitorTimer.Start();
		}
	}

	private void CitiesMonitorTimer_Elapsed(object sender, ElapsedEventArgs e)
	{
		MonitorTick?.Invoke(IsAvailable(), IsRunning());
	}

	public bool IsAvailable()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			var loaderScript = CrossIO.Combine(_locationManager.GamePath, "Cities_Loader.sh");
			var appBinary = CrossIO.Combine(_locationManager.GamePath, "Cities.app", "Contents", "MacOS", "Cities");
			return CrossIO.FileExists(loaderScript) || CrossIO.FileExists(appBinary);
		}

		var file = (_profileManager.CurrentPlayset as Playset)!.LaunchSettings.UseCitiesExe
			? _locationManager.CitiesPathWithExe
			: _locationManager.SteamPathWithExe;

		return CrossIO.FileExists(file);
	}

	public void Launch()
	{
		UpdateFiles();

		var args = GetCommandArgs();
		var useCitiesExe = (_profileManager.CurrentPlayset as Playset)!.LaunchSettings.UseCitiesExe;

		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			if (!useCitiesExe)
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "/usr/bin/open",
					Arguments = "steam://rungameid/255710",
					UseShellExecute = false
				});
				return;
			}
			else
			{
				var loaderScript = CrossIO.Combine(_locationManager.GamePath, "Cities_Loader.sh");
				var targetExecutable = CrossIO.FileExists(loaderScript)
					? loaderScript
					: CrossIO.Combine(_locationManager.GamePath, "Cities.app", "Contents", "MacOS", "Cities");

				var psi = new ProcessStartInfo
				{
					FileName = targetExecutable,
					Arguments = string.Join(" ", args),
					WorkingDirectory = _locationManager.GamePath,
					UseShellExecute = false
				};
				psi.EnvironmentVariables["SteamAppId"] = "255710";
				psi.EnvironmentVariables["SYSTEM_VERSION_COMPAT"] = "1";
				Process.Start(psi);
				return;
			}
		}

		var file = useCitiesExe
			? _locationManager.CitiesPathWithExe
			: _locationManager.SteamPathWithExe;

		_iOUtil.Execute(file, string.Join(" ", args));
	}

	private void UpdateFiles()
	{
		try
		{
			bool success;

			var launchSettings = (_profileManager.CurrentPlayset as Playset)!.LaunchSettings;

			if (launchSettings.DebugMono)
			{
				var sessionSettings = (_settings.SessionSettings as SessionSettings)!;

				if (!CommandUtil.NoWindow && !sessionSettings.FpsBoosterLogWarning)
				{
					var fpsBooster = _contentManager.Mods.FirstOrDefault(mod => Path.GetFileNameWithoutExtension(mod.FilePath) == "FPS_Booster");

					if (fpsBooster != null && _modUtil.IsIncluded(fpsBooster) && _modUtil.IsEnabled(fpsBooster))
					{
						var result = MessagePrompt.Show(Locale.DisableFpsBoosterDebug, PromptButtons.YesNo);

						if (result == DialogResult.Yes)
						{
							_modUtil.SetIncluded(fpsBooster, false);
						}
						else
						{
							sessionSettings.FpsBoosterLogWarning = true;
							sessionSettings.Save();
						}
					}
				}

				success = MonoFile.Instance.UseDebug();
			}
			else
			{
				success = MonoFile.Instance.UseRelease();
			}

			if (!success)
			{
				if (MonoFile.Instance.ReleaseIsUsed() is bool bReleaseMono)
				{
					_logger.Warning("reverting launchSettings.ReleaseMono to " + bReleaseMono);
					launchSettings.DebugMono = !bReleaseMono;
				}
			}

			if (launchSettings.UnityProfiler)
			{
				success = CitiesFile.Instance.UseDebug();
			}
			else
			{
				success = CitiesFile.Instance.UseRelease();
			}

			if (!success)
			{
				if (CitiesFile.Instance.ReleaseIsUsed() is bool bReleaseCities)
				{
					_logger.Warning("reverting launchSettings.ReleaseMono to " + bReleaseCities);
					launchSettings.UnityProfiler = !bReleaseCities;
				}
			}

			if (!_settings.UserSettings.AdvancedIncludeEnable)
			{
				foreach (var item in _contentManager.Mods)
				{
					_colossalOrderUtil.SetEnabled(item, item.IsIncluded());
				}

				_colossalOrderUtil.SaveSettings();
			}
		}
		catch (Exception ex)
		{
			_logger.Exception(ex, "Failed to update files");
		}
	}

	private string quote(string path)
	{
		return '"' + path + '"';
	}

	private string[] GetCommandArgs()
	{
		var args = new List<string>();

		var launchSettings = (_profileManager.CurrentPlayset as Playset)!.LaunchSettings;

		if (!launchSettings.UseCitiesExe)
		{
			args.Add("-applaunch 255710");
		}

		if (launchSettings.NoWorkshop)
		{
			args.Add("-noWorkshop");
		}

		if (launchSettings.ResetAssets)
		{
			args.Add("-reset-assets");
		}

		if (launchSettings.NoAssets)
		{
			args.Add("-noAssets");
		}

		if (launchSettings.NoMods)
		{
			args.Add("-disableMods");
		}

		if (launchSettings.LHT)
		{
			args.Add("-LHT");
		}

		if (launchSettings.DevUi)
		{
			args.Add("-enable-dev-ui");
		}

		if (launchSettings.RefreshWorkshop)
		{
			args.Add("-refreshWorkshop");
		}

		if (launchSettings.NewAsset)
		{
			args.Add("-newAsset");
		}

		if (launchSettings.LoadAsset)
		{
			args.Add("-loadAsset");
		}

		if (launchSettings.LoadSaveGame)
		{
			if (CrossIO.FileExists(launchSettings.SaveToLoad))
			{
				args.Add("--loadSave=" + quote(launchSettings.SaveToLoad!));
			}
			else
			{
				args.Add("-continuelastsave");
			}
		}
		else if (launchSettings.StartNewGame)
		{
			if (CrossIO.FileExists(launchSettings.MapToLoad))
			{
				args.Add("--newGame=" + quote(launchSettings.MapToLoad!));
			}
			else
			{
				args.Add("-newGame");
			}
		}

		if (!string.IsNullOrWhiteSpace(launchSettings.CustomArgs))
		{
			args.Add(launchSettings.CustomArgs!);
		}

		return args.ToArray();
	}

	public void RunStub()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "/usr/bin/open",
				Arguments = "steam://run/255710//-stub/",
				UseShellExecute = false
			});
			return;
		}

		var command = $"-applaunch 255710 -stub";

		var file = _locationManager.SteamPathWithExe;

		_iOUtil.Execute(file, command);
	}

	public bool IsRunning()
	{
		try
		{
			var procs = Process.GetProcessesByName("Cities");
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				return procs.Any(p =>
				{
					try { return p.MainModule?.FileName?.EndsWith("Cities.app/Contents/MacOS/Cities", StringComparison.OrdinalIgnoreCase) ?? true; }
					catch { return true; }
				});
			}
			return procs.Length > 0;
		}
		catch { return false; }
	}

	public void Kill()
	{
		try
		{
			foreach (var proc in Process.GetProcessesByName("Cities"))
			{
				KillProcessAndChildren(proc);
			}
		}
		catch (Exception ex) { _logger.Exception(ex, "Failed to kill C:S"); }
	}

	private void KillProcessAndChildren(Process proc)
	{
		foreach (var childProc in GetChildProcesses(proc))
		{
			KillProcessAndChildren(childProc);
		}

		proc.Kill();
	}

	private List<Process> GetChildProcesses(Process proc)
	{
		var childProcs = new List<Process>();

		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			try
			{
				var psi = new ProcessStartInfo
				{
					FileName = "/usr/bin/pgrep",
					Arguments = $"-P {proc.Id}",
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true
				};
				using var pgrep = Process.Start(psi);
				if (pgrep != null)
				{
					var output = pgrep.StandardOutput.ReadToEnd();
					pgrep.WaitForExit();
					foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
					{
						if (int.TryParse(line.Trim(), out var childPid))
						{
							try { childProcs.Add(Process.GetProcessById(childPid)); } catch { }
						}
					}
				}
			}
			catch { }
		}

		return childProcs;
	}

	public void SetLaunchingStatus(bool launching)
	{
		LaunchingStatusChanged?.Invoke(launching);
	}
}