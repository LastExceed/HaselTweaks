using System.Collections.Immutable;
using HaselTweaks.InteropGenerator;
using HaselTweaks.InteropSourceGenerators.Extensions;
using LanguageExt;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HaselTweaks.InteropSourceGenerators;

[Generator]
internal sealed class TweakInitializerGenerator : IIncrementalGenerator
{
    private const string AttributeName = "HaselTweaks.TweakAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var tweakNamesProvider =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeName,
                    static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                    static (context, _) =>
                    {
                        var classSyntax = (ClassDeclarationSyntax)context.TargetNode;
                        return classSyntax.GetNameWithTypeDeclarationList();
                    });

        var collectedTweakNames = tweakNamesProvider.Collect();

        context.RegisterSourceOutput(collectedTweakNames,
            (sourceContext, tweakNames) =>
            {
                sourceContext.AddSource("Plugin.TweakInitializer.g.cs", BuildInitializeTweaksSource(tweakNames.Sort()));
            });
    }
    
    private static string BuildInitializeTweaksSource(ImmutableArray<string> tweakNames)
    {
        IndentedStringBuilder builder = new();

        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using Dalamud.Logging;");
        builder.AppendLine("using HaselTweaks.Tweaks;");
        builder.AppendLine();
        builder.AppendLine("namespace HaselTweaks;");
        builder.AppendLine();

        builder.AppendLine("public partial class Plugin");
        builder.AppendLine("{");
        builder.Indent();
        builder.AppendLine("private void InitializeTweaks()");
        builder.AppendLine("{");
        builder.Indent();

        builder.AppendLine($"Tweaks = new HashSet<Tweak>({tweakNames.Length});");
        builder.AppendLine("");

        for (var i = 0; i < tweakNames.Length; i++)
        {
            builder.AppendLine("try");
            builder.AppendLine("{");
            builder.Indent();
            builder.AppendLine($"Tweaks.Add(new {tweakNames[i]}());");
            builder.DecrementIndent();
            builder.AppendLine("}");
            builder.AppendLine("catch (Exception ex)");
            builder.AppendLine("{");
            builder.Indent();
            builder.AppendLine("PluginLog.Error(ex, \"[" + tweakNames[i] + "] Error during initialization\");");
            builder.DecrementIndent();
            builder.AppendLine("}");
            builder.AppendLine("");
        }

        builder.DecrementIndent();
        builder.AppendLine("}");
        builder.DecrementIndent();
        builder.AppendLine("}");

        return builder.ToString();
    }
}
