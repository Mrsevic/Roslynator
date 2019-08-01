﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslynator.CSharp;
using Roslynator.CSharp.Syntax;

namespace Roslynator.CodeAnalysis.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class InvocationExpressionAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(
                    DiagnosticDescriptors.UnnecessaryNullCheck,
                    DiagnosticDescriptors.UseElementAccess,
                    DiagnosticDescriptors.UseReturnValue);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            base.Initialize(context);

            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax)context.Node;

            if (invocationExpression.ContainsDiagnostics)
                return;

            SimpleMemberInvocationExpressionInfo invocationInfo = SyntaxInfo.SimpleMemberInvocationExpressionInfo(invocationExpression);

            if (!invocationInfo.Success)
                return;

            ISymbol symbol = null;

            string methodName = invocationInfo.NameText;

            switch (invocationInfo.Arguments.Count)
            {
                case 0:
                    {
                        switch (methodName)
                        {
                            case "First":
                                {
                                    if (!context.IsAnalyzerSuppressed(DiagnosticDescriptors.UseElementAccess))
                                        UseElementAccessInsteadOfCallingFirst();

                                    break;
                                }
                        }

                        break;
                    }
                case 1:
                    {
                        switch (methodName)
                        {
                            case "ElementAt":
                                {
                                    if (!context.IsAnalyzerSuppressed(DiagnosticDescriptors.UseElementAccess))
                                        UseElementAccessInsteadOfCallingElementAt();

                                    break;
                                }
                            case "IsKind":
                                {
                                    if (!context.IsAnalyzerSuppressed(DiagnosticDescriptors.UnnecessaryNullCheck))
                                        AnalyzeUnnecessaryNullCheck();

                                    break;
                                }
                        }

                        break;
                    }
            }

            if (!context.IsAnalyzerSuppressed(DiagnosticDescriptors.UseReturnValue)
                && invocationExpression.IsParentKind(SyntaxKind.ExpressionStatement))
            {
                UseReturnValue();
            }

            void AnalyzeUnnecessaryNullCheck()
            {
                ExpressionSyntax expression = invocationInfo.InvocationExpression.WalkUpParentheses();

                SyntaxNode parent = expression.Parent;

                if (!parent.IsKind(SyntaxKind.LogicalAndExpression))
                    return;

                var binaryExpression = (BinaryExpressionSyntax)parent;

                if (expression != binaryExpression.Right)
                    return;

                if (binaryExpression.Left.ContainsDirectives)
                    return;

                if (binaryExpression.OperatorToken.ContainsDirectives)
                    return;

                NullCheckExpressionInfo nullCheckInfo = SyntaxInfo.NullCheckExpressionInfo(binaryExpression.Left, NullCheckStyles.CheckingNotNull & ~NullCheckStyles.HasValue);

                if (!nullCheckInfo.Success)
                    return;

                if (!CSharpFactory.AreEquivalent(invocationInfo.Expression, nullCheckInfo.Expression))
                    return;

                if (!CSharpSymbolUtility.IsIsKindExtensionMethod(invocationExpression, context.SemanticModel, context.CancellationToken))
                    return;

                TextSpan span = TextSpan.FromBounds(binaryExpression.Left.SpanStart, binaryExpression.OperatorToken.Span.End);

                context.ReportDiagnostic(
                    DiagnosticDescriptors.UnnecessaryNullCheck,
                    Location.Create(invocationInfo.InvocationExpression.SyntaxTree, span));
            }

            void UseElementAccessInsteadOfCallingFirst()
            {
                if (!invocationInfo.Expression.GetTrailingTrivia().IsEmptyOrWhitespace())
                    return;

                symbol = context.SemanticModel.GetSymbol(invocationExpression, context.CancellationToken);

                if (symbol?.Kind != SymbolKind.Method
                    || symbol.IsStatic
                    || symbol.DeclaredAccessibility != Accessibility.Public
                    || !RoslynSymbolUtility.IsList(symbol.ContainingType.OriginalDefinition))
                {
                    return;
                }

                TextSpan span = TextSpan.FromBounds(invocationInfo.Name.SpanStart, invocationExpression.Span.End);

                context.ReportDiagnostic(
                    DiagnosticDescriptors.UseElementAccess,
                    Location.Create(invocationExpression.SyntaxTree, span));
            }

            void UseElementAccessInsteadOfCallingElementAt()
            {
                if (!invocationInfo.Expression.GetTrailingTrivia().IsEmptyOrWhitespace())
                    return;

                symbol = context.SemanticModel.GetSymbol(invocationExpression, context.CancellationToken);

                if (symbol?.Kind != SymbolKind.Method
                    || symbol.IsStatic
                    || symbol.DeclaredAccessibility != Accessibility.Public
                    || !symbol.ContainingType.OriginalDefinition.HasMetadataName(RoslynMetadataNames.Microsoft_CodeAnalysis_SyntaxTriviaList))
                {
                    return;
                }

                TextSpan span = TextSpan.FromBounds(invocationInfo.Name.SpanStart, invocationExpression.Span.End);

                context.ReportDiagnostic(
                    DiagnosticDescriptors.UseElementAccess,
                    Location.Create(invocationExpression.SyntaxTree, span));
            }

            void UseReturnValue()
            {
                if (symbol == null)
                    symbol = context.SemanticModel.GetSymbol(invocationExpression, context.CancellationToken);

                if (symbol?.Kind != SymbolKind.Method)
                    return;

                if (!RoslynSymbolUtility.IsRoslynType(symbol.ContainingType))
                    return;

                var methodSymbol = (IMethodSymbol)symbol;

                if (!RoslynSymbolUtility.IsRoslynType(methodSymbol.ReturnType))
                    return;

                context.ReportDiagnostic(DiagnosticDescriptors.UseReturnValue, invocationExpression);
            }
        }
    }
}