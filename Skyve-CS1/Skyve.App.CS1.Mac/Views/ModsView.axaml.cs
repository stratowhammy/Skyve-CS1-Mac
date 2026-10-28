using Avalonia.Controls;
using Avalonia.Interactivity;
using Skyve.App.CS1.Mac.ViewModels;

namespace Skyve.App.CS1.Mac.Views;

public partial class ModsView : UserControl
{
	public ModsView()
	{
		InitializeComponent();
	}

	private void OnFilterClick(object? sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is string filter && DataContext is ModsViewModel vm)
		{
			vm.SelectedFilter = filter;
		}
	}

	private void OnIncludeAllClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is ModsViewModel vm)
		{
			vm.IncludeAll();
		}
	}

	private void OnExcludeAllClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is ModsViewModel vm)
		{
			vm.ExcludeAll();
		}
	}

	private void OnWorkshopClick(object? sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is ModItemViewModel item)
		{
			item.OpenWorkshop();
		}
	}

	private void OnRevealClick(object? sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is ModItemViewModel item)
		{
			item.RevealInFinder();
		}
	}
}
