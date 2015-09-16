' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Partial Friend MustInherit Class VisualBasicSyntaxVisitor
        Public Overridable Function VisitSyntaxToken(token As SyntaxToken) As SyntaxToken
            Debug.Assert(token IsNot Nothing)
            Return token
        End Function

        Public Overridable Function VisitSyntaxTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
            Debug.Assert(trivia IsNot Nothing)
            Return trivia
        End Function
    End Class

    Partial Friend Class VisualBasicSyntaxRewriter
        Inherits VisualBasicSyntaxVisitor

        Public Function VisitList(Of TNode As VisualBasicSyntaxNode)(list As SyntaxList(Of TNode)) As SyntaxList(Of TNode)
            Dim alternate As SyntaxListBuilder(Of TNode) = Nothing
            Dim i As Integer = 0
            Dim n As Integer = list.Count
            Do While (i < n)
                Dim item As TNode = list.Item(i)
                Dim visited As TNode = DirectCast(Me.Visit(item), TNode)
                If item IsNot visited AndAlso alternate.IsNull Then
                    alternate = New SyntaxListBuilder(Of TNode)(n)
                    alternate.AddRange(list, 0, i)
                End If

                If Not alternate.IsNull Then
                    If visited IsNot Nothing AndAlso visited.Kind <> SyntaxKind.None Then
                        alternate.Add(visited)
                    End If
                End If
                i += 1
            Loop
            If Not alternate.IsNull Then
                Return alternate.ToList()
            End If
            Return list
        End Function

        Public Function VisitList(Of TNode As VisualBasicSyntaxNode)(list As SeparatedSyntaxList(Of TNode)) As SeparatedSyntaxList(Of TNode)
            Dim alternate As SeparatedSyntaxListBuilder(Of TNode) = Nothing
            Dim i As Integer = 0
            Dim itemCount As Integer = list.Count
            Dim separatorCount As Integer = list.SeparatorCount

            While i < itemCount
                Dim item = list(i)
                Dim visitedItem = Me.Visit(item)

                Dim separator As SyntaxToken = Nothing
                Dim visitedSeparator As SyntaxToken = Nothing

                If (i < separatorCount) Then

                    separator = list.GetSeparator(i)
                    ' LastTokenReplacer depends on us calling Visit rather than VisitToken for separators.
                    ' It is not clear whether this is desirable/acceptable.
                    Dim visitedSeparatorNode = Me.Visit(separator)
                    Debug.Assert(TypeOf visitedSeparatorNode Is SyntaxToken, "Cannot replace a separator with a non-separator")

                    visitedSeparator = DirectCast(visitedSeparatorNode, SyntaxToken)

                    Debug.Assert((separator Is Nothing AndAlso separator.Kind = SyntaxKind.None) OrElse
                        (visitedSeparator IsNot Nothing AndAlso visitedSeparator.Kind <> SyntaxKind.None),
                    "Cannot delete a separator from a separated list. Removing an element will remove the corresponding separator.")
                End If

                If (item IsNot visitedItem OrElse separator IsNot visitedSeparator) AndAlso alternate.IsNull Then
                    alternate = New SeparatedSyntaxListBuilder(Of TNode)(itemCount)
                    alternate.AddRange(list, i)
                End If

                If Not alternate.IsNull Then
                    If visitedItem IsNot Nothing AndAlso visitedItem.Kind <> SyntaxKind.None Then
                        alternate.Add(DirectCast(visitedItem, TNode))
                        If visitedSeparator IsNot Nothing Then
                            alternate.AddSeparator(visitedSeparator)
                        End If
                    ElseIf i >= separatorCount AndAlso alternate.Count > 0 Then ' last element deleted
                        alternate.RemoveLast() ' delete *preceding* separator
                    End If
                End If

                i += 1
            End While

            If Not alternate.IsNull Then
                Return alternate.ToList()
            End If

            Return list
        End Function

        Public Overrides Function VisitBinaryExpression(ByVal node As BinaryExpressionSyntax) As VisualBasicSyntaxNode
            ' Do not blow the stack due to a deep recursion on the left. 
            ' This is consistent with Parser.ParseExpressionCore implementation.

            Dim childAsBinary = TryCast(node.Left, BinaryExpressionSyntax)

            If childAsBinary Is Nothing Then
                Return VisitBinaryExpressionSimple(node)
            End If

            Dim stack = ArrayBuilder(Of BinaryExpressionSyntax).GetInstance()
            stack.Push(node)

            Dim binary As BinaryExpressionSyntax = childAsBinary
            Dim child As ExpressionSyntax

            Do
                stack.Push(binary)
                child = binary.Left
                childAsBinary = TryCast(child, BinaryExpressionSyntax)

                If childAsBinary Is Nothing Then
                    Exit Do
                End If

                binary = childAsBinary
            Loop

            Dim anyChanges As Boolean = False

            Dim newLeft = DirectCast(Visit(child), ExpressionSyntax)
            If child IsNot newLeft Then
                anyChanges = True
            End If

            Do
                binary = stack.Pop()

                Dim newOperatorToken = DirectCast(Visit(binary.OperatorToken), SyntaxToken)
                If binary._operatorToken IsNot newOperatorToken Then
                    anyChanges = True
                End If

                Dim newRight = DirectCast(Visit(binary._right), ExpressionSyntax)
                If binary._right IsNot newRight Then
                    anyChanges = True
                End If

                If anyChanges Then
                    newLeft = New BinaryExpressionSyntax(binary.Kind, binary.GetDiagnostics, binary.GetAnnotations, newLeft, newOperatorToken, newRight)
                Else
                    newLeft = binary
                End If
            Loop While binary IsNot node

            Debug.Assert(stack.Count = 0)
            stack.Free()

            Return newLeft
        End Function

        Friend Function VisitBinaryExpressionSimple(node As BinaryExpressionSyntax) As VisualBasicSyntaxNode
            Dim anyChanges As Boolean = False

            Dim newLeft = DirectCast(Visit(node._left), ExpressionSyntax)
            If node._left IsNot newLeft Then anyChanges = True
            Dim newOperatorToken = DirectCast(Visit(node.OperatorToken), SyntaxToken)
            If node._operatorToken IsNot newOperatorToken Then anyChanges = True
            Dim newRight = DirectCast(Visit(node._right), ExpressionSyntax)
            If node._right IsNot newRight Then anyChanges = True

            If anyChanges Then
                Return New BinaryExpressionSyntax(node.Kind, node.GetDiagnostics, node.GetAnnotations, newLeft, newOperatorToken, newRight)
            Else
                Return node
            End If
        End Function
    End Class
End Namespace
