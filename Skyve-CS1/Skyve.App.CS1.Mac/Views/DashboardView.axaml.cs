using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Skyve.App.CS1.Mac.ViewModels;

namespace Skyve.App.CS1.Mac.Views;

public partial class DashboardView : UserControl
{
	public DashboardView()
	{
		InitializeComponent();
	}

	private void OnRefreshClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is DashboardViewModel vm)
		{
			vm.RefreshDashboard();
		}
	}

	private void OnSaveCurrentPlaysetClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is DashboardViewModel vm)
		{
			vm.OpenSaveCurrentPlaysetDialog();
		}
	}

	private void OnNewEmptyPlaysetClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is DashboardViewModel vm)
		{
			vm.OpenNewEmptyPlaysetDialog();
		}
	}

	private void OnCancelCreateClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is DashboardViewModel vm)
		{
			vm.CancelCreatePlayset();
		}
	}

	private void OnConfirmSavePlaysetClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is DashboardViewModel vm)
		{
			vm.ConfirmSavePlayset();
		}
	}

	private void OnPlaysetNameKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Enter && DataContext is DashboardViewModel vm)
		{
			vm.ConfirmSavePlayset();
		}
		else if (e.Key == Key.Escape && DataContext is DashboardViewModel vm2)
		{
			vm2.CancelCreatePlayset();
		}
	}

	private void OnDismissStatusClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is DashboardViewModel vm)
		{
			vm.DismissStatus();
		}
	}

	private void OnActivatePlaysetClick(object? sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is PlaysetItemViewModel playsetVm && DataContext is DashboardViewModel vm)
		{
			vm.ActivatePlayset(playsetVm.Playset);
		}
	}

	private void OnDuplicatePlaysetClick(object? sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is PlaysetItemViewModel playsetVm && DataContext is DashboardViewModel vm)
		{
			vm.DuplicatePlayset(playsetVm.Playset);
		}
	}

	private void OnDeletePlaysetClick(object? sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is PlaysetItemViewModel playsetVm && DataContext is DashboardViewModel vm)
		{
			vm.DeletePlayset(playsetVm.Playset);
		}
	}
}
