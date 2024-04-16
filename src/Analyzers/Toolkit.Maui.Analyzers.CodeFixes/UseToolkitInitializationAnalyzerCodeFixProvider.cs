using System.Collections.Immutable;
using System.Composition;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.ComTypes;
using Esri.ArcGISRuntime.Maui.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Esri.ArcGISRuntime.Toolkit.Maui.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseToolkitInitializationAnalyzerCodeFixProvider)), Shared]
public class UseToolkitInitializationAnalyzerCodeFixProvider : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(UseToolkitInitializationAnalyzer.DiagnosticId);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

		var diagnostic = context.Diagnostics.First();
		var diagnosticSpan = diagnostic.Location.SourceSpan;

		// Find the type declaration identified by the diagnostic.
		var declaration = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().Last() ?? throw new InvalidOperationException();

		// Register a code action that will invoke the fix.
		context.RegisterCodeFix(
			CodeAction.Create(
				title: CodeFixResources.Initialize_ArcGIS_Runtime_Toolkit_for__NET_MAUI_Before_UseMauiApp,
				createChangedDocument: c => AddUseArcGISToolkit(context.Document, declaration, c),
				equivalenceKey: nameof(CodeFixResources.Initialize_ArcGIS_Runtime_Toolkit_for__NET_MAUI_Before_UseMauiApp)),
			diagnostic);
	}

	static async Task<Document> AddUseArcGISToolkit(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
		{
			return document;
		}

		var updatedInvocationExpression =
			InvocationExpression(
				MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression, invocationExpression, IdentifierName("UseArcGISToolkit")));

		var mauiToolkitUsingStatement =
			UsingDirective(
				QualifiedName(
					QualifiedName(QualifiedName(IdentifierName("Esri"), IdentifierName("ArcGISRuntime")), IdentifierName("Toolkit")),
					IdentifierName("Maui")));

		var newRoot = root.ReplaceNode(invocationExpression, updatedInvocationExpression);

		if (newRoot is CompilationUnitSyntax compilationSyntax)
		{
			newRoot = compilationSyntax.AddUsings(mauiToolkitUsingStatement);
		}

		var newDocument = document.WithSyntaxRoot(newRoot);

		return newDocument;
	}
}