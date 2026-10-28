using Skyve.Domain;
using System;

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
	public string Name => string.IsNullOrEmpty(_playset.Name) ? "Unnamed Playset" : _playset.Name;
	public int ModCount => _playset.ModCount;
	public int AssetCount => _playset.AssetCount;
	public bool IsTemporary => _playset.Temporary;
	public bool IsCustom => !_playset.Temporary;
	public bool CanActivate => !_isActive;
	public bool CanDelete => !_playset.Temporary && !_isActive;
	public string ActiveIndicatorBrush => _isActive ? "#98C379" : "#5C6370";
	public string CardBorderBrush => _isActive ? "#61AFEF" : "#282C34";

	public DateTime LastModified => _playset.DateUpdated != default ? _playset.DateUpdated : (_playset.DateCreated != default ? _playset.DateCreated : DateTime.Now);
	public string FormattedDate => LastModified.ToString("yyyy-MM-dd HH:mm");

	public bool IsActive
	{
		get => _isActive;
		set
		{
			if (SetField(ref _isActive, value))
			{
				OnPropertyChanged(nameof(CanActivate));
				OnPropertyChanged(nameof(CanDelete));
				OnPropertyChanged(nameof(ActiveIndicatorBrush));
				OnPropertyChanged(nameof(CardBorderBrush));
			}
		}
	}
}
