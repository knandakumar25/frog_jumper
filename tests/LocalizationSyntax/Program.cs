using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>Offline syntax-aware display inventory and interpolation externalizer; never executes inspected game code.</summary>
internal static class Program
{
    /// <summary>Stable semantic owner plus a content suffix. Catalog keys survive source-line and formatting changes.</summary>
    private static string Key(string file, SyntaxNode node, string english)
    {
        string owner = node.Ancestors().OfType<MemberDeclarationSyntax>().Select(n => n switch
        {
            MethodDeclarationSyntax method => method.Identifier.Text,
            PropertyDeclarationSyntax property => property.Identifier.Text,
            FieldDeclarationSyntax field => field.Declaration.Variables.First().Identifier.Text,
            _ => ""
        }).FirstOrDefault(s => s.Length > 0) ?? "Display";
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(english)))[..8];
        return Regex.Replace($"UI_{Path.GetFileNameWithoutExtension(file)}_{owner}_{hash}", "[^A-Za-z0-9_]", "_").ToUpperInvariant();
    }

    /// <summary>Excludes identities used for node lookup, paths, tracing and existing translation calls.</summary>
    private static bool Technical(SyntaxNode node, string value)
    {
        // A native Control may store a semantic translation key as Text. The
        // catalog scanner follows that key separately; it is not English prose.
        if (Regex.IsMatch(value, @"^[A-Z][A-Z0-9]*(?:_[A-Z0-9]+)+$")) return true;
        // SQL belongs to the persistence contract, never to a player's text catalog.
        if (Regex.IsMatch(value, @"^\s*(?:SELECT\s.+\sFROM\b|INSERT\s+INTO\b|UPDATE\s+\w+\s+SET\b|DELETE\s+FROM\b|CREATE\s+TABLE\b)", RegexOptions.IgnoreCase | RegexOptions.Singleline)) return true;
        if (value.Contains('_') && Regex.IsMatch(value, @"^[A-Z0-9_]+(?:\{[^{}]+\}[A-Z0-9_]*)+$")) return true;
        if (node.Ancestors().OfType<ObjectCreationExpressionSyntax>().Any(creation => creation.Type.ToString() is "Color" or "NodePath" or "StringName")) return true;
        if (Regex.IsMatch(value, @"^(?:res://|user://|https?://|[A-Za-z]:\\)") || value.Contains(".tscn")) return true;
        if (node.Ancestors().Any(n => n is AttributeSyntax)) return true;
        foreach (var invocation in node.Ancestors().OfType<InvocationExpressionSyntax>())
        {
            string target = invocation.Expression.ToString();
            if (target == "GuidanceLabel") return true; // Arguments identify the node; its Text is assigned separately.
            if (Regex.IsMatch(target, @"(?:GD\.(?:Print|Push)|Console\.|GetNode|FindChild|GetParent|ResourceLoader|\.Enqueue|PlayStoryCue|WildfireText\.|\bLocalized$|\bTr$|\bTranslate$|\.Call$|\.Get$|\.Set$)")) return true;
        }
        var assignment = node.Ancestors().OfType<AssignmentExpressionSyntax>().FirstOrDefault();
        if (assignment != null && Regex.IsMatch(assignment.Left.ToString(), @"(?:\bName|\.Name)$")) return true;
        return false;
    }

    /// <summary>Recognizes interpolation at a display boundary, leaving paths, IDs and arbitrary external data untouched.</summary>
    private static bool DisplayInterpolation(InterpolatedStringExpressionSyntax node)
    {
        var assignment = node.Ancestors().OfType<AssignmentExpressionSyntax>().FirstOrDefault();
        if (assignment != null && Regex.IsMatch(assignment.Left.ToString(), @"(?:Text|TooltipText|PlaceholderText)$")) return true;
        var invocation = node.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        if (invocation != null && Regex.IsMatch(invocation.Expression.ToString(), @"(?:NewLabel|ShowDialogue|ShowPrompt|DrawString|SetDialogueLine)$")) return true;
        var method = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        return method != null && Regex.IsMatch(method.Identifier.Text, @"(?:Objective|Caption|Guidance|Prompt|FragmentLog|IntroLine)");
    }

    /// <summary>Single-word headings/labels are player text too, even without spaces.</summary>
    private static bool DisplayLiteral(LiteralExpressionSyntax node)
    {
        var assignment = node.Ancestors().OfType<AssignmentExpressionSyntax>().FirstOrDefault();
        if (assignment != null && Regex.IsMatch(assignment.Left.ToString(), @"(?:Text|TooltipText|PlaceholderText)$")) return true;
        var invocation = node.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        return invocation != null && Regex.IsMatch(invocation.Expression.ToString(), @"(?:NewLabel|DrawString|ShowPrompt|ShowDialogue)$");
    }

    /// <summary>Matches the compiler's composite-format template including escaped braces, alignment and numeric formats.</summary>
    private static string Template(InterpolatedStringExpressionSyntax node)
    {
        var text = new StringBuilder();
        int index = 0;
        foreach (var part in node.Contents)
        {
            if (part is InterpolatedStringTextSyntax literal) text.Append(literal.TextToken.ValueText);
            else if (part is InterpolationSyntax field)
            {
                text.Append('{').Append(index++);
                if (field.AlignmentClause != null) text.Append(field.AlignmentClause.ToString());
                if (field.FormatClause != null) text.Append(field.FormatClause.ToString());
                text.Append('}');
            }
        }
        return text.ToString();
    }

    /// <summary>Reads a JSON list of reachable project source paths; --apply externalizes only display interpolations.</summary>
    private static int Main(string[] args)
    {
        string root = args[0], list = args[1], output = args[2];
        bool apply = args.Contains("--apply");
        var paths = JsonSerializer.Deserialize<string[]>(File.ReadAllText(list))!;
        var entries = new List<object>();
        // Rebuild from current syntax: retaining the previous output made removed
        // callers and corrected false positives permanently appear reachable.
        foreach (string file in paths.Where(p => p.EndsWith(".cs")))
        {
            string path = Path.Combine(root, file), source = File.ReadAllText(path);
            var tree = CSharpSyntaxTree.ParseText(source);
            var syntax = tree.GetRoot();
            var edits = new List<(int Start, int Length, string Value)>();
            foreach (var call in syntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                string target = call.Expression.ToString();
                int keyIndex = target == "WildfireText.Bind" || target == "WildfireText.SetText" ? 2 : 0;
                if (target is "WildfireText.SetText" or "WildfireText.Bind" or "WildfireText.Interpolate" or "WildfireText.TemplateValue" &&
                    call.ArgumentList.Arguments.Count > keyIndex + 1 &&
                    call.ArgumentList.Arguments[keyIndex].Expression is LiteralExpressionSyntax boundKey &&
                    call.ArgumentList.Arguments[keyIndex + 1].Expression is InterpolatedStringExpressionSyntax boundSource)
                {
                    entries.Add(new { key = boundKey.Token.ValueText, english = Template(boundSource), source = file,
                        line = tree.GetLineSpan(call.Span).StartLinePosition.Line + 1, kind = "interpolation" });
                }
                if (call.Expression.ToString() == "WildfireText.Interpolate" && call.ArgumentList.Arguments.Count == 2 &&
                    call.ArgumentList.Arguments[1].Expression is InterpolatedStringExpressionSyntax formatted && Technical(call, Template(formatted)))
                {
                    edits.Add((call.SpanStart, call.Span.Length, formatted.ToString()));
                    continue;
                }
                if (call.Expression.ToString() != "WildfireText.Get" || call.ArgumentList.Arguments.Count != 2) continue;
                if (call.ArgumentList.Arguments[0].Expression is not LiteralExpressionSyntax key ||
                    !key.Token.ValueText.StartsWith("UI_")) continue;
                if (call.ArgumentList.Arguments[1].Expression is LiteralExpressionSyntax fallback && Technical(call, fallback.Token.ValueText))
                    edits.Add((call.SpanStart, call.Span.Length, fallback.ToString()));
            }
            foreach (var literal in syntax.DescendantNodes().OfType<LiteralExpressionSyntax>().Where(n => n.IsKind(SyntaxKind.StringLiteralExpression)))
            {
                string value = literal.Token.ValueText;
                if (Technical(literal, value) || !Regex.IsMatch(value, "[A-Za-z]{2}") ||
                    (!Regex.IsMatch(value, @"\s") && !DisplayLiteral(literal))) continue;
                entries.Add(new { key = Key(file, literal, value), english = value, source = file,
                    line = tree.GetLineSpan(literal.Span).StartLinePosition.Line + 1, kind = "source alias" });
                var invocation = literal.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
                if (apply && invocation != null && invocation.Expression.ToString().EndsWith("DrawString"))
                    edits.Add((literal.SpanStart, literal.Span.Length, $"WildfireText.Get(\"{Key(file, literal, value)}\", {literal})"));
            }
            foreach (var interpolation in syntax.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>())
            {
                var wrapped = interpolation.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
                if (apply && wrapped != null && Regex.IsMatch(wrapped.Expression.ToString(), @"^WildfireText\.(?:SetText|Bind)$"))
                {
                    foreach (var field in interpolation.Contents.OfType<InterpolationSyntax>())
                    {
                        string expression = field.Expression.ToString();
                        var translated = Regex.Match(expression, @"^TranslationServer.Translate\((.+\.(?:DisplayName|CivilianName|PrimaryAbilityName|DashAbilityName))\)$");
                        if (translated.Success)
                            edits.Add((field.Expression.SpanStart, field.Expression.Span.Length, $"WildfireText.DisplayValue({translated.Groups[1].Value})"));
                        else if (Regex.IsMatch(expression, @"\.(?:DisplayName|CivilianName|PrimaryAbilityName|DashAbilityName)$"))
                            edits.Add((field.Expression.SpanStart, field.Expression.Span.Length, $"WildfireText.DisplayValue({expression})"));
                    }
                }
                if (apply && wrapped?.Expression.ToString() == "WildfireText.Interpolate")
                {
                    var assignment = wrapped.Parent as AssignmentExpressionSyntax;
                    if (assignment?.Left is MemberAccessExpressionSyntax member)
                    {
                        string arguments = string.Join(", ", wrapped.ArgumentList.Arguments.Select(a => a.ToString()));
                        string property = member.Name.Identifier.Text;
                        edits.Add((assignment.SpanStart, assignment.Span.Length,
                            $"WildfireText.SetText({member.Expression}, \"{property}\", {arguments})"));
                        continue;
                    }
                    if (assignment?.Parent is InitializerExpressionSyntax initializer && initializer.Parent is ObjectCreationExpressionSyntax creation)
                    {
                        string arguments = string.Join(", ", wrapped.ArgumentList.Arguments.Select(a => a.ToString()));
                        string property = assignment.Left.ToString();
                        var replacement = creation.WithInitializer(initializer.WithExpressions(initializer.Expressions.Remove(assignment)));
                        edits.Add((creation.SpanStart, creation.Span.Length,
                            $"WildfireText.Bind({replacement}, \"{property}\", {arguments})"));
                        continue;
                    }
                    var draw = wrapped.Ancestors().OfType<InvocationExpressionSyntax>().Skip(1).FirstOrDefault();
                    if (draw?.Expression.ToString().EndsWith("DrawString") == true)
                        continue;
                    // Only schema-owned operative display fields are translated;
                    // usernames and other arbitrary interpolation values stay literal.
                    foreach (var field in interpolation.Contents.OfType<InterpolationSyntax>())
                    {
                        string expression = field.Expression.ToString();
                        if (Regex.IsMatch(expression, @"\.(?:DisplayName|CivilianName|PrimaryAbilityName|DashAbilityName)$"))
                            edits.Add((field.Expression.SpanStart, field.Expression.Span.Length, $"WildfireText.DisplayValue({expression})"));
                    }
                }
                string value = Template(interpolation);
                if (Technical(interpolation, value) || !DisplayInterpolation(interpolation) || !Regex.IsMatch(value, "[A-Za-z]{2}")) continue;
                string key = Key(file, interpolation, value);
                entries.Add(new { key, english = value, source = file,
                    line = tree.GetLineSpan(interpolation.Span).StartLinePosition.Line + 1, kind = "interpolation" });
                if (apply) edits.Add((interpolation.SpanStart, interpolation.Span.Length,
                    $"WildfireText.Interpolate(\"{key}\", {interpolation})"));
            }
            if (apply && edits.Count > 0)
            {
                // Process right-to-left so spans refer to the original syntax tree.
                int lastStart = source.Length;
                foreach (var edit in edits.OrderByDescending(e => e.Start))
                {
                    if (edit.Start + edit.Length > lastStart) continue;
                    source = source.Remove(edit.Start, edit.Length).Insert(edit.Start, edit.Value);
                    lastStart = edit.Start;
                }
                File.WriteAllText(path, source, new UTF8Encoding(false));
            }
        }
        var unique = entries.GroupBy(row => JsonSerializer.SerializeToElement(row).GetProperty("key").GetString())
            .Select(group => group.Last()).OrderBy(row => JsonSerializer.SerializeToElement(row).GetProperty("key").GetString());
        File.WriteAllText(output, JsonSerializer.Serialize(unique, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        return 0;
    }
}
