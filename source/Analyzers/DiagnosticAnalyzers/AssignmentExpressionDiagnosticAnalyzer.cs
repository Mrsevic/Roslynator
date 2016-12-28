﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslynator.CSharp.Refactorings;

namespace Roslynator.CSharp.DiagnosticAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AssignmentExpressionDiagnosticAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(
                    DiagnosticDescriptors.UsePostfixUnaryOperatorInsteadOfAssignment,
                    DiagnosticDescriptors.UsePostfixUnaryOperatorInsteadOfAssignmentFadeOut,
                    DiagnosticDescriptors.RemoveRedundantDelegateCreation,
                    DiagnosticDescriptors.RemoveRedundantDelegateCreationFadeOut);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            base.Initialize(context);

            context.RegisterSyntaxNodeAction(f => AnalyzeAssignmentExpression(f), SyntaxKind.AddAssignmentExpression);
            context.RegisterSyntaxNodeAction(f => AnalyzeAssignmentExpression(f), SyntaxKind.SubtractAssignmentExpression);
        }

        private void AnalyzeAssignmentExpression(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;

            UsePostfixUnaryOperatorInsteadOfAssignmentRefactoring.Analyze(context, assignment);

            RemoveRedundantDelegateCreationRefactoring.Analyze(context, assignment);
        }
    }
}
