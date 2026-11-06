using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>Single-pass named interpolation. User data is inserted literally and can never expand another placeholder.</summary>
public static class LocalizedTemplate
{
    private static readonly Regex Field = new(@"\{(?<name>[A-Za-z_][A-Za-z_0-9]*)\}", RegexOptions.Compiled);

    /// <summary>Substitutes known named fields; unknown fields stay visible for the structural validator to report.</summary>
    public static string Fill(string template, IReadOnlyDictionary<string, object> values) => Field.Replace(template,
        match => values.TryGetValue(match.Groups["name"].Value, out var value)
            ? System.Convert.ToString(value, CultureInfo.InvariantCulture) ?? "" : match.Value);
}
