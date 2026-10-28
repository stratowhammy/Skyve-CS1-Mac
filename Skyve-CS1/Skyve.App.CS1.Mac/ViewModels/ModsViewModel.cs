using Skyve.Domain.Systems;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Skyve.App.CS1.Mac.ViewModels;

public class ModsViewModel : ViewModelBase
{
	private readonly IPackageManager _packageManager;
	private readonly IModUtil _modUtil;
	private readonly INotifier _notifier;
	private string _searchText = string.Empty;
	private string _selectedFilter = "All";
	private readonly List<ModItemViewModel> _allMods = new();

	public ModsViewModel(IPackageManager packageManager, IModUtil modUtil, INotifier notifier)
	{
		_packageManager = packageManager;
		_modUtil = modUtil;
		_notifier = notifier;

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
