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
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
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

            var arguments = new List<ArgumentSyntax>(objectCreation.ArgumentList.Arguments);
            for (int i = objectCreation.ArgumentList.Arguments.Count(); i < invokedSymbol.Parameters.Length; i++)
            {
                var constructorParam = invokedSymbol.Parameters[i];
                var fieldName = "_" + constructorParam.Name + "Mock";
                INamedTypeSymbol paramTypeSymbol = (INamedTypeSymbol)constructorParam.Type;
                if (paramTypeSymbol.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Func<TResult>")
                {
                    if (!FieldExists(fieldName, classDeclaration))
                    {
                        var genericType = IdentifierName(paramTypeSymbol.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                        var fieldDeclaration = FieldDeclaration(VariableDeclaration(genericType)
                            .WithVariables(SingletonSeparatedList(VariableDeclarator(fieldName))))
                            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)));
                        editor.InsertBefore(methodDeclaration, fieldDeclaration);
                        var mockCreationStatement = ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(fieldName),
                            InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Substitute"), GenericName(Identifier("For")).WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList<TypeSyntax>(genericType)))))));
                        editor.InsertBefore(objectCreation.FirstAncestorOrSelf<StatementSyntax>(), mockCreationStatement);
                    }
                    arguments.Add(Argument(ParenthesizedLambdaExpression(IdentifierName(fieldName))));
                }
                else if (paramTypeSymbol.IsAbstract)
                {
                    if (!FieldExists(fieldName, classDeclaration))
                    {
                        var genericType = IdentifierName(paramTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                        var fieldDeclaration = FieldDeclaration(VariableDeclaration(genericType)
                            .WithVariables(SingletonSeparatedList(VariableDeclarator(fieldName))))
                            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)));
                        editor.InsertBefore(methodDeclaration, fieldDeclaration);
                        var mockCreationStatement = ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(fieldName),
                            InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("Substitute"), GenericName(Identifier("For")).WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList<TypeSyntax>(genericType)))))));
                        editor.InsertBefore(objectCreation.FirstAncestorOrSelf<StatementSyntax>(), mockCreationStatement);
                    }
                    arguments.Add(Argument(IdentifierName(fieldName)));
                }
                else
                {
                    /*var localVarName = constructorParam.Name + "Mock";
                    var mockCreationStatement = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(localVarName),
                        SyntaxFactory.DefaultExpression(SyntaxFactory.IdentifierName(paramTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))));
                    editor.InsertBefore(objectCreation.FirstAncestorOrSelf<StatementSyntax>(), mockCreationStatement);*/
                    arguments.Add(Argument(IdentifierName("TODO")));
                }
            }

            var newObjectCreation = objectCreation.WithArgumentList(ArgumentList(SeparatedList(arguments)));
            editor.ReplaceNode(objectCreation, newObjectCreation);

            return editor.GetChangedDocument();
        }

        private bool FieldExists(string fieldName, ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Members.OfType<FieldDeclarationSyntax>().Any(x => x.Declaration.Variables.Any(x2 => x2.Identifier.ValueText == fieldName));
        }
    }
}