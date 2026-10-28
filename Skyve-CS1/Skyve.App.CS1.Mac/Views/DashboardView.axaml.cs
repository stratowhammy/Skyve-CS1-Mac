using Avalonia.Controls;
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

	private void OnActivatePlaysetClick(object? sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is PlaysetItemViewModel playsetVm && DataContext is DashboardViewModel vm)
		{
			vm.ActivatePlayset(playsetVm.Playset);
		}
	}
}
