using Skyve.Domain;
using Skyve.Domain.Enums;
using System.Diagnostics;

namespace Skyve.App.CS1.Mac.ViewModels;

public class CompatibilityItemViewModel : ViewModelBase
{
	public IPackageIdentity? Package { get; set; }
	public string PackageName { get; set; } = string.Empty;
	public string StatusType { get; set; } = string.Empty;
	public string Message { get; set; } = string.Empty;
	public NotificationType Severity { get; set; }

	public string SeverityColor => Severity switch
	{
		>= NotificationType.AttentionRequired => "#E06C75",
		>= NotificationType.Caution => "#E5C07B",
		>= NotificationType.Info => "#61AFEF",
		_ => "#98C379"
	};

	public string SeverityLabel => Severity switch
	{
		>= NotificationType.AttentionRequired => "CRITICAL",
		>= NotificationType.Caution => "WARNING",
		>= NotificationType.Info => "NOTE",
		_ => "OK"
	};

	public void OpenWorkshop()
	{
		try
		{
			if (Package?.Id > 0)
			{
				Process.Start("/usr/bin/open", new[] { $"https://steamcommunity.com/sharedfiles/filedetails/?id={Package.Id}" });
			}
		}
		catch { }
	}
}
