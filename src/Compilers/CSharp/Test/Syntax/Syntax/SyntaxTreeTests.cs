// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SyntaxTreeTests
    {
        [Fact]
        public void WithRootAndOptions_ParsedTree()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("class B {}");
            var newRoot = SyntaxFactory.ParseCompilationUnit("class C {}");
            var newOptions = new CSharpParseOptions();
            var newTree = oldTree.WithRootAndOptions(newRoot, newOptions);
            var newText = newTree.GetText();

            Assert.Equal(newRoot.ToString(), newTree.GetRoot().ToString());
            Assert.Same(newOptions, newTree.Options);

            Assert.Null(newText.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha1, newText.ChecksumAlgorithm);
        }

        [Fact]
        public void WithRootAndOptions_ParsedTreeWithText()
        {
            var oldText = SourceText.From("class B {}", Encoding.UTF7, SourceHashAlgorithm.Sha256);
            var oldTree = SyntaxFactory.ParseSyntaxTree(oldText);

            var newRoot = SyntaxFactory.ParseCompilationUnit("class C {}");
            var newOptions = new CSharpParseOptions();
            var newTree = oldTree.WithRootAndOptions(newRoot, newOptions);
            var newText = newTree.GetText();

            Assert.Equal(newRoot.ToString(), newTree.GetRoot().ToString());
            Assert.Same(newOptions, newTree.Options);
            Assert.Same(Encoding.UTF7, newText.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha256, newText.ChecksumAlgorithm);
        }

        [Fact]
        public void WithRootAndOptions_DummyTree()
        {
            var dummy = new CSharpSyntaxTree.DummySyntaxTree();
            var newRoot = SyntaxFactory.ParseCompilationUnit("class C {}");
            var newOptions = new CSharpParseOptions();
            var newTree = dummy.WithRootAndOptions(newRoot, newOptions);
            Assert.Equal(newRoot.ToString(), newTree.GetRoot().ToString());
            Assert.Same(newOptions, newTree.Options);
        }

        [Fact]
        public void WithFilePath_ParsedTree()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("class B {}", path: "old.cs");
            var newTree = oldTree.WithFilePath("new.cs");
            var newText = newTree.GetText();

            Assert.Equal(newTree.FilePath, "new.cs");
            Assert.Equal(oldTree.ToString(), newTree.ToString());

            Assert.Null(newText.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha1, newText.ChecksumAlgorithm);
        }

        [Fact]
        public void WithFilePath_ParsedTreeWithText()
        {
            var oldText = SourceText.From("class B {}", Encoding.UTF7, SourceHashAlgorithm.Sha256);
            var oldTree = SyntaxFactory.ParseSyntaxTree(oldText, path: "old.cs");

            var newTree = oldTree.WithFilePath("new.cs");
            var newText = newTree.GetText();

            Assert.Equal(newTree.FilePath, "new.cs");
            Assert.Equal(oldTree.ToString(), newTree.ToString());

            Assert.Same(Encoding.UTF7, newText.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha256, newText.ChecksumAlgorithm);
        }

        [Fact]
        public void WithFilePath_DummyTree()
        {
            var oldTree = new CSharpSyntaxTree.DummySyntaxTree();
            var newTree = oldTree.WithFilePath("new.cs");

            Assert.Equal(newTree.FilePath, "new.cs");
            Assert.Equal(oldTree.ToString(), newTree.ToString());
        }

        [Fact, WorkItem(4576, "https://github.com/dotnet/roslyn/issues/4576")]
        public void VisitLongSequenceOfBinaryExpressions()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(
@"
class Test
{ 
    void Main()
    {
" + $"        string s = a0{ BuildSequenceOfBinaryExpressions("a") };" + @"
    }
}
");
            Assert.True(tree.GetDiagnostics().IsEmpty());

            var visitor = new TestWalker();
            visitor.Visit(tree.GetRoot());
            Assert.Equal(10001, visitor.AIdentifiers.Count);

            var internalrewriter = new TestInternalRewriter();
            var result1 = internalrewriter.Visit((Syntax.InternalSyntax.CSharpSyntaxNode)tree.GetRoot().Green);
            Assert.Equal(
@"
class Test
{ 
    void Main()
    {
" + $"        string s = b0{ BuildSequenceOfBinaryExpressions("b") };" + @"
    }
}
", result1.ToFullString());

            var rewriter = new TestRewriter();
            var result2 = rewriter.Visit(tree.GetRoot());

            Assert.Equal(
@"
class Test
{ 
    void Main()
    {
" + $"        string s = c0{ BuildSequenceOfBinaryExpressions("c") };" + @"
    }
}
", result2.ToFullString());
        }

        private static string BuildSequenceOfBinaryExpressions(string identifierPrefix)
        {
            var builder = new StringBuilder();

            for (int i = 1; i <= 10000; i++)
            {
                builder.Append(" + ");
                builder.Append(identifierPrefix);
                builder.Append(i);
            }

            return builder.ToString();
        }

        class TestWalker : CSharpSyntaxWalker
        {
            public HashSet<IdentifierNameSyntax> AIdentifiers = new HashSet<IdentifierNameSyntax>();

            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (node.Identifier.ValueText.StartsWith("a", StringComparison.Ordinal))
                {
                    AIdentifiers.Add(node);
                }

                base.VisitIdentifierName(node);
            }
        }

        class TestRewriter : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (node.Identifier.ValueText.StartsWith("a", StringComparison.Ordinal))
                {
                    return SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("c" + node.Identifier.ValueText.Substring(1)).
                                                                          WithLeadingTrivia(node.Identifier.LeadingTrivia).
                                                                          WithTrailingTrivia(node.Identifier.TrailingTrivia));
                }

                return base.VisitIdentifierName(node);
            }
        }

        private class TestInternalRewriter : Syntax.InternalSyntax.CSharpSyntaxRewriter
        {
            public override Syntax.InternalSyntax.CSharpSyntaxNode VisitIdentifierName(Syntax.InternalSyntax.IdentifierNameSyntax node)
            {
                if (node.Identifier.ValueText.StartsWith("a", StringComparison.Ordinal))
                {
                    return Syntax.InternalSyntax.SyntaxFactory.IdentifierName(Syntax.InternalSyntax.SyntaxFactory.Identifier("b" + node.Identifier.ValueText.Substring(1)).
                                                                                      WithLeadingTrivia(node.Identifier.GetLeadingTrivia()).
                                                                                      WithTrailingTrivia(node.Identifier.GetTrailingTrivia()));
                }

                return base.VisitIdentifierName(node);
            }
        }
    }
}
