using Extensions;
using Skyve.Domain;
using Skyve.Domain.Systems;
using System.Diagnostics;

namespace Skyve.App.CS1.Mac.ViewModels;

public class AssetItemViewModel : ViewModelBase
{
	private readonly IAsset _asset;
	private readonly IAssetUtil _assetUtil;
	private bool _isIncluded;

	public AssetItemViewModel(IAsset asset, IAssetUtil assetUtil)
	{
		_asset = asset;
		_assetUtil = assetUtil;
		_isIncluded = assetUtil.IsIncluded(asset);
	}

	public IAsset Asset => _asset;
	public ulong Id => _asset.Id;
	public string Name => _asset.Name ?? "Unknown Asset";
	public string FilePath => _asset.FilePath ?? string.Empty;
	public string Folder => _asset.Folder ?? string.Empty;
	public string FileSize => _asset.LocalSize.SizeString();

	public bool IsIncluded
	{
		get => _isIncluded;
		set
		{
			if (SetField(ref _isIncluded, value))
			{
				_assetUtil.SetIncluded(_asset, value);
			}
		}
	}

	public void RevealInFinder()
	{
		try
		{
			var path = !string.IsNullOrEmpty(FilePath) && CrossIO.FileExists(FilePath) ? FilePath : Folder;
			if (!string.IsNullOrEmpty(path))
			{
				Process.Start("/usr/bin/open", new[] { "-R", path });
			}
		}
		catch { }
	}

	public void OpenWorkshop()
	{
		try
		{
			if (Id > 0)
			{
				Process.Start("/usr/bin/open", new[] { $"https://steamcommunity.com/sharedfiles/filedetails/?id={Id}" });
			}
		}
		catch { }
	}

	public void RefreshStatus()
	{
		_isIncluded = _assetUtil.IsIncluded(_asset);
		OnPropertyChanged(nameof(IsIncluded));
	}
}
