using Skyve.Domain.Systems;
using Skyve.Systems.CS1.Services;
using System;
using System.Timers;

namespace Skyve.App.CS1.Mac.ViewModels;

public class MainViewModel : ViewModelBase
{
	private readonly ICitiesManager _citiesManager;
	private ViewModelBase _currentView;
	private string _currentTab = "Dashboard";
	private bool _isGameRunning;
	private readonly Timer _statusTimer;

	public MainViewModel(
		DashboardViewModel dashboardVm,
		ModsViewModel modsVm,
		AssetsViewModel assetsVm,
		CompatibilityViewModel compatibilityVm,
		HelpLogsViewModel helpLogsVm,
		ICitiesManager citiesManager)
	{
		Dashboard = dashboardVm;
		Mods = modsVm;
		Assets = assetsVm;
		Compatibility = compatibilityVm;
		HelpLogs = helpLogsVm;
		_citiesManager = citiesManager;

		_currentView = Dashboard;

		_statusTimer = new Timer(1500);
		_statusTimer.Elapsed += (s, e) => CheckGameStatus();
		_statusTimer.Start();
		CheckGameStatus();
	}

	public DashboardViewModel Dashboard { get; }
	public ModsViewModel Mods { get; }
	public AssetsViewModel Assets { get; }
	public CompatibilityViewModel Compatibility { get; }
	public HelpLogsViewModel HelpLogs { get; }

	public ViewModelBase CurrentView
	{
		get => _currentView;
		set => SetField(ref _currentView, value);
	}

	public string CurrentTab
	{
		get => _currentTab;
		set => SetField(ref _currentTab, value);
	}

	public bool IsGameRunning
	{
		get => _isGameRunning;
		set
		{
			if (SetField(ref _isGameRunning, value))
			{
				OnPropertyChanged(nameof(GameStatusText));
				OnPropertyChanged(nameof(GameStatusColor));
				OnPropertyChanged(nameof(CanLaunch));
			}
		}
	}

	public string GameStatusText => IsGameRunning ? "Game Running" : "Ready";
	public string GameStatusColor => IsGameRunning ? "#98C379" : "#5C6370";
	public bool CanLaunch => !IsGameRunning;

	public void NavigateTo(string tab)
	{
		CurrentTab = tab;
		CurrentView = tab switch
		{
			"Dashboard" => Dashboard,
			"Mods" => Mods,
			"Assets" => Assets,
			"Compatibility" => Compatibility,
			"HelpLogs" => HelpLogs,
			_ => Dashboard
		};
	}

	public void LaunchGame()
	{
		try
		{
			_citiesManager.Launch();
			CheckGameStatus();
		}
		catch { }
	}

	public void StopGame()
	{
		try
		{
			_citiesManager.Kill();
			CheckGameStatus();
		}
		catch { }
	}

	private void CheckGameStatus()
	{
		try
		{
			IsGameRunning = _citiesManager.IsRunning();
		}
		catch { }
	}
}
