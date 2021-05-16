﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codartis.NsDepCop.Core.Interface.Analysis;
using Codartis.NsDepCop.Core.Util;
using DotNet.Globbing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Codartis.NsDepCop.ParserAdapter
{
    /// <summary>
    /// Abstract base class for type dependency enumerators that use Roslyn as the parser.
    /// </summary>
    public abstract class RoslynTypeDependencyEnumeratorBase : ITypeDependencyEnumerator
    {
        private readonly ISyntaxNodeAnalyzer _syntaxNodeAnalyzer;
        private readonly MessageHandler _traceMessageHandler;

        protected RoslynTypeDependencyEnumeratorBase(ISyntaxNodeAnalyzer syntaxNodeAnalyzer, MessageHandler traceMessageHandler)
        {
            _syntaxNodeAnalyzer = syntaxNodeAnalyzer ?? throw new ArgumentNullException(nameof(syntaxNodeAnalyzer));
            _traceMessageHandler = traceMessageHandler;
        }

        protected virtual CSharpParseOptions ParseOptions => null;
        protected abstract TypeDependencyEnumeratorSyntaxVisitor CreateSyntaxVisitor(SemanticModel semanticModel, ISyntaxNodeAnalyzer syntaxNodeAnalyzer);

        public IEnumerable<TypeDependency> GetTypeDependencies(
            IEnumerable<string> sourceFilePaths, 
            IEnumerable<string> referencedAssemblyPaths,
            IEnumerable<Glob> sourcePathExclusionGlobs)
        {
            var referencedAssemblies = referencedAssemblyPaths.Select(LoadMetadata).Where(i => i != null).ToList();
            var syntaxTrees = sourceFilePaths.Select(ParseFile).Where(i => i != null).ToList();

            var compilation = CSharpCompilation.Create("NsDepCopProject", syntaxTrees, referencedAssemblies,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

            foreach (var syntaxTree in syntaxTrees.Where(i => !IsExcludedFilePath(i.FilePath, sourcePathExclusionGlobs)))
            foreach (var typeDependency in GetTypeDependenciesForSyntaxTree(compilation, syntaxTree, _syntaxNodeAnalyzer))
                yield return typeDependency;
        }

        private static bool IsExcludedFilePath(string filePath, IEnumerable<Glob> sourcePathExclusionGlobs)
        {
            return sourcePathExclusionGlobs.Any(i => i.IsMatch(filePath));
        }

        private IEnumerable<TypeDependency> GetTypeDependenciesForSyntaxTree(CSharpCompilation compilation, SyntaxTree syntaxTree, ISyntaxNodeAnalyzer syntaxNodeAnalyzer)
        {
            var documentRootNode = syntaxTree.GetRoot();
            if (documentRootNode == null)
                return Enumerable.Empty<TypeDependency>();

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var syntaxVisitor = CreateSyntaxVisitor(semanticModel, syntaxNodeAnalyzer);
            syntaxVisitor.Visit(documentRootNode);
            return syntaxVisitor.TypeDependencies;
        }

        public IEnumerable<TypeDependency> GetTypeDependencies(
            ISyntaxNode syntaxNode, 
            ISemanticModel semanticModel,
            IEnumerable<Glob> sourcePathExclusionGlobs)
        {
            var roslynSyntaxNode = Unwrap<SyntaxNode>(syntaxNode);

            return IsExcludedFilePath(roslynSyntaxNode?.SyntaxTree?.FilePath, sourcePathExclusionGlobs) 
                ? Enumerable.Empty<TypeDependency>() 
                : _syntaxNodeAnalyzer.GetTypeDependencies(roslynSyntaxNode, Unwrap<SemanticModel>(semanticModel));
        }

        private static TUnwrapped Unwrap<TUnwrapped>(object wrappedValue)
        {
            if (wrappedValue == null)
                throw new ArgumentNullException(nameof(wrappedValue));

            if (!(wrappedValue is ObjectWrapper<TUnwrapped>))
                throw new ArgumentException("Wrapped value should be a subclass of ObjectWrapper<T>).");

            return ((ObjectWrapper<TUnwrapped>) wrappedValue).Value;
        }

        private MetadataReference LoadMetadata(string fileName)
        {
            try
            {
                return MetadataReference.CreateFromFile(fileName);
            }
            catch (Exception e)
            {
                LogTraceMessage($"Error loading metadata file '{fileName}': {e}");
                return null;
            }
        }

        private SyntaxTree ParseFile(string fileName)
        {
            try
            {
                using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var streamReader = new StreamReader(stream))
                {
                    var sourceText = streamReader.ReadToEnd();
                    return CSharpSyntaxTree.ParseText(sourceText, ParseOptions, fileName);
                }
            }
            catch (Exception e)
            {
                LogTraceMessage($"Error parsing source file '{fileName}': {e}");
                return null;
            }
        }

        private void LogTraceMessage(string message) => _traceMessageHandler?.Invoke(message);
    }
}