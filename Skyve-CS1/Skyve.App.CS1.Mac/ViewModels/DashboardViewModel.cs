using Skyve.Domain;
using Skyve.Domain.Systems;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Skyve.App.CS1.Mac.ViewModels;

public class DashboardViewModel : ViewModelBase
{
	private readonly IPlaysetManager _playsetManager;
	private readonly IPackageManager _packageManager;
	private readonly ICompatibilityManager _compatibilityManager;
	private readonly ICitiesManager _citiesManager;
	private readonly INotifier _notifier;

	private string _activePlaysetName = "Default";
	private int _totalModsCount;
	private int _totalAssetsCount;
	private int _totalIncludedCount;
	private int _compatibilityIssuesCount;
	private PlaysetItemViewModel? _selectedPlayset;

	public DashboardViewModel(
		IPlaysetManager playsetManager,
		IPackageManager packageManager,
		ICompatibilityManager compatibilityManager,
		ICitiesManager citiesManager,
		INotifier notifier)
	{
		_playsetManager = playsetManager;
		_packageManager = packageManager;
		_compatibilityManager = compatibilityManager;
		_citiesManager = citiesManager;
		_notifier = notifier;

		Playsets = new ObservableCollection<PlaysetItemViewModel>();

		_notifier.ContentLoaded += RefreshDashboard;
		_notifier.PlaysetChanged += RefreshDashboard;
		_notifier.PlaysetUpdated += RefreshDashboard;

		RefreshDashboard();
	}

	public ObservableCollection<PlaysetItemViewModel> Playsets { get; }

	public string ActivePlaysetName
	{
		get => _activePlaysetName;
		set => SetField(ref _activePlaysetName, value);
	}

	public int TotalModsCount
	{
		get => _totalModsCount;
		set => SetField(ref _totalModsCount, value);
	}

	public int TotalAssetsCount
	{
		get => _totalAssetsCount;
		set => SetField(ref _totalAssetsCount, value);
	}

	public int TotalIncludedCount
	{
		get => _totalIncludedCount;
		set => SetField(ref _totalIncludedCount, value);
	}

	public int CompatibilityIssuesCount
	{
		get => _compatibilityIssuesCount;
		set => SetField(ref _compatibilityIssuesCount, value);
	}

	public PlaysetItemViewModel? SelectedPlayset
	{
		get => _selectedPlayset;
		set
		{
			if (SetField(ref _selectedPlayset, value) && value != null)
			{
				ActivatePlayset(value.Playset);
			}
		}
	}

	public void ActivatePlayset(ICustomPlayset playset)
	{
		try
		{
			_playsetManager.SetCurrentPlayset(playset);
			RefreshDashboard();
		}
		catch { }
	}

	public void RefreshDashboard()
	{
		var current = _playsetManager.CurrentPlayset;
		ActivePlaysetName = current?.Name ?? "Default";

		var mods = _packageManager.Mods?.ToList() ?? new();
		var assets = _packageManager.Assets?.ToList() ?? new();

		TotalModsCount = mods.Count;
		TotalAssetsCount = assets.Count;

		Playsets.Clear();
		foreach (var p in _playsetManager.Playsets ?? Enumerable.Empty<ICustomPlayset>())
		{
			var isCurrent = current != null && p.Name == current.Name;
			var vm = new PlaysetItemViewModel(p, isCurrent);
			Playsets.Add(vm);
			if (isCurrent)
			{
				_selectedPlayset = vm;
				OnPropertyChanged(nameof(SelectedPlayset));
			}
		}
	}
}
