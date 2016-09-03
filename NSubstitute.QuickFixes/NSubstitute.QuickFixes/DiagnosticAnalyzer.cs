using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NSubstitute.QuickFixes
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NSubstituteHelperAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NSHA100";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Testing";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSemanticModelAction(AnalyzeSymbol);
        }

        private static void AnalyzeSymbol(SemanticModelAnalysisContext context)
        {
            var expression = context.SemanticModel.SyntaxTree.GetRoot().DescendantNodes()
                                        .OfType<ObjectCreationExpressionSyntax>()
                                        .FirstOrDefault();

            if (expression == null)
                return;

            var constructorSymbolInfo = context.SemanticModel.GetSymbolInfo(expression);

            if (constructorSymbolInfo.CandidateReason != CandidateReason.OverloadResolutionFailure)
                return;

            var invokedSymbol = constructorSymbolInfo.CandidateSymbols.FirstOrDefault(x => x is IMethodSymbol);

            if (invokedSymbol == null)
                return;

            var namespaceLookup = context.SemanticModel.LookupNamespacesAndTypes(0, name: "NSubstitute").FirstOrDefault();
            if (namespaceLookup == null)
                return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, expression.GetLocation()));
        }
    }
}
