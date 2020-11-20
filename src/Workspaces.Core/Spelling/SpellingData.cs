﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Roslynator.Spelling
{
    internal class SpellingData
    {
        private static readonly Regex _dictionaryFileName = new Regex(@"\Aroslynator\.spelling(\.|\z)", RegexOptions.IgnoreCase);

        public static SpellingData Empty { get; } = new SpellingData(
            ImmutableHashSet.Create<string>(StringComparer.CurrentCultureIgnoreCase),
            ImmutableHashSet.Create<string>(StringComparer.CurrentCulture),
            ImmutableDictionary.Create<string, string>(StringComparer.CurrentCulture));

        public SpellingData(
            ImmutableHashSet<string> wordList,
            ImmutableHashSet<string> ignoreList,
            ImmutableDictionary<string, string> fixes)
        {
            WordList = wordList;
            IgnoreList = ignoreList;
            Fixes = fixes;
        }

        public ImmutableHashSet<string> WordList { get; }

        public ImmutableHashSet<string> IgnoreList { get; }

        public ImmutableDictionary<string, string> Fixes { get; }

        public SpellingData AddWords(IEnumerable<string> values)
        {
            ImmutableHashSet<string> dictionary = ImmutableHashSet.CreateRange(
                WordList.KeyComparer,
                WordList.Concat(values));

            return new SpellingData(dictionary, IgnoreList, Fixes);
        }

        public SpellingData AddWord(string value)
        {
            return new SpellingData(WordList.Add(value), IgnoreList, Fixes);
        }

        public SpellingData AddFix(string error, string fix)
        {
            return new SpellingData(WordList, IgnoreList, Fixes.Add(error, fix));
        }

        public SpellingData AddIgnoredValue(string value)
        {
            return new SpellingData(WordList, IgnoreList.Add(value), Fixes);
        }

        public static SpellingData LoadFromDirectory(string directoryPath)
        {
            SpellingData spellingData = Empty;

            foreach (string filePath in Directory.EnumerateFiles(directoryPath, "*.dictionary", SearchOption.TopDirectoryOnly))
            {
                string input = Path.GetFileNameWithoutExtension(filePath);
                if (!_dictionaryFileName.IsMatch(input))
                    continue;

                IEnumerable<string> dictionary = File.ReadAllLines(filePath)
                    .Where(f => !string.IsNullOrWhiteSpace(f))
                    .Select(f => f.Trim());

                spellingData = spellingData.AddWords(dictionary);
            }

            return spellingData;
        }
    }
}