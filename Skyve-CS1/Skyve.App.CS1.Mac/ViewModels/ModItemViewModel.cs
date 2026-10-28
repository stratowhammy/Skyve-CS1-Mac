using Extensions;
using Skyve.Domain;
using Skyve.Domain.Systems;
using System.Diagnostics;

namespace Skyve.App.CS1.Mac.ViewModels;

public class ModItemViewModel : ViewModelBase
{
	private readonly IMod _mod;
	private readonly IModUtil _modUtil;
	private bool _isIncluded;
	private bool _isEnabled;

	public ModItemViewModel(IMod mod, IModUtil modUtil)
	{
		_mod = mod;
		_modUtil = modUtil;
		_isIncluded = modUtil.IsIncluded(mod);
		_isEnabled = modUtil.IsEnabled(mod);
	}

	public IMod Mod => _mod;
	public ulong Id => _mod.Id;
	public string Name => _mod.Name ?? "Unknown Mod";
	public string VersionString => _mod.Version?.ToString() ?? "1.0";
	public string FilePath => _mod.FilePath ?? string.Empty;
	public string Folder => _mod.Folder ?? string.Empty;
	public bool IsWorkshop => _mod.Id > 0;

	public bool IsIncluded
	{
		get => _isIncluded;
		set
		{
			if (SetField(ref _isIncluded, value))
			{
				_modUtil.SetIncluded(_mod, value);
			}
		}
	}

	public bool IsEnabled
	{
		get => _isEnabled;
		set
		{
			if (SetField(ref _isEnabled, value))
			{
				_modUtil.SetEnabled(_mod, value);
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
		_isIncluded = _modUtil.IsIncluded(_mod);
		_isEnabled = _modUtil.IsEnabled(_mod);
		OnPropertyChanged(nameof(IsIncluded));
		OnPropertyChanged(nameof(IsEnabled));
	}
}
