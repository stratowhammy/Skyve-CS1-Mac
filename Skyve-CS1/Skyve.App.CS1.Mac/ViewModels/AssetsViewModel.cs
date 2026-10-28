using Skyve.Domain.Systems;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Skyve.App.CS1.Mac.ViewModels;

public class AssetsViewModel : ViewModelBase
{
	private readonly IPackageManager _packageManager;
	private readonly IAssetUtil _assetUtil;
	private readonly INotifier _notifier;
	private string _searchText = string.Empty;
	private readonly List<AssetItemViewModel> _allAssets = new();

	public AssetsViewModel(IPackageManager packageManager, IAssetUtil assetUtil, INotifier notifier)
	{
		_packageManager = packageManager;
		_assetUtil = assetUtil;
		_notifier = notifier;

		FilteredAssets = new ObservableCollection<AssetItemViewModel>();

		_notifier.ContentLoaded += ReloadAssets;
		_notifier.PackageInclusionUpdated += OnInclusionUpdated;

		ReloadAssets();
	}

	public ObservableCollection<AssetItemViewModel> FilteredAssets { get; }

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

	public void ReloadAssets()
	{
		_allAssets.Clear();
		var assets = _packageManager.Assets?.ToList() ?? new();
		foreach (var asset in assets)
		{
			_allAssets.Add(new AssetItemViewModel(asset, _assetUtil));
		}
		ApplyFilter();
	}

	public void OnInclusionUpdated()
	{
		foreach (var asset in _allAssets)
		{
			asset.RefreshStatus();
		}
	}

	public void ApplyFilter()
	{
		FilteredAssets.Clear();
		var query = _allAssets.AsEnumerable();

		if (!string.IsNullOrWhiteSpace(_searchText))
		{
			query = query.Where(a =>
				a.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
				a.Id.ToString().Contains(_searchText));
		}

		foreach (var item in query.OrderBy(a => a.Name))
		{
			FilteredAssets.Add(item);
		}
	}

	public void IncludeAll()
	{
		foreach (var asset in FilteredAssets)
		{
			asset.IsIncluded = true;
		}
	}

	public void ExcludeAll()
	{
		foreach (var asset in FilteredAssets)
		{
			asset.IsIncluded = false;
		}
	}
}
