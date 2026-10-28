using Extensions;
using Skyve.Domain;
using Skyve.Domain.CS1;
using Skyve.Domain.Systems;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Skyve.App.CS1.Mac.ViewModels;

public class DashboardViewModel : ViewModelBase
{
	private readonly IPlaysetManager _playsetManager;
	private readonly IPackageManager _packageManager;
	private readonly ICompatibilityManager _compatibilityManager;
	private readonly ICitiesManager _citiesManager;
	private readonly INotifier _notifier;
	private readonly IModUtil _modUtil;
	private readonly IAssetUtil _assetUtil;
	private readonly IDlcManager _dlcManager;
	private readonly ILocationManager _locationManager;

	private string _activePlaysetName = "Default";
	private int _totalModsCount;
	private int _totalAssetsCount;
	private int _totalIncludedCount;
	private int _compatibilityIssuesCount;
	private PlaysetItemViewModel? _selectedPlayset;

	private bool _isCreatingPlayset;
	private string _newPlaysetName = string.Empty;
	private bool _newPlaysetIncludeCurrent = true;
	private string _createDialogTitle = "Save Selection as New Playset";
	private string _createDialogDescription = "Save your currently selected mods and assets into a new playset.";
	private string _statusMessage = string.Empty;
	private string _statusMessageColor = "#98C379";
	private int _totalIncludedMods;
	private int _totalIncludedAssets;

