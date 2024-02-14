using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;

namespace Devlooped.Dynamically;

[Generator(LanguageNames.CSharp)]
public class DynamicallyGenerator : IIncrementalGenerator
{
    static readonly SymbolDisplayFormat fullNameFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable);

    static readonly Template template;

    static DynamicallyGenerator()
    {
        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Dynamically.DynamicallyCreate.sbntxt");
        using var reader = new StreamReader(resource!);
        template = Template.Parse(reader.ReadToEnd());
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        static bool IsDynamically(ExpressionSyntax expression)
            => expression is IdentifierNameSyntax identifier && identifier.Identifier.Text == "Dynamically";

        static bool IsAliasedDynamically(ExpressionSyntax expression)
            => expression is AliasQualifiedNameSyntax aliased && aliased.Name.Identifier.Text == "Dynamically";

        // create a syntax provider that extracts the return type kind of method symbols
        var createdTypes = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) =>
                node is InvocationExpressionSyntax invocation &&
                invocation.Expression is MemberAccessExpressionSyntax member &&
                member.Name.Identifier.Text == "Create" &&
                member.Name is GenericNameSyntax &&
                (IsDynamically(member.Expression) || IsAliasedDynamically(member.Expression)),
            static (context, cancellation) =>
            {
                var invocation = (InvocationExpressionSyntax)context.Node;
                var member = (MemberAccessExpressionSyntax)invocation.Expression;
                var generic = (GenericNameSyntax)member.Name;
                var symbol = context.SemanticModel.GetSymbolInfo(generic.TypeArgumentList.Arguments[0]).Symbol;
                return symbol;
            });

        context.RegisterImplementationSourceOutput(
            createdTypes.Collect().Combine(context.CompilationProvider),
            (ctx, data) =>
            {
                var types = data.Left.Where(t => t != null).Distinct(SymbolEqualityComparer.Default)
                    .Select(t => t!.ToFullName(data.Right)).OrderBy(t => t).ToArray();

                var output = template.Render(new
                {
                    Aliases = data.Right.References.SelectMany(x => x.Properties.Aliases).ToArray(),
                    Types = types,
                }, member => member.Name);

                ctx.AddSource("Dynamically.Create.g", output);
            });

        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("Dynamically.g",
                """
                /// <summary>Allows creating records from structurally compatible dynamic data</summary>
                static partial class Dynamically
                {
                    /// <summary>Creates a record of the specified type from the specified dynamic data</summary>
                    public static partial T Create<T>(dynamic data);
                }
                """);
        });
    }
}
