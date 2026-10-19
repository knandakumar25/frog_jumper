using Godot;

/// <summary>Resolves the localized 2D title artwork.</summary>
public static class TitleLogo
{
	public static string GetLocalizedTitle2DPath()
	{
		string locale = TranslationServer.GetLocale().ToLower().Replace("-", "_");
		string langName = "english";

		if (locale is "arabic" || locale.StartsWith("ar_") || locale == "ar") langName = "arabic";
		else if (locale is "hebrew" || locale.StartsWith("he_") || locale == "he" || locale.StartsWith("iw_") || locale == "iw") langName = "hebrew";
		else if (locale is "japanese" || locale.StartsWith("ja_") || locale == "ja") langName = "japanese";
		else if (locale is "korean" || locale.StartsWith("ko_") || locale == "ko") langName = "korean";
		else if (locale is "mandarin" || locale == "zh_cn" || locale.StartsWith("zh_cn_") || locale.StartsWith("zh_hans")) langName = "mandarin";
		else if (locale is "traditionalchinese" || locale == "zh_tw" || locale == "zh_hk" || locale.StartsWith("zh_tw_") || locale.StartsWith("zh_hk_") || locale.StartsWith("zh_hant") || locale == "zh") langName = "traditionalchinese";
		else if (locale is "german" || locale.StartsWith("de_") || locale == "de") langName = "german";
		else if (locale is "eurofrench" || locale is "french" || locale.StartsWith("fr_") || locale == "fr") langName = "eurofrench";
		else if (locale is "italian" || locale.StartsWith("it_") || locale == "it") langName = "italian";
		else if (locale is "polish" || locale.StartsWith("pl_") || locale == "pl") langName = "polish";
		else if (locale is "brazilianportuguese" || locale is "portuguese" || locale.StartsWith("pt_") || locale == "pt") langName = "brazilianportuguese";
		else if (locale is "spanish" || locale.StartsWith("es_") || locale == "es") langName = "spanish";
		else if (locale is "thai" || locale.StartsWith("th_") || locale == "th") langName = "thai";
		else if (locale is "turkish" || locale.StartsWith("tr_") || locale == "tr") langName = "turkish";
		else if (locale is "vietnamese" || locale.StartsWith("vi_") || locale == "vi") langName = "vietnamese";
		else if (locale is "indonesian" || locale.StartsWith("id_") || locale == "id" || locale.StartsWith("in_") || locale == "in") langName = "indonesian";

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
