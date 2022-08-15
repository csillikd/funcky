using System.Collections.Immutable;
using Funcky.SourceGenerator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Funcky.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class OrNoneFromTryPatternGenerator : IIncrementalGenerator
{
    private const string AttributeFullName = "Funcky.Internal.OrNoneFromTryPatternAttribute";
    private static readonly IEnumerable<string> GeneratedFileHeadersSource = ImmutableList.Create("// <auto-generated/>", "#nullable enable", string.Empty);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(RegisterOrNoneAttribute);
        context.RegisterSourceOutput(GetOrNonePartialMethods(context), RegisterOrNonePartials);
    }

    private static void RegisterOrNonePartials(SourceProductionContext context, ImmutableArray<MethodPartial> partialMethods)
        => _ = partialMethods
            .GroupBy(partialMethod => partialMethod.SourceTree.FilePath)
            .Aggregate(context, CreateSourceByClass);

    private static SourceProductionContext CreateSourceByClass(SourceProductionContext context, IGrouping<string, MethodPartial> methodByClass)
    {
        var syntaxTree = OrNoneFromTryPatternPartial.GetSyntaxTree(methodByClass.First().NamespaceName, methodByClass.First().ClassName, methodByClass.SelectMany(m => m.Methods));

        context.AddSource($"{Path.GetFileName(methodByClass.Key)}.g.cs", string.Join(Environment.NewLine, GeneratedFileHeadersSource) + Environment.NewLine + syntaxTree.NormalizeWhitespace().ToFullString());

        return context;
    }

    private static IncrementalValueProvider<ImmutableArray<MethodPartial>> GetOrNonePartialMethods(IncrementalGeneratorInitializationContext context)
        => context.SyntaxProvider.CreateSyntaxProvider(predicate: IsSyntaxTargetForGeneration, transform: GetSemanticTargetForGeneration)
            .WhereNotNull()
            .Combine(context.CompilationProvider)
            .Select((state, _) => ToMethodPartial(state.Left, state.Right))
            .Collect();

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken cancellationToken)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };

    private static SemanticTarget? GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        => context.Node is ClassDeclarationSyntax classDeclarationSyntax
           && context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax, cancellationToken) is { } classSymbol
           && classSymbol.GetAttributes()
               .Where(a => a.AttributeClass?.ToDisplayString() == AttributeFullName)
               .Where(AttributeBelongsToPartialPart(classDeclarationSyntax))
               .Select(ParseAttribute)
               .ToImmutableArray() is { Length: >=1 } attributes
            ? new SemanticTarget(classDeclarationSyntax, attributes)
            : null;

    private static Func<AttributeData, bool> AttributeBelongsToPartialPart(ClassDeclarationSyntax partialPart)
        => attribute => attribute.ApplicationSyntaxReference?.GetSyntax().Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault() == partialPart;

    private static ParsedAttribute ParseAttribute(AttributeData attribute)
        => attribute.ConstructorArguments.Length >= 2
           && attribute.ConstructorArguments[0].Value is INamedTypeSymbol type
           && attribute.ConstructorArguments[1].Value is string methodName
            ? new ParsedAttribute(type, methodName)
            : throw new InvalidOperationException("Invalid attribute: expected a named type and a method name");

    private static MethodPartial ToMethodPartial(SemanticTarget semanticTarget, Compilation compilation)
    {
        var methods =
            from attribute in semanticTarget.Attributes
            from method in attribute.Type.GetMembers().OfType<IMethodSymbol>()
            where method.Name == attribute.MethodName
            select GenerateOrNoneMethod(attribute.Type, method);

        return new MethodPartial(
            NamespaceName: GetNamespaceName(semanticTarget.ClassDeclarationSyntax, compilation),
            ClassName: semanticTarget.ClassDeclarationSyntax.Identifier.ToString(),
            Methods: methods.ToImmutableArray(),
            SourceTree: semanticTarget.ClassDeclarationSyntax.SyntaxTree);
    }

    private static MethodDeclarationSyntax GenerateOrNoneMethod(ITypeSymbol type, IMethodSymbol method)
        => MethodDeclaration(
            ParseTypeName($"Funcky.Monads.Option<{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>"),
            GetMethodName(type, method))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(ParameterList(SeparatedList(method.Parameters.Where(p => p.RefKind == RefKind.None).Select(GenerateParameter))))
            .WithExpressionBody(ArrowExpressionClause(GenerateOrNoneImplementation(type, method)))
            .WithAttributeLists(SingletonList(AttributeList(SingletonSeparatedList(Attribute(IdentifierName("global::System.Diagnostics.Contracts.Pure"))))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

    private static ExpressionSyntax GenerateOrNoneImplementation(ITypeSymbol type, IMethodSymbol method)
        => ConditionalExpression(
            condition: InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ParseTypeName(method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                    IdentifierName(method.Name)),
                GenerateTryMethodArgumentList(method)),
            whenTrue: IdentifierName(method.Parameters.Single(p => p.RefKind == RefKind.Out).Name),
            whenFalse: DefaultExpression(ParseTypeName($"Funcky.Monads.Option<{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>")));

    private static ArgumentListSyntax GenerateTryMethodArgumentList(IMethodSymbol method)
        => ArgumentList(SeparatedList(method.Parameters.Select(GenerateTryMethodArgument)));

    private static ArgumentSyntax GenerateTryMethodArgument(IParameterSymbol parameter, int index)
        => parameter.RefKind == RefKind.Out
            ? GenerateOutVarArgument(Identifier(GetParameterName(parameter, index)))
            : Argument(IdentifierName(GetParameterName(parameter, index)));

    private static ArgumentSyntax GenerateOutVarArgument(SyntaxToken identifier)
        => Argument(
            nameColon: null,
            Token(SyntaxKind.OutKeyword),
            DeclarationExpression(
                IdentifierName(VarKeyword()),
                SingleVariableDesignation(identifier)));

    private static SyntaxToken VarKeyword()
        => Identifier(TriviaList(), SyntaxKind.VarKeyword, "var", "var", TriviaList());

    private static ParameterSyntax GenerateParameter(IParameterSymbol parameter, int index)
        => Parameter(Identifier(GetParameterName(parameter, index)))
            .WithModifiers(index == 0 ? TokenList(Token(SyntaxKind.ThisKeyword)) : TokenList())
            .WithType(GenerateTypeSyntax(parameter.Type))
            .WithDefault(GetParameterDefaultValue(parameter));

    private static TypeSyntax GenerateTypeSyntax(ITypeSymbol type)
    {
        var parsedType = ParseTypeName(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier)));
        return type.NullableAnnotation is not NullableAnnotation.None
            ? parsedType
            : parsedType
                .WithLeadingTrivia(Trivia(NullableDirectiveTrivia(Token(SyntaxKind.DisableKeyword), true)))
                .WithTrailingTrivia(Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true)));
    }

    private static string GetNamespaceName(SyntaxNode methodDeclaration, Compilation compilation)
        => compilation.GetSemanticModel(methodDeclaration.SyntaxTree)
            .GetDeclaredSymbol(methodDeclaration)?
            .ContainingNamespace
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))
                ?? throw new InvalidOperationException("Unable to detect containing namespace");

    private static string GetMethodName(ITypeSymbol type, IMethodSymbol method)
    {
        const string tryPrefix = "Try";
        const string orNoneSuffix = "OrNone";
        return method.Name.StartsWith(tryPrefix)
            ? $"{method.Name.Substring(tryPrefix.Length)}{type.Name}{orNoneSuffix}"
            : $"{method.Name}{type.Name}{orNoneSuffix}";
    }

    private static string GetParameterName(IParameterSymbol parameter, int index)
        => index == 0
            ? "candidate"
            : parameter.Name;

    private static EqualsValueClauseSyntax? GetParameterDefaultValue(IParameterSymbol parameter)
        => parameter.HasExplicitDefaultValue
            ? throw new InvalidOperationException("Default values are not supported")
            : null;

    private static void RegisterOrNoneAttribute(IncrementalGeneratorPostInitializationContext context)
        => context.AddSource("OrNoneFromTryPatternAttribute.g.cs", CodeSnippets.OrNoneFromTryPatternAttribute);

    private sealed record SemanticTarget(ClassDeclarationSyntax ClassDeclarationSyntax, ImmutableArray<ParsedAttribute> Attributes);

    private sealed record ParsedAttribute(INamedTypeSymbol Type, string MethodName);
}
