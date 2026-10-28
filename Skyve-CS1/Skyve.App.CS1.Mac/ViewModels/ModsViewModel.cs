using Skyve.Domain.CS1;
using Skyve.Domain.Systems;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Skyve.App.CS1.Mac.ViewModels;

public class ModsViewModel : ViewModelBase
{
	private readonly IPackageManager _packageManager;
	private readonly IModUtil _modUtil;
	private readonly INotifier _notifier;
	private readonly IPlaysetManager _playsetManager;
	private readonly IAssetUtil _assetUtil;
	private readonly IDlcManager _dlcManager;

	private string _searchText = string.Empty;
	private string _selectedFilter = "All";
	private readonly List<ModItemViewModel> _allMods = new();

	private bool _isSavingPlayset;
	private string _newPlaysetName = string.Empty;
	private string _saveStatusMessage = string.Empty;
	private string _saveStatusColor = "#98C379";

	public ModsViewModel(
		IPackageManager packageManager,
		IModUtil modUtil,
		INotifier notifier,
		IPlaysetManager playsetManager,
		IAssetUtil assetUtil,
		IDlcManager dlcManager)
	{
		_packageManager = packageManager;
		_modUtil = modUtil;
		_notifier = notifier;
		_playsetManager = playsetManager;
		_assetUtil = assetUtil;
		_dlcManager = dlcManager;

		FilteredMods = new ObservableCollection<ModItemViewModel>();

		_notifier.ContentLoaded += ReloadMods;
		_notifier.PackageInclusionUpdated += OnInclusionUpdated;

		ReloadMods();
	}

	public ObservableCollection<ModItemViewModel> FilteredMods { get; }

	public string SearchText
	{
		get => _searchText;
		set
		{
			if (SetField(ref _searchText, value))
			{
				ApplyFilter();
			}
		}
	}

	public string SelectedFilter
	{
		get => _selectedFilter;
		set
		{
			if (SetField(ref _selectedFilter, value))
			{
				ApplyFilter();
			}
		}
	}

	public bool IsSavingPlayset
	{
		get => _isSavingPlayset;
		set => SetField(ref _isSavingPlayset, value);
	}

	public string NewPlaysetName
	{
		get => _newPlaysetName;
		set => SetField(ref _newPlaysetName, value);
	}

	public string SaveStatusMessage
	{
		get => _saveStatusMessage;
		set
		{
			if (SetField(ref _saveStatusMessage, value))
			{
				OnPropertyChanged(nameof(HasSaveStatus));
			}
		}
	}

	public string SaveStatusColor
	{
		get => _saveStatusColor;
		set => SetField(ref _saveStatusColor, value);
	}

	public bool HasSaveStatus => !string.IsNullOrEmpty(_saveStatusMessage);

	public int IncludedModsCount => _allMods.Count(m => m.IsIncluded);

	public void OpenSavePlaysetDialog()
	{
		NewPlaysetName = _playsetManager.GetNewPlaysetName();
		SaveStatusMessage = string.Empty;
		IsSavingPlayset = true;
	}

	public void CancelSavePlayset()
	{
		IsSavingPlayset = false;
	}

	public void ConfirmSavePlayset()
	{
		if (string.IsNullOrWhiteSpace(NewPlaysetName))
		{
			SaveStatusMessage = "Please enter a valid playset name.";
			SaveStatusColor = "#E06C75";
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
				Mods = _packageManager.Mods?.Where(_modUtil.IsIncluded).Select(x => new Playset.Mod(x) { Enabled = _modUtil.IsEnabled(x) }).ToList() ?? new(),
				Assets = _packageManager.Assets?.Where(_assetUtil.IsIncluded).Select(x => new Playset.Asset(x)).ToList() ?? new(),
				ExcludedDLCs = _dlcManager.GetExcludedDlcs()?.ToList() ?? new()
			};

			if (!_playsetManager.Save(newPlayset, forced: true))
			{
				SaveStatusMessage = $"Could not save playset '{cleanName}' to disk.";
				SaveStatusColor = "#E06C75";
				return;
			}

			_playsetManager.AddPlayset(newPlayset);
			_playsetManager.SetCurrentPlayset(newPlayset);

			IsSavingPlayset = false;
			SaveStatusMessage = $"Saved {newPlayset.ModCount} mods and {newPlayset.AssetCount} assets to '{cleanName}'!";
			SaveStatusColor = "#98C379";
		}
		catch (Exception ex)
		{
			SaveStatusMessage = "Error saving playset: " + ex.Message;
			SaveStatusColor = "#E06C75";
		}
	}

	public void ReloadMods()
	{
		_allMods.Clear();
		var mods = _packageManager.Mods?.ToList() ?? new();
		foreach (var mod in mods)
		{
			_allMods.Add(new ModItemViewModel(mod, _modUtil));
		}
		ApplyFilter();
	}

	public void OnInclusionUpdated()
	{
		foreach (var mod in _allMods)
		{
			mod.RefreshStatus();
		}
		OnPropertyChanged(nameof(IncludedModsCount));
	}

	public void ApplyFilter()
	{
		FilteredMods.Clear();
		var query = _allMods.AsEnumerable();

		if (!string.IsNullOrWhiteSpace(_searchText))
		{
			query = query.Where(m =>
				m.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
				m.Id.ToString().Contains(_searchText));
		}

		if (_selectedFilter == "Included")
		{
			query = query.Where(m => m.IsIncluded);
		}
		else if (_selectedFilter == "Excluded")
		{
			query = query.Where(m => !m.IsIncluded);
		}
		else if (_selectedFilter == "Enabled")
		{
			query = query.Where(m => m.IsEnabled);
		}
		else if (_selectedFilter == "Disabled")
		{
			query = query.Where(m => !m.IsEnabled);
		}

		foreach (var item in query.OrderBy(m => m.Name))
		{
			FilteredMods.Add(item);
		}
	}

	public void IncludeAll()
	{
		foreach (var mod in FilteredMods)
		{
			mod.IsIncluded = true;
		}
	}

	public void ExcludeAll()
	{
		foreach (var mod in FilteredMods)
		{
			mod.IsIncluded = false;
		}
	}

	public void EnableAll()
	{
		foreach (var mod in FilteredMods)
		{
			mod.IsEnabled = true;
		}
	}

	public void DisableAll()
	{
		foreach (var mod in FilteredMods)
		{
			mod.IsEnabled = false;
		}
	}
}
