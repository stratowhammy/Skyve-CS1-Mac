using System;

namespace SlickControls;

public enum PromptButtons
{
	OK,
	OKCancel,
	AbortRetryIgnore,
	YesNoCancel,
	YesNo,
	RetryCancel,
	None,
	OKIgnore
}

public enum PromptIcons
{
	None,
	Hand,
	Info,
	Input,
	Question,
	Warning,
	Error,
	Ok,
	Loading
}

public enum DialogResult
{
	None = 0,
	OK = 1,
	Cancel = 2,
	Abort = 3,
	Retry = 4,
	Ignore = 5,
	Yes = 6,
	No = 7
}

public class SlickForm { }

public static class MessagePrompt
{
	public static Func<string, string?, PromptButtons, PromptIcons, DialogResult>? Handler { get; set; }

	public static DialogResult Show(string message, PromptButtons buttons = PromptButtons.OK, PromptIcons icon = PromptIcons.None, object? form = null)
	{
		return Handler?.Invoke(message, null, buttons, icon) ?? DialogResult.OK;
	}

	public static DialogResult Show(string message, string title, PromptButtons buttons = PromptButtons.OK, PromptIcons icon = PromptIcons.None, object? form = null)
	{
		return Handler?.Invoke(message, title, buttons, icon) ?? DialogResult.OK;
	}

	public static DialogResult Show(Exception ex, string message, PromptButtons buttons = PromptButtons.OK, PromptIcons icon = PromptIcons.Error, object? form = null)
	{
		return Handler?.Invoke($"{message}\n{ex.Message}", "Error", buttons, icon) ?? DialogResult.OK;
	}
}

public class Notification
{
	public string Text { get; set; } = string.Empty;
	public string? Title { get; set; }
	public PromptIcons Icon { get; set; }
	public Action? Action { get; set; }

	public static Notification Create(string text, string? title, PromptIcons icon, Action? action)
	{
		return new Notification { Text = text, Title = title, Icon = icon, Action = action };
	}

	public void Show(object? form = null, int timeoutSeconds = 5)
	{
	}
}
