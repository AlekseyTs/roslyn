// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class CSharpSyntaxRewriter : CSharpSyntaxVisitor<CSharpSyntaxNode>
    {
        protected readonly bool VisitIntoStructuredTrivia;

        public CSharpSyntaxRewriter(bool visitIntoStructuredTrivia = false)
        {
            this.VisitIntoStructuredTrivia = visitIntoStructuredTrivia;
        }

        public override CSharpSyntaxNode VisitToken(SyntaxToken token)
        {
            var leading = this.VisitList(token.LeadingTrivia);
            var trailing = this.VisitList(token.TrailingTrivia);

            if (leading != token.LeadingTrivia || trailing != token.TrailingTrivia)
            {
                if (leading != token.LeadingTrivia)
                {
                    token = token.WithLeadingTrivia(leading.Node);
                }

                if (trailing != token.TrailingTrivia)
                {
                    token = token.WithTrailingTrivia(trailing.Node);
                }
            }

            return token;
        }

        public override CSharpSyntaxNode VisitTrivia(SyntaxTrivia trivia)
        {
            return trivia;
        }

        public SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> list) where TNode : CSharpSyntaxNode
        {
            SyntaxListBuilder alternate = null;
            for (int i = 0, n = list.Count; i < n; i++)
            {
                var item = list[i];
                var visited = (TNode)this.Visit(item);
                if (item != visited && alternate == null)
                {
                    alternate = new SyntaxListBuilder(n);
                    alternate.AddRange(list, 0, i);
                }

                if (alternate != null)
                {
                    Debug.Assert(visited != null && visited.Kind != SyntaxKind.None, "Cannot remove node using Syntax.InternalSyntax.SyntaxRewriter.");
                    alternate.Add(visited);
                }
            }

            if (alternate != null)
            {
                return alternate.ToList();
            }

            return list;
        }

        public SeparatedSyntaxList<TNode> VisitList<TNode>(SeparatedSyntaxList<TNode> list) where TNode : CSharpSyntaxNode
        {
            var withSeps = list.GetWithSeparators();
            var result = this.VisitList(withSeps);
            if (result != withSeps)
            {
                return result.AsSeparatedList<TNode>();
            }

            return list;
        }

        public override CSharpSyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            // Do not blow the stack due to a deep recursion on the left. 
            // This is consistent with Parser.ParseSubExpressionCore implementation.

            var childAsBinary = node.Left as BinaryExpressionSyntax;

            if (childAsBinary == null)
            {
                return VisitBinaryExpressionSimple(node);
            }

            var stack = ArrayBuilder<BinaryExpressionSyntax>.GetInstance();
            stack.Push(node);

            BinaryExpressionSyntax binary = childAsBinary;
            ExpressionSyntax child;

            while (true)
            {
                stack.Push(binary);
                child = binary.Left;
                childAsBinary = child as BinaryExpressionSyntax;

                if (childAsBinary == null)
                {
                    break;
                }

                binary = childAsBinary;
            }

            var left = (ExpressionSyntax)this.Visit(child);

            do
            {
                binary = stack.Pop();

                var operatorToken = (SyntaxToken)this.Visit(binary.OperatorToken);
                var right = (ExpressionSyntax)this.Visit(binary.Right);

                left = binary.Update(left, operatorToken, right);
            }
            while ((object)binary != node);

            Debug.Assert(stack.Count == 0);
            stack.Free();

            return left;
        }

        private CSharpSyntaxNode VisitBinaryExpressionSimple(BinaryExpressionSyntax node)
        {
            var left = (ExpressionSyntax)this.Visit(node.Left);
            var operatorToken = (SyntaxToken)this.Visit(node.OperatorToken);
            var right = (ExpressionSyntax)this.Visit(node.Right);
            return node.Update(left, operatorToken, right);
        }
    }
}
