using Skyve.Domain;
using Skyve.Domain.Enums;
using Skyve.Domain.Systems;
using System.Collections.ObjectModel;
using System.Linq;

namespace Skyve.App.CS1.Mac.ViewModels;

public class CompatibilityViewModel : ViewModelBase
{
	private readonly ICompatibilityManager _compatibilityManager;
	private readonly IPackageManager _packageManager;
	private readonly INotifier _notifier;
	private int _criticalCount;
	private int _warningCount;
	private int _totalIssuesCount;

	public CompatibilityViewModel(
		ICompatibilityManager compatibilityManager,
		IPackageManager packageManager,
		INotifier notifier)
	{
		_compatibilityManager = compatibilityManager;
		_packageManager = packageManager;
		_notifier = notifier;

		ReportItems = new ObservableCollection<CompatibilityItemViewModel>();

		_notifier.CompatibilityDataLoaded += RefreshReport;
		_notifier.CompatibilityReportProcessed += RefreshReport;

		RefreshReport();
	}

	public ObservableCollection<CompatibilityItemViewModel> ReportItems { get; }

	public int CriticalCount
	{
		get => _criticalCount;
		set => SetField(ref _criticalCount, value);
	}

	public int WarningCount
	{
		get => _warningCount;
		set => SetField(ref _warningCount, value);
	}

	public int TotalIssuesCount
	{
		get => _totalIssuesCount;
		set => SetField(ref _totalIssuesCount, value);
	}

	public void RefreshReport()
	{
		ReportItems.Clear();
		var packages = _packageManager.Packages?.ToList() ?? new();

		var critical = 0;
		var warning = 0;

		foreach (var pkg in packages)
		{
			try
			{
				var info = _compatibilityManager.GetCompatibilityInfo(pkg);
				if (info?.ReportItems == null) continue;

				foreach (var item in info.ReportItems)
				{
					var severity = item.Status?.Notification ?? NotificationType.Info;
					if (severity >= NotificationType.Caution)
					{
						if (severity >= NotificationType.AttentionRequired) critical++;
						else warning++;

						ReportItems.Add(new CompatibilityItemViewModel
						{
							Package = pkg,
							PackageName = pkg.Name ?? $"Package #{pkg.Id}",
							StatusType = item.Type.ToString(),
							Message = item.Message ?? item.Status?.Note ?? item.Status?.LocaleKey ?? "Compatibility warning reported.",
							Severity = severity
						});
					}
				}
			}
			catch { }
		}

		CriticalCount = critical;
		WarningCount = warning;
		TotalIssuesCount = critical + warning;
	}
}