	public DashboardViewModel(
		IPlaysetManager playsetManager,
		IPackageManager packageManager,
		ICompatibilityManager compatibilityManager,
		ICitiesManager citiesManager,
		INotifier notifier,
		IModUtil modUtil,
		IAssetUtil assetUtil,
		IDlcManager dlcManager,
		ILocationManager locationManager)
	{
		_playsetManager = playsetManager;
		_packageManager = packageManager;
		_compatibilityManager = compatibilityManager;
		_citiesManager = citiesManager;
		_notifier = notifier;
		_modUtil = modUtil;
		_assetUtil = assetUtil;
		_dlcManager = dlcManager;
		_locationManager = locationManager;

		Playsets = new ObservableCollection<PlaysetItemViewModel>();

		_notifier.ContentLoaded += RefreshDashboard;
		_notifier.PlaysetChanged += RefreshDashboard;
		_notifier.PlaysetUpdated += RefreshDashboard;
		_notifier.PackageInclusionUpdated += UpdateIncludedCounts;

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

	public bool IsCreatingPlayset
	{
		get => _isCreatingPlayset;
		set => SetField(ref _isCreatingPlayset, value);
	}

	public string NewPlaysetName
	{
		get => _newPlaysetName;
		set => SetField(ref _newPlaysetName, value);
	}

	public bool NewPlaysetIncludeCurrent
	{
		get => _newPlaysetIncludeCurrent;
		set
		{
			if (SetField(ref _newPlaysetIncludeCurrent, value))
			{
				OnPropertyChanged(nameof(IncludedSummaryText));
			}
		}
	}

	public string CreateDialogTitle
	{
		get => _createDialogTitle;
		set => SetField(ref _createDialogTitle, value);
	}

	public string CreateDialogDescription
	{
		get => _createDialogDescription;
		set => SetField(ref _createDialogDescription, value);
	}

	public string StatusMessage
	{
		get => _statusMessage;
		set
		{
			if (SetField(ref _statusMessage, value))
			{
				OnPropertyChanged(nameof(HasStatusMessage));
			}
		}
	}

	public string StatusMessageColor
	{
		get => _statusMessageColor;
		set => SetField(ref _statusMessageColor, value);
	}

	public bool HasStatusMessage => !string.IsNullOrEmpty(_statusMessage);

	public int TotalIncludedMods
	{
		get => _totalIncludedMods;
		set => SetField(ref _totalIncludedMods, value);
	}

	public int TotalIncludedAssets
	{
		get => _totalIncludedAssets;
		set => SetField(ref _totalIncludedAssets, value);
	}

	public string IncludedSummaryText => NewPlaysetIncludeCurrent
		? $"Will include {TotalIncludedMods} mods and {TotalIncludedAssets} assets currently selected."
		: "Will create an empty playset with 0 mods and 0 assets.";

	public void OpenSaveCurrentPlaysetDialog()
	{
		UpdateIncludedCounts();
		NewPlaysetIncludeCurrent = true;
		CreateDialogTitle = "Save Current Selection as New Playset";
		CreateDialogDescription = "Enter a name for your new playset containing your currently selected mods and assets:";
		NewPlaysetName = _playsetManager.GetNewPlaysetName();
		StatusMessage = string.Empty;
		IsCreatingPlayset = true;
	}

	public void OpenNewEmptyPlaysetDialog()
	{
		UpdateIncludedCounts();
		NewPlaysetIncludeCurrent = false;
		CreateDialogTitle = "Create New Empty Playset";
		CreateDialogDescription = "Enter a name for a new blank playset with no mods or assets selected:";
		NewPlaysetName = _playsetManager.GetNewPlaysetName();
		StatusMessage = string.Empty;
		IsCreatingPlayset = true;
	}

	public void CancelCreatePlayset()
	{
		IsCreatingPlayset = false;
	}

	public void DismissStatus()
	{
		StatusMessage = string.Empty;
	}

	public void UpdateIncludedCounts()
	{
		TotalIncludedMods = _packageManager.Mods?.Count(_modUtil.IsIncluded) ?? 0;
		TotalIncludedAssets = _packageManager.Assets?.Count(_assetUtil.IsIncluded) ?? 0;
		TotalIncludedCount = TotalIncludedMods + TotalIncludedAssets;
		OnPropertyChanged(nameof(TotalIncludedMods));
		OnPropertyChanged(nameof(TotalIncludedAssets));
		OnPropertyChanged(nameof(TotalIncludedCount));
		OnPropertyChanged(nameof(IncludedSummaryText));
	}

	public void ConfirmSavePlayset()
	{
		if (string.IsNullOrWhiteSpace(NewPlaysetName))
		{
			StatusMessage = "Please enter a valid playset name.";
			StatusMessageColor = "#E06C75";
			return;
		}

		var cleanName = NewPlaysetName.Trim();
		foreach (var c in Path.GetInvalidFileNameChars())
		{
			cleanName = cleanName.Replace(c, '_');
		}

		try
		{
			var newPlayset = new Playset(cleanName)
			{
				DateCreated = DateTime.Now,
				LastEditDate = DateTime.Now,
				LastUsed = DateTime.Now,
				AutoSave = true,
				Temporary = false,
			};

			if (NewPlaysetIncludeCurrent)
			{
				newPlayset.Mods = _packageManager.Mods?.Where(_modUtil.IsIncluded).Select(x => new Playset.Mod(x) { Enabled = _modUtil.IsEnabled(x) }).ToList() ?? new();
				newPlayset.Assets = _packageManager.Assets?.Where(_assetUtil.IsIncluded).Select(x => new Playset.Asset(x)).ToList() ?? new();
				newPlayset.ExcludedDLCs = _dlcManager.GetExcludedDlcs()?.ToList() ?? new();
			}
			else
			{
				newPlayset.Mods = new();
				newPlayset.Assets = new();
				newPlayset.ExcludedDLCs = new();
			}

			if (!_playsetManager.Save(newPlayset, forced: true))
			{
				StatusMessage = $"Could not save playset file for '{cleanName}'.";
				StatusMessageColor = "#E06C75";
				return;
			}

			_playsetManager.AddPlayset(newPlayset);
			_playsetManager.SetCurrentPlayset(newPlayset);

			IsCreatingPlayset = false;
			StatusMessage = $"Playset '{cleanName}' successfully created with {newPlayset.ModCount} mods and {newPlayset.AssetCount} assets!";
			StatusMessageColor = "#98C379";

			RefreshDashboard();
		}
		catch (Exception ex)
		{
			StatusMessage = "Failed to create playset: " + ex.Message;
			StatusMessageColor = "#E06C75";
		}
	}

	public void ActivatePlayset(ICustomPlayset playset)
	{
		try
		{
			_playsetManager.SetCurrentPlayset(playset);
			StatusMessage = $"Activated playset '{playset.Name}'.";
			StatusMessageColor = "#98C379";
			RefreshDashboard();
		}
		catch (Exception ex)
		{
			StatusMessage = "Failed to activate playset: " + ex.Message;
			StatusMessageColor = "#E06C75";
		}
	}

	public void DuplicatePlayset(ICustomPlayset sourcePlayset)
	{
		if (sourcePlayset is not Playset source)
		{
			return;
		}

		var cloneBaseName = $"{source.Name} (Copy)";
		var cloneName = cloneBaseName;
		int counter = 1;
		while (CrossIO.FileExists(CrossIO.Combine(_locationManager.SkyvePlaysetsAppDataPath, $"{cloneName}.json")))
		{
			cloneName = $"{cloneBaseName} {++counter}";
		}

		try
		{
			var clone = new Playset(cloneName)
			{
				DateCreated = DateTime.Now,
				LastEditDate = DateTime.Now,
				LastUsed = DateTime.Now,
				AutoSave = true,
				Temporary = false,
				LaunchSettings = source.LaunchSettings,
				LsmSettings = source.LsmSettings,
				Usage = source.Usage,
				Mods = source.Mods?.Select(m => new Playset.Mod { SteamId = m.SteamId, Enabled = m.Enabled, RelativePath = m.RelativePath }).ToList() ?? new(),
				Assets = source.Assets?.Select(a => new Playset.Asset { SteamId = a.SteamId, RelativePath = a.RelativePath }).ToList() ?? new(),
				ExcludedDLCs = source.ExcludedDLCs?.ToList() ?? new()
			};

			_playsetManager.Save(clone, forced: true);
			_playsetManager.AddPlayset(clone);

			StatusMessage = $"Playset duplicated as '{cloneName}'.";
			StatusMessageColor = "#98C379";
			RefreshDashboard();
		}
		catch (Exception ex)
		{
			StatusMessage = "Error duplicating playset: " + ex.Message;
			StatusMessageColor = "#E06C75";
		}
	}

	public void DeletePlayset(ICustomPlayset playset)
	{
		if (playset == null || playset.Temporary)
		{
			StatusMessage = "Cannot delete the temporary playset.";
			StatusMessageColor = "#E06C75";
			return;
		}

		try
		{
			_playsetManager.DeletePlayset(playset);
			StatusMessage = $"Deleted playset '{playset.Name}'.";
			StatusMessageColor = "#98C379";
			RefreshDashboard();
		}
		catch (Exception ex)
		{
			StatusMessage = "Error deleting playset: " + ex.Message;
			StatusMessageColor = "#E06C75";
		}
	}

	public void RefreshDashboard()
	{
		var current = _playsetManager.CurrentPlayset;
		ActivePlaysetName = current?.Name ?? "Default";

		var mods = _packageManager.Mods?.ToList() ?? new();
		var assets = _packageManager.Assets?.ToList() ?? new();

		TotalModsCount = mods.Count;
		TotalAssetsCount = assets.Count;

		UpdateIncludedCounts();

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
