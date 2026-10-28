using Avalonia.Controls;
using Avalonia.Interactivity;
using Skyve.App.CS1.Mac.ViewModels;

namespace Skyve.App.CS1.Mac.Views;

public partial class HelpLogsView : UserControl
{
	public HelpLogsView()
	{
		InitializeComponent();
	}

	private void OnFixLoaderClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is HelpLogsViewModel vm)
		{
			vm.FixMacPatchLoader();
		}
	}

	private void OnExportZipClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is HelpLogsViewModel vm)
		{
			vm.ExportLogZip();
		}
	}

	private void OnRefreshLogClick(object? sender, RoutedEventArgs e)
	{
		if (DataContext is HelpLogsViewModel vm)
		{
			vm.LoadPlayerLog();
		}
	}

	private void OnOpenFolderClick(object? sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is string path && DataContext is HelpLogsViewModel vm)
		{
			vm.OpenFolder(path);
		}
	}
}
