using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Extensions;
using Microsoft.Extensions.DependencyInjection;
using Skyve.App.CS1.Mac.Services;
using Skyve.App.CS1.Mac.ViewModels;
using Skyve.Domain;
using Skyve.Domain.Systems;
using Skyve.Systems;
using Skyve.Systems.CS1;
using Skyve.Systems.CS1.Services;
using Skyve.Systems.CS1.Utilities;
using SlickControls;
using System;

namespace Skyve.App.CS1.Mac;

public partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public static void InitializeServices()
	{
		SaveHandler.AppName = "Skyve-CS1";
		var services = new ServiceCollection();
		services.AddSkyveSystems();
		services.AddCs1SkyveSystems();
		services.AddSingleton(new SaveHandler());
		services.AddSingleton<IInterfaceService, MacInterfaceService>();

		services.AddSingleton<DashboardViewModel>();
		services.AddSingleton<ModsViewModel>();
		services.AddSingleton<AssetsViewModel>();
		services.AddSingleton<CompatibilityViewModel>();
		services.AddSingleton<HelpLogsViewModel>();
		services.AddSingleton<MainViewModel>();

		var serviceProvider = services.BuildServiceProvider();
		ServiceCenter.Provider = serviceProvider;

		MessagePrompt.Handler = (msg, title, btns, icon) =>
		{
			Console.WriteLine($"[Skyve Prompt] {title}: {msg}");
			return DialogResult.OK;
		};
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			InitializeServices();

			try
			{
				Locale.Load();
				LocaleCR.Load();
				LocaleCS1.Load();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Locale load error: {ex.Message}");
			}

			try
			{
				ServiceCenter.Get<ICentralManager>().Start();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"CentralManager start error: {ex}");
			}

			var mainVm = ServiceCenter.Get<MainViewModel>();
			desktop.MainWindow = new MainWindow
			{
				DataContext = mainVm
			};
		}

		base.OnFrameworkInitializationCompleted();
	}
}
