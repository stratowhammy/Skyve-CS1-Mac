using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Extensions;

public static partial class WinExtensionClass
{
	private static bool? isAdministrator;

	/// <summary>
	/// Checks if the App currently has Administrator Privileges
	/// </summary>
	public static bool IsAdministrator => (bool)(isAdministrator ??=
		RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? CheckWindowsAdmin()
			: false);

	private static bool CheckWindowsAdmin()
	{
		try
		{
			return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
		}
		catch
		{
			return false;
		}
	}

	public static bool TryInvoke(this object? control, Action? action)
	{
		try
		{
			action?.Invoke();
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static Color MergeColor(this Color color, Color backColor, int Perc = 50)
	{
		if (backColor == Color.Empty)
		{
			return color;
		}

		var R = (color.R * Perc / 100) + (backColor.R * (100 - Perc) / 100);
		var G = (color.G * Perc / 100) + (backColor.G * (100 - Perc) / 100);
		var B = (color.B * Perc / 100) + (backColor.B * (100 - Perc) / 100);
		return Color.FromArgb(color.A, Math.Clamp(R, 0, 255), Math.Clamp(G, 0, 255), Math.Clamp(B, 0, 255));
	}

	public static Color GetTextColor(this Color color)
	{
		if (color.A <= 125)
		{
			return FormDesign.Design.ForeColor;
		}

		var dark = (color.R * 0.299) + (color.G * 0.587) + (color.B * 0.114) > 186;

		if (dark == (!FormDesign.Design.IsDarkTheme))
		{
			return FormDesign.Design.ForeColor;
		}

		var b = dark ? 0.05f : 0.95f;

		return ColorFromHSL(color.GetHue(), 0.2f, b);
	}

	public static Size GetProportionalDownscaledSize(this Size originalSize, int sizeLimit, bool widthOnly = false)
	{
		if (originalSize.Width <= sizeLimit && (widthOnly || originalSize.Height <= sizeLimit))
		{
			return originalSize;
		}

		var scaleFactor = widthOnly ? (float)sizeLimit / originalSize.Width :
			Math.Min((float)sizeLimit / originalSize.Width, (float)sizeLimit / originalSize.Height);

		var newWidth = (int)Math.Round(originalSize.Width * scaleFactor);
		var newHeight = (int)Math.Round(originalSize.Height * scaleFactor);

		return new Size(newWidth, newHeight);
	}

	public static Color Tint(this Color color, Color source, float Lum = 0, float Sat = 0)
	{
		return color.Tint(source.GetHue(), Lum, Sat);
	}

	public static Color Tint(this Color color, float? Hue = null, float Lum = 0, float Sat = 0)
	{
		color.RgbToHsl(out var cHue, out var cSat, out var cLum);

		return Color.FromArgb(color.A, ColorFromHSL(Between(Hue ?? cHue, 0, 360), Between(cSat + (Sat / 100f), 0, 1), Between(cLum + (Lum / 100f), 0, 1)));
	}

	public static Color GetAccentColor(this Color color)
	{
		return (color.R * 0.299) + (color.G * 0.587) + (color.B * 0.114) > 186 ? Color.Black : Color.White;
	}

	public static bool IsDark(this Color color)
	{
		return (color.R * 0.299) + (color.G * 0.587) + (color.B * 0.114) <= 186;
	}

	public static string YesNo(this bool b)
	{
		return b ? "Yes" : "No";
	}

	public static void RgbToHsl(this Color rgbColor, out float hue, out float saturation, out float lightness)
	{
		float r = rgbColor.R / 255f;
		float g = rgbColor.G / 255f;
		float b = rgbColor.B / 255f;

		float max = Math.Max(r, Math.Max(g, b));
		float min = Math.Min(r, Math.Min(g, b));

		lightness = (max + min) / 2f;

		if (max == min)
		{
			hue = 0f;
			saturation = 0f;
		}
		else
		{
			float delta = max - min;
			saturation = lightness > 0.5f ? delta / (2f - max - min) : delta / (max + min);

			if (max == r)
			{
				hue = (g - b) / delta + (g < b ? 6f : 0f);
			}
			else if (max == g)
			{
				hue = (b - r) / delta + 2f;
			}
			else
			{
				hue = (r - g) / delta + 4f;
			}

			hue *= 60f;
		}
	}

	public static Color ColorFromHSL(float h, float s, float l)
	{
		float r, g, b;

		if (s == 0)
		{
			r = g = b = l;
		}
		else
		{
			float q = l < 0.5f ? l * (1f + s) : l + s - (l * s);
			float p = 2f * l - q;
			r = HuetoRGB(p, q, h / 360f + 1f / 3f);
			g = HuetoRGB(p, q, h / 360f);
			b = HuetoRGB(p, q, h / 360f - 1f / 3f);
		}

		return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
	}

	private static float HuetoRGB(float p, float q, float t)
	{
		if (t < 0) t += 1;
		if (t > 1) t -= 1;
		if (t < 1f / 6f) return p + (q - p) * 6f * t;
		if (t < 1f / 2f) return q;
		if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
		return p;
	}

	public static Size CalculateNewSize(Size imageSize, Size preferredSize)
	{
		var widthRatio = (double)preferredSize.Width / imageSize.Width;
		var heightRatio = (double)preferredSize.Height / imageSize.Height;

		var minRatio = Math.Max(widthRatio, heightRatio);

		var newWidth = (int)(imageSize.Width * minRatio);
		var newHeight = (int)(imageSize.Height * minRatio);

		return new Size(newWidth, newHeight);
	}

	private static float Between(float value, float min, float max)
	{
		return Math.Max(min, Math.Min(max, value));
	}
}
