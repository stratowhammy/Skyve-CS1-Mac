using Avalonia.Controls;
using Avalonia.Input;
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

	private void OnOpenSavePlaysetClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is ModsViewModel vm)
		{
			vm.OpenSavePlaysetDialog();
		}
	}

	private void OnCancelSavePlaysetClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is ModsViewModel vm)
		{
			vm.CancelSavePlayset();
		}
	}

	private void OnConfirmSavePlaysetClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is ModsViewModel vm)
		{
			vm.ConfirmSavePlayset();
		}
	}

	private void OnSavePlaysetKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Enter && DataContext is ModsViewModel vm)
		{
			vm.ConfirmSavePlayset();
		}
		else if (e.Key == Key.Escape && DataContext is ModsViewModel vm2)
		{
			vm2.CancelSavePlayset();
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
