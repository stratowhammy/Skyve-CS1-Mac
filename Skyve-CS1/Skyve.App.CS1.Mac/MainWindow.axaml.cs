using Avalonia.Controls;
using Avalonia.Interactivity;
using Skyve.App.CS1.Mac.ViewModels;

namespace Skyve.App.CS1.Mac;

public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
	}

	private void OnNavClick(object? sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is string tab && DataContext is MainViewModel vm)
		{
			vm.NavigateTo(tab);
		}
	}

	private void OnLaunchClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is MainViewModel vm)
		{
			vm.LaunchGame();
		}
	}

	private void OnStopClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is MainViewModel vm)
		{
			vm.StopGame();
		}
	}
}
