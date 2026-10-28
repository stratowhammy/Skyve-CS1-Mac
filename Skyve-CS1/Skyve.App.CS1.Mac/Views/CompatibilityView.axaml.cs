using Avalonia.Controls;
using Avalonia.Interactivity;
using Skyve.App.CS1.Mac.ViewModels;

namespace Skyve.App.CS1.Mac.Views;

public partial class CompatibilityView : UserControl
{
	public CompatibilityView()
	{
		InitializeComponent();
	}

	private void OnRefreshClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is CompatibilityViewModel vm)
		{
			vm.RefreshReport();
		}
	}

	private void OnWorkshopClick(object? sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is CompatibilityItemViewModel item)
		{
			item.OpenWorkshop();
		}
	}
}
