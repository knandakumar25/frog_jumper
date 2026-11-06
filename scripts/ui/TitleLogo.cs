using Godot;

/// <summary>Resolves the localized 2D title artwork.</summary>
public static class TitleLogo
{
	/// <summary>Returns the existing localized title artwork using the shared locale aliases.</summary>
	public static string GetLocalizedTitle2DPath()
	{
		string langName = LocaleCatalog.Resolve(TranslationServer.GetLocale()).Key;

		if (langName == "english")
		{
			return "res://assets/images/axiom_force_title_2d.png";
		}

		string candidate = $"res://assets/images/axiom_force_title_2d_{langName}.png";
		if (ResourceLoader.Exists(candidate) || System.IO.File.Exists(ProjectSettings.GlobalizePath(candidate)))
		{
			return candidate;
		}

		return "res://assets/images/axiom_force_title_2d.png";
	}

}
