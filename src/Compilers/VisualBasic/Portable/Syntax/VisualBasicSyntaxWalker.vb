' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Represents a <see cref="VisualBasicSyntaxVisitor"/> that descends an entire <see cref="SyntaxNode"/> tree
    ''' visiting each SyntaxNode and its child <see cref="SyntaxNode"/>s and <see cref="SyntaxToken"/>s in depth-first order.
    ''' </summary>
    Public MustInherit Class VisualBasicSyntaxWalker
        Inherits VisualBasicSyntaxVisitor

        Protected ReadOnly Property Depth As SyntaxWalkerDepth

        Protected Sub New(Optional depth As SyntaxWalkerDepth = SyntaxWalkerDepth.Node)
            Me.Depth = depth
        End Sub

        Private _recursionDepth As Integer

        Public Overrides Sub Visit(node As SyntaxNode)
            If node IsNot Nothing Then
                _recursionDepth += 1

                If _recursionDepth > Syntax.InternalSyntax.Parser.MaxUncheckedRecursionDepth Then
                    PortableShim.RuntimeHelpers.EnsureSufficientExecutionStack()
                End If

                DirectCast(node, VisualBasicSyntaxNode).Accept(Me)

                _recursionDepth -= 1
            End If
        End Sub

        Public Overrides Sub DefaultVisit(node As SyntaxNode)
            Dim binary = TryCast(node, BinaryExpressionSyntax)

            If binary IsNot Nothing Then
                VisitBinaryExpressionInternal(binary)
                Return
            End If

            Dim list = node.ChildNodesAndTokens()
            Dim childCnt = list.Count

            Dim i As Integer = 0
            Do
                Dim child = list(i)
                i = i + 1

                Dim asNode = child.AsNode()
                If asNode IsNot Nothing Then
                    If Depth >= SyntaxWalkerDepth.Node Then
                        Me.Visit(asNode)
                    End If
                Else
                    If Depth >= SyntaxWalkerDepth.Token Then
                        Me.VisitToken(child.AsToken())
                    End If
                End If
            Loop While i < childCnt

        End Sub

        Private Sub VisitBinaryExpressionInternal(node As BinaryExpressionSyntax)
            ' Do not blow the stack due to a deep recursion on the left. 
            ' This is consistent with Parser.ParseExpressionCore implementation.

            Dim binary As BinaryExpressionSyntax = node
            Dim child As ExpressionSyntax

            Do
                child = binary.Left
                Dim childAsBinary = TryCast(child, BinaryExpressionSyntax)

                If childAsBinary Is Nothing Then
                    Exit Do
                End If

                binary = childAsBinary
            Loop

            If Depth >= SyntaxWalkerDepth.Node Then
                Me.Visit(child)
            End If

            Do
                binary = DirectCast(child.Parent, BinaryExpressionSyntax)

                If Depth >= SyntaxWalkerDepth.Token Then
                    Me.VisitToken(binary.OperatorToken)
                End If

                If Depth >= SyntaxWalkerDepth.Node Then
                    Me.Visit(binary.Right)
                End If

                child = binary
            Loop While child IsNot node
        End Sub

        Public Overridable Sub VisitToken(token As SyntaxToken)
            If Depth >= SyntaxWalkerDepth.Trivia Then
                Me.VisitLeadingTrivia(token)
                Me.VisitTrailingTrivia(token)
            End If
        End Sub

        Public Overridable Sub VisitLeadingTrivia(token As SyntaxToken)
            If token.HasLeadingTrivia Then
                For Each tr In token.LeadingTrivia
                    VisitTrivia(tr)
                Next
            End If
        End Sub

        Public Overridable Sub VisitTrailingTrivia(token As SyntaxToken)
            If token.HasTrailingTrivia Then
                For Each tr In token.TrailingTrivia
                    VisitTrivia(tr)
                Next
            End If
        End Sub

        Public Overridable Sub VisitTrivia(trivia As SyntaxTrivia)
            If Depth >= SyntaxWalkerDepth.StructuredTrivia AndAlso trivia.HasStructure Then
                Visit(DirectCast(trivia.GetStructure(), VisualBasicSyntaxNode))
            End If
        End Sub
    End Class
End Namespace
