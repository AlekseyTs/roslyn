' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class VisualBasicSyntaxTreeTests
        <Fact>
        Public Sub WithRootAndOptions_ParsedTree()
            Dim oldTree = SyntaxFactory.ParseSyntaxTree("Class B : End Class")
            Dim newRoot = SyntaxFactory.ParseCompilationUnit("Class C : End Class")
            Dim newOptions = New VisualBasicParseOptions()
            Dim newTree = oldTree.WithRootAndOptions(newRoot, newOptions)
            Dim newText = newTree.GetText()

            Assert.Equal(newRoot.ToString(), newTree.GetRoot().ToString())
            Assert.Same(newOptions, newTree.Options)

            Assert.Null(newText.Encoding)
            Assert.Equal(SourceHashAlgorithm.Sha1, newText.ChecksumAlgorithm)
        End Sub

        <Fact>
        Public Sub WithRootAndOptions_ParsedTreeWithText()
            Dim oldText = SourceText.From("Class B : End Class", Encoding.UTF7, SourceHashAlgorithm.Sha256)
            Dim oldTree = SyntaxFactory.ParseSyntaxTree(oldText)

            Dim newRoot = SyntaxFactory.ParseCompilationUnit("Class C : End Class")
            Dim newOptions = New VisualBasicParseOptions()
            Dim newTree = oldTree.WithRootAndOptions(newRoot, newOptions)
            Dim newText = newTree.GetText()

            Assert.Equal(newRoot.ToString(), newTree.GetRoot().ToString())
            Assert.Same(newOptions, newTree.Options)

            Assert.Same(Encoding.UTF7, newText.Encoding)
            Assert.Equal(SourceHashAlgorithm.Sha256, newText.ChecksumAlgorithm)
        End Sub

        <Fact>
        Public Sub WithRootAndOptions_DummyTree()
            Dim dummy = New VisualBasicSyntaxTree.DummySyntaxTree()
            Dim newRoot = SyntaxFactory.ParseCompilationUnit("Class C : End Class")
            Dim newOptions = New VisualBasicParseOptions()
            Dim newTree = dummy.WithRootAndOptions(newRoot, newOptions)
            Assert.Equal(newRoot.ToString(), newTree.GetRoot().ToString())
            Assert.Same(newOptions, newTree.Options)
        End Sub

        <Fact>
        Public Sub WithFilePath_ParsedTree()
            Dim oldTree = SyntaxFactory.ParseSyntaxTree("Class B : End Class", path:="old.vb")
            Dim newTree = oldTree.WithFilePath("new.vb")
            Dim newText = newTree.GetText()

            Assert.Equal(newTree.FilePath, "new.vb")
            Assert.Equal(oldTree.ToString(), newTree.ToString())

            Assert.Null(newText.Encoding)
            Assert.Equal(SourceHashAlgorithm.Sha1, newText.ChecksumAlgorithm)
        End Sub

        <Fact>
        Public Sub WithFilePath_ParsedTreeWithText()
            Dim oldText = SourceText.From("Class B : End Class", Encoding.UTF7, SourceHashAlgorithm.Sha256)
            Dim oldTree = SyntaxFactory.ParseSyntaxTree(oldText, path:="old.vb")
            Dim newTree = oldTree.WithFilePath("new.vb")
            Dim newText = newTree.GetText()

            Assert.Equal(newTree.FilePath, "new.vb")
            Assert.Equal(oldTree.ToString(), newTree.ToString())

            Assert.Same(Encoding.UTF7, newText.Encoding)
            Assert.Equal(SourceHashAlgorithm.Sha256, newText.ChecksumAlgorithm)
        End Sub

        <Fact>
        Public Sub WithFilePath_DummyTree()
            Dim oldTree = New VisualBasicSyntaxTree.DummySyntaxTree()
            Dim newTree = oldTree.WithFilePath("new.vb")

            Assert.Equal(newTree.FilePath, "new.vb")
            Assert.Equal(oldTree.ToString(), newTree.ToString())
        End Sub

        <Fact, WorkItem(4576, "https://github.com/dotnet/roslyn/issues/4576")>
        Public Sub VisitLongSequenceOfBinaryExpressions()
            Dim tree = SyntaxFactory.ParseSyntaxTree(
$"
Module Test
    Sub Main()
        dim s as string = a0{BuildSequenceOfBinaryExpressions("a")}
    End Sub
End Module
")
            Assert.True(tree.GetDiagnostics().IsEmpty())

            Dim visitor As New TestWalker()
            visitor.Visit(tree.GetRoot())
            Assert.Equal(10001, visitor.AIdentifiers.Count)

            Dim internalrewriter = New TestInternalRewriter()
            Dim result1 = internalrewriter.Visit(DirectCast(tree.GetRoot().Green, InternalSyntax.VisualBasicSyntaxNode))
            Assert.Equal(
$"
Module Test
    Sub Main()
        dim s as string = b0{BuildSequenceOfBinaryExpressions("b")}
    End Sub
End Module
", result1.ToFullString())

            Dim rewriter As New TestRewriter()
            Dim result2 = rewriter.Visit(tree.GetRoot())

            Assert.Equal(
$"
Module Test
    Sub Main()
        dim s as string = c0{BuildSequenceOfBinaryExpressions("c")}
    End Sub
End Module
", result2.ToFullString())
        End Sub

        Private Shared Function BuildSequenceOfBinaryExpressions(identifierPrefix As String) As String
            Dim builder = New StringBuilder()

            For i As Integer = 1 To 10000
                builder.Append(" & ")
                builder.Append(identifierPrefix)
                builder.Append(i)
            Next

            Return builder.ToString()
        End Function

        Class TestWalker
            Inherits VisualBasicSyntaxWalker

            Public AIdentifiers As New HashSet(Of IdentifierNameSyntax)

            Public Overrides Sub VisitIdentifierName(node As IdentifierNameSyntax)
                If node.Identifier.ValueText.StartsWith("a", StringComparison.Ordinal) Then
                    AIdentifiers.Add(node)
                End If

                MyBase.VisitIdentifierName(node)
            End Sub
        End Class

        Class TestRewriter
            Inherits VisualBasicSyntaxRewriter

            Public Overrides Function VisitIdentifierName(node As IdentifierNameSyntax) As SyntaxNode
                If node.Identifier.ValueText.StartsWith("a", StringComparison.Ordinal) Then
                    Return SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("c" + node.Identifier.ValueText.Substring(1)).
                                                                          WithLeadingTrivia(node.Identifier.LeadingTrivia).
                                                                          WithTrailingTrivia(node.Identifier.TrailingTrivia))
                End If

                Return MyBase.VisitIdentifierName(node)
            End Function
        End Class

        Private Class TestInternalRewriter
            Inherits Syntax.InternalSyntax.VisualBasicSyntaxRewriter

            Public Overrides Function VisitIdentifierName(node As InternalSyntax.IdentifierNameSyntax) As InternalSyntax.VisualBasicSyntaxNode
                If node.Identifier.ValueText.StartsWith("a", StringComparison.Ordinal) Then
                    Return InternalSyntax.SyntaxFactory.IdentifierName(DirectCast(InternalSyntax.SyntaxFactory.Identifier("b" + node.Identifier.ValueText.Substring(1)).
                                                                                      WithLeadingTrivia(node.Identifier.GetLeadingTrivia()).
                                                                                      WithTrailingTrivia(node.Identifier.GetTrailingTrivia()), InternalSyntax.IdentifierTokenSyntax))
                End If

                Return MyBase.VisitIdentifierName(node)
            End Function
        End Class
    End Class
End Namespace
