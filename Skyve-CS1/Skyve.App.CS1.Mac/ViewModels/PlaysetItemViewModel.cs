using Skyve.Domain;

namespace Skyve.App.CS1.Mac.ViewModels;

public class PlaysetItemViewModel : ViewModelBase
{
	private readonly ICustomPlayset _playset;
	private bool _isActive;

	public PlaysetItemViewModel(ICustomPlayset playset, bool isActive)
	{
		_playset = playset;
		_isActive = isActive;
	}

	public ICustomPlayset Playset => _playset;
	public string Name => _playset.Name ?? "Unnamed Playset";
	public int ModCount => _playset.ModCount;
	public int AssetCount => _playset.AssetCount;

	public bool IsActive
	{
		get => _isActive;
		set => SetField(ref _isActive, value);
	}
}
