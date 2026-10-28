using Avalonia.Controls;
using Avalonia.Interactivity;
using Skyve.App.CS1.Mac.ViewModels;

namespace Skyve.App.CS1.Mac.Views;

public partial class AssetsView : UserControl
{
	public AssetsView()
	{
		InitializeComponent();
	}

	private void OnIncludeAllClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is AssetsViewModel vm)
		{
			vm.IncludeAll();
		}
	}

	private void OnExcludeAllClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is AssetsViewModel vm)
		{
			vm.ExcludeAll();
		}
	}

	private void OnWorkshopClick(object? sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is AssetItemViewModel item)
		{
			item.OpenWorkshop();
		}
	}

	private void OnRevealClick(object? sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is AssetItemViewModel item)
		{
			item.RevealInFinder();
		}
	}
}
