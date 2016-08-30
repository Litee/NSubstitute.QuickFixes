using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace NSubstitute.QuickFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NSubstituteHelperCodeFixProvider)), Shared]
    public class NSubstituteHelperCodeFixProvider : CodeFixProvider
    {
        private const string title = "Generate mocks as fields";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(NSubstituteHelperAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => GenerateMocks(root, context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Document> GenerateMocks(SyntaxNode root, Document document, ObjectCreationExpressionSyntax objectCreation, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var symbolInfo = semanticModel.GetSymbolInfo(objectCreation);

            var invokedSymbol = (IMethodSymbol)symbolInfo.CandidateSymbols.FirstOrDefault(x => x is IMethodSymbol);

            var methodDeclaration = objectCreation.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>();
            var classDeclaration = objectCreation.FirstAncestorOrSelf<ClassDeclarationSyntax>();

            var editor = await DocumentEditor.CreateAsync(document).ConfigureAwait(false);

            var arguments = new List<ArgumentSyntax>();
            foreach (var constructorParam in invokedSymbol.Parameters)
            {
                INamedTypeSymbol paramTypeSymbol = (INamedTypeSymbol)constructorParam.Type;
                if (paramTypeSymbol.IsAbstract)
                {
                    var fieldName = "_" + constructorParam.Name + "Mock";
                    if (!FieldExists(fieldName, classDeclaration))
                    {
                        var fieldDeclaration = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName(paramTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))
                            .WithVariables(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(fieldName))))
                            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
                        editor.InsertBefore(methodDeclaration, fieldDeclaration);
                        var mockCreationStatement = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(fieldName),
                            SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("Substitute"), SyntaxFactory.GenericName(SyntaxFactory.Identifier("For")).WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(SyntaxFactory.IdentifierName(paramTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))))))));
                        editor.InsertBefore(objectCreation.FirstAncestorOrSelf<StatementSyntax>(), mockCreationStatement);
                    }
                    arguments.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(fieldName)));
                }
                else
                {
                    /*var localVarName = constructorParam.Name + "Mock";
                    var mockCreationStatement = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(localVarName),
                        SyntaxFactory.DefaultExpression(SyntaxFactory.IdentifierName(paramTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))));
                    editor.InsertBefore(objectCreation.FirstAncestorOrSelf<StatementSyntax>(), mockCreationStatement);*/
                    arguments.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("TODO")));
                }
            }

            var newObjectCreation = objectCreation.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
            editor.ReplaceNode(objectCreation, newObjectCreation);

            return editor.GetChangedDocument();
        }

        private bool FieldExists(string fieldName, ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Members.OfType<FieldDeclarationSyntax>().Any(x => x.Declaration.Variables.Any(x2 => x2.Identifier.ValueText == fieldName));
        }
    }
}