﻿using Codartis.NsDepCop.Core.Util;
using Microsoft.CodeAnalysis;

namespace Codartis.NsDepCop.ParserAdapter.Roslyn1x
{
    /// <summary>
    /// Enumerates type dependencies in source code using Roslyn 1.x
    /// </summary>
    public class Roslyn1TypeDependencyEnumerator : RoslynTypeDependencyEnumeratorBase
    {
        public Roslyn1TypeDependencyEnumerator(MessageHandler traceMessageHandler)
            : base(new SyntaxNodeAnalyzer(), traceMessageHandler)
        {
        }

        protected override TypeDependencyEnumeratorSyntaxVisitor CreateSyntaxVisitor(SemanticModel semanticModel, ISyntaxNodeAnalyzer syntaxNodeAnalyzer)
            => new TypeDependencyEnumeratorSyntaxVisitor(semanticModel, syntaxNodeAnalyzer);
    }
}