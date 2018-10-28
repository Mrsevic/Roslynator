﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using static Roslynator.Logger;

namespace Roslynator.CommandLine
{
    internal class AnalyzeAssemblyCommandExecutor
    {
        public AnalyzeAssemblyCommandExecutor(string language = null)
        {
            Language = language;
        }

        public string Language { get; }

        public CommandResult Execute(AnalyzeAssemblyCommandLineOptions options)
        {
            var analyzerAssemblies = new List<AnalyzerAssembly>();

            foreach (AnalyzerAssembly analyzerAssembly in AnalyzerAssembly.LoadFiles(
                path: options.Path,
                loadAnalyzers: !options.NoAnalyzers,
                loadFixers: !options.NoFixers,
                language: Language))
            {
                analyzerAssemblies.Add(analyzerAssembly);

                WriteLine($"{analyzerAssembly.GetName().Name} ({analyzerAssembly.Location})", ConsoleColor.Green, Verbosity.Minimal);

                DiagnosticAnalyzer[] analyzers = analyzerAssembly
                    .Analyzers
                    .SelectMany(f => f.Value)
                    .Distinct()
                    .ToArray();

                if (analyzers.Length > 0)
                {
                    WriteLine($"  {analyzers.Length} DiagnosticAnalyzers", Verbosity.Normal);

                    foreach (IGrouping<string, DiagnosticAnalyzer> grouping in analyzers
                        .GroupBy(f => f.GetType().Namespace)
                        .OrderBy(f => f.Key))
                    {
                        WriteLine($"    {grouping.Key}", Verbosity.Detailed);

                        foreach (DiagnosticAnalyzer analyzer in grouping.OrderBy(f => f.GetType().Name))
                        {
                            Type type = analyzer.GetType();

                            DiagnosticAnalyzerAttribute attribute = type.GetCustomAttribute<DiagnosticAnalyzerAttribute>();

                            WriteLine($"      {type.Name} ({string.Join(") (", attribute.Languages.OrderBy(f => f))}) ({string.Join(", ", analyzer.SupportedDiagnostics.Select(f => f.Id).OrderBy(f => f))})", Verbosity.Detailed);
                        }
                    }
                }

                CodeFixProvider[] fixers = analyzerAssembly
                    .Fixers
                    .SelectMany(f => f.Value)
                    .Distinct()
                    .ToArray();

                if (fixers.Length > 0)
                {
                    WriteLine($"  {fixers.Length} CodeFixProviders", Verbosity.Normal);

                    foreach (IGrouping<string, CodeFixProvider> grouping in fixers
                        .GroupBy(f => f.GetType().Namespace)
                        .OrderBy(f => f.Key))
                    {
                        WriteLine($"    {grouping.Key}", Verbosity.Detailed);

                        foreach (CodeFixProvider fixer in grouping)
                        {
                            Type type = fixer.GetType();

                            ExportCodeFixProviderAttribute attribute = type.GetCustomAttribute<ExportCodeFixProviderAttribute>();

                            WriteLine($"      {type.Name} ({string.Join(") (", attribute.Languages.Select(f => GetLanguageShortName(f)).OrderBy(f => f))}) ({string.Join(", ", fixer.FixableDiagnosticIds.OrderBy(f => f))})", Verbosity.Detailed);
                        }
                    }
                }
            }

            if (analyzerAssemblies.Count > 0)
            {
                WriteLine(Verbosity.Minimal);

                WriteLine($"{analyzerAssemblies.Count} analyzer {((analyzerAssemblies.Count == 1) ? "assembly" : "assemblies")} found", Verbosity.Minimal);

                foreach (AnalyzerAssembly analyzerAssembly in analyzerAssemblies
                    .OrderBy(f => f.GetName().Name)
                    .ThenBy(f => f.Location))
                {
                    WriteLine($"  {analyzerAssembly.GetName().Name}", ConsoleColor.Green, Verbosity.Normal);
                    WriteLine($"    Location: {analyzerAssembly.Location}", Verbosity.Normal);

                    foreach (KeyValuePair<string, ImmutableArray<DiagnosticAnalyzer>> kvp in analyzerAssembly.Analyzers
                        .OrderBy(f => f.Key))
                    {
                        WriteLine($"    {GetLanguageShortName(kvp.Key)} Analyzers: {kvp.Value.Length}", Verbosity.Normal);
                    }

                    foreach (KeyValuePair<string, ImmutableArray<CodeFixProvider>> kvp in analyzerAssembly.Fixers
                        .OrderBy(f => f.Key))
                    {
                        WriteLine($"    {GetLanguageShortName(kvp.Key)} Fixers:    {kvp.Value.Length}", Verbosity.Normal);
                    }
                }

                WriteLine(Verbosity.Minimal);
            }

            return new CommandResult(success: true);

            string GetLanguageShortName(string languageName)
            {
                switch (languageName)
                {
                    case LanguageNames.CSharp:
                        return languageName;
                    case LanguageNames.VisualBasic:
                        return "VB";
                }

                Debug.Fail(languageName);

                return languageName;
            }
        }
    }
}
