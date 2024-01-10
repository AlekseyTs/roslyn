﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class ParamsCollectionTests : CompilingTestBase
    {
        private static string ExpectedOutput(string output)
        {
            return ExecutionConditionUtil.IsMonoOrCoreClr ? output : null;
        }

        [Fact]
        public void Span()
        {
            var src = @"
class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    static void Test(params System.Span<long> a)
    {
        if (a.Length == 0)
        {
            System.Console.WriteLine(a.Length);
        }
        else
        {
            System.Console.WriteLine(""{0}: {1} ... {2}"", a.Length, a[0], a[^1]);
        }
    }

    static void Test2()
    {
        Test([2, 3]);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ?
                            Verification.FailsILVerify with { ILVerifyMessage = "[InlineArrayAsSpan]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0xc }" }
                            : Verification.Skipped,
                expectedOutput: ExpectedOutput(@"
0
1: 1 ... 1
2: 2 ... 3
")).VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Take(3).ToArray();

            Assert.Equal("Test()", nodes[0].ToString());
            comp.VerifyOperationTree(nodes[0], expectedOperationTree: """
IInvocationOperation (void Program.Test(params System.Span<System.Int64> a)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Test()')
Instance Receiver:
  null
Arguments(1):
    IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: a) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Test()')
        ICollectionExpressionOperation (0 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Span<System.Int64>, IsImplicit) (Syntax: 'Test()')
          Elements(0)
      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
""");

            Assert.Equal("Test(1)", nodes[1].ToString());
            comp.VerifyOperationTree(nodes[1], expectedOperationTree: """
IInvocationOperation (void Program.Test(params System.Span<System.Int64> a)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Test(1)')
Instance Receiver:
  null
Arguments(1):
    IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: a) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Test(1)')
        ICollectionExpressionOperation (1 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Span<System.Int64>, IsImplicit) (Syntax: 'Test(1)')
          Elements(1):
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 1, IsImplicit) (Syntax: '1')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand:
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
""");
            Assert.Equal("Test(2, 3)", nodes[2].ToString());
            comp.VerifyOperationTree(nodes[2], expectedOperationTree: """
IInvocationOperation (void Program.Test(params System.Span<System.Int64> a)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Test(2, 3)')
Instance Receiver:
  null
Arguments(1):
    IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: a) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'Test(2, 3)')
        ICollectionExpressionOperation (2 elements, ConstructMethod: null) (OperationKind.CollectionExpression, Type: System.Span<System.Int64>, IsImplicit) (Syntax: 'Test(2, 3)')
          Elements(2):
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 2, IsImplicit) (Syntax: '2')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand:
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 3, IsImplicit) (Syntax: '3')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand:
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
""");

            var argument = nodes[1].ArgumentList.Arguments[0].Expression;
            var model = comp.GetSemanticModel(tree);

            var typeInfo = model.GetTypeInfo(argument);
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.Int64", typeInfo.ConvertedType.ToTestDisplayString());

            Assert.True(model.GetConversion(argument).IsNumeric);
        }

        [Fact]
        public void Span_InAttribute()
        {
            var src = @"
[Test()]
class C1;

[Test(1)]
class C2;

[Test(2, 3)]
class C3;

class Test : System.Attribute
{
    public Test(params System.Span<long> a) {}
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (2,2): error CS0181: Attribute constructor parameter 'a' has type 'Span<long>', which is not a valid attribute parameter type
                // [Test()]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Test").WithArguments("a", "System.Span<long>").WithLocation(2, 2),
                // (5,2): error CS0181: Attribute constructor parameter 'a' has type 'Span<long>', which is not a valid attribute parameter type
                // [Test(1)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Test").WithArguments("a", "System.Span<long>").WithLocation(5, 2),
                // (8,2): error CS0181: Attribute constructor parameter 'a' has type 'Span<long>', which is not a valid attribute parameter type
                // [Test(2, 3)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Test").WithArguments("a", "System.Span<long>").WithLocation(8, 2)
                );

            assertAttributeData("C1");
            assertAttributeData("C2");
            assertAttributeData("C3");

            var tree = comp.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(3, nodes.Length);

            var model = comp.GetSemanticModel(tree);

            foreach (LiteralExpressionSyntax expression in nodes)
            {
                assertTypeInfo(expression);
            }

            void assertTypeInfo(LiteralExpressionSyntax expression)
            {
                var typeInfo = model.GetTypeInfo(expression);
                Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
                Assert.Equal("System.Int64", typeInfo.ConvertedType.ToTestDisplayString());

                Assert.True(model.GetConversion(expression).IsNumeric);
            }

            void assertAttributeData(string name)
            {
                var attributeData1 = comp.GetTypeByMetadataName(name).GetAttributes().Single();
                Assert.True(attributeData1.HasErrors);

                var c1Arg = attributeData1.ConstructorArguments.Single();
                Assert.Equal(TypedConstantKind.Error, c1Arg.Kind);
                Assert.Equal("System.Span<System.Int64>", c1Arg.Type.ToTestDisplayString());
                Assert.Null(c1Arg.Value);
                Assert.Throws<System.InvalidOperationException>(() => c1Arg.Values);
            }
        }

        [Fact]
        public void ReadOnlySpan()
        {
            var src = @"
class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    static void Test(params System.ReadOnlySpan<long> a)
    {
        if (a.Length == 0)
        {
            System.Console.WriteLine(a.Length);
        }
        else
        {
            System.Console.WriteLine(""{0}: {1} ... {2}"", a.Length, a[0], a[^1]);
        }
    }

    static void Test2()
    {
        Test([2, 3]);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped,
                expectedOutput: ExpectedOutput(@"
0
1: 1 ... 1
2: 2 ... 3
")).VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlySpan_InAttribute()
        {
            var src = @"
[Test()]
class C1;

[Test(1)]
class C2;

[Test(2, 3)]
class C3;

class Test : System.Attribute
{
    public Test(params System.ReadOnlySpan<long> a) {}
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (2,2): error CS0181: Attribute constructor parameter 'a' has type 'ReadOnlySpan<long>', which is not a valid attribute parameter type
                // [Test()]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Test").WithArguments("a", "System.ReadOnlySpan<long>").WithLocation(2, 2),
                // (5,2): error CS0181: Attribute constructor parameter 'a' has type 'ReadOnlySpan<long>', which is not a valid attribute parameter type
                // [Test(1)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Test").WithArguments("a", "System.ReadOnlySpan<long>").WithLocation(5, 2),
                // (8,2): error CS0181: Attribute constructor parameter 'a' has type 'ReadOnlySpan<long>', which is not a valid attribute parameter type
                // [Test(2, 3)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Test").WithArguments("a", "System.ReadOnlySpan<long>").WithLocation(8, 2)
                );

            assertAttributeData("C1");
            assertAttributeData("C2");
            assertAttributeData("C3");

            var tree = comp.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(3, nodes.Length);

            var model = comp.GetSemanticModel(tree);

            foreach (LiteralExpressionSyntax expression in nodes)
            {
                assertTypeInfo(expression);
            }

            void assertTypeInfo(LiteralExpressionSyntax expression)
            {
                var typeInfo = model.GetTypeInfo(expression);
                Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
                Assert.Equal("System.Int64", typeInfo.ConvertedType.ToTestDisplayString());

                Assert.True(model.GetConversion(expression).IsNumeric);
            }

            void assertAttributeData(string name)
            {
                var attributeData1 = comp.GetTypeByMetadataName(name).GetAttributes().Single();
                Assert.True(attributeData1.HasErrors);

                var c1Arg = attributeData1.ConstructorArguments.Single();
                Assert.Equal(TypedConstantKind.Error, c1Arg.Kind);
                Assert.Equal("System.ReadOnlySpan<System.Int64>", c1Arg.Type.ToTestDisplayString());
                Assert.Null(c1Arg.Value);
                Assert.Throws<System.InvalidOperationException>(() => c1Arg.Values);
            }
        }

        [Fact]
        public void String()
        {
            var src = @"
class Program
{
    static void Main()
    {
        Test();
        Test('a');
        Test('b', 'c');
    }

    static void Test(params string a)
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            // PROTOTYPE(ParamsCollections): Note, there is no error at the declaration site, because
            //                               according to https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/collection-expressions.md#conversions,
            //                               there is a conversion from a collection expression consisting of `char`s to a `string` type.
            //                               Even though `string` lacks APIs needed to perform the conversion.
            //                               Similar situation can happen with other types. Are we fine with this behavior, or should we
            //                               enforce existence of at least some APIs at the declaration site?
            comp.VerifyDiagnostics(
                // (6,9): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         Test();
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test()").WithArguments("string", "0").WithLocation(6, 9),
                // (7,9): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         Test('a');
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test('a')").WithArguments("string", "0").WithLocation(7, 9),
                // (7,14): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         Test('a');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'a'").WithArguments("string", "Add").WithLocation(7, 14),
                // (8,9): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         Test('b', 'c');
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test('b', 'c')").WithArguments("string", "0").WithLocation(8, 9),
                // (8,14): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         Test('b', 'c');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'b'").WithArguments("string", "Add").WithLocation(8, 14),
                // (8,19): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         Test('b', 'c');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'c'").WithArguments("string", "Add").WithLocation(8, 19)
                );
        }

        [Fact]
        public void String_InAttribute()
        {
            var src = @"
[Test()]
class C1;

[Test('1')]
class C2;

[Test('2', '3')]
class C3;

class Test : System.Attribute
{
    public Test(params string a) {}
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (2,2): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                // [Test()]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test()").WithArguments("string", "0").WithLocation(2, 2),
                // (5,2): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                // [Test('1')]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test('1')").WithArguments("string", "0").WithLocation(5, 2),
                // (5,7): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                // [Test('1')]
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'1'").WithArguments("string", "Add").WithLocation(5, 7),
                // (8,2): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                // [Test('2', '3')]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Test('2', '3')").WithArguments("string", "0").WithLocation(8, 2),
                // (8,7): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                // [Test('2', '3')]
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'2'").WithArguments("string", "Add").WithLocation(8, 7),
                // (8,12): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                // [Test('2', '3')]
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'3'").WithArguments("string", "Add").WithLocation(8, 12)
                );

            assertAttributeData("C1");
            assertAttributeData("C2");
            assertAttributeData("C3");

            var tree = comp.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(3, nodes.Length);

            var model = comp.GetSemanticModel(tree);

            foreach (LiteralExpressionSyntax expression in nodes)
            {
                assertTypeInfo(expression);
            }

            void assertTypeInfo(LiteralExpressionSyntax expression)
            {
                var typeInfo = model.GetTypeInfo(expression);
                Assert.Equal("System.Char", typeInfo.Type.ToTestDisplayString());
                Assert.Equal("System.Char", typeInfo.ConvertedType.ToTestDisplayString());

                Assert.True(model.GetConversion(expression).IsIdentity);
            }

            void assertAttributeData(string name)
            {
                var attributeData1 = comp.GetTypeByMetadataName(name).GetAttributes().Single();
                Assert.True(attributeData1.HasErrors);

                var c1Arg = attributeData1.ConstructorArguments.Single();
                Assert.Equal(TypedConstantKind.Error, c1Arg.Kind);
                Assert.Equal("System.String", c1Arg.Type.ToTestDisplayString());
                Assert.Null(c1Arg.Value);
                Assert.Throws<System.InvalidOperationException>(() => c1Arg.Values);
            }
        }

        [Fact]
        public void CreateMethod()
        {
            var src = """
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
class MyCollection
{
    public long[] Array;
    public IEnumerator<long> GetEnumerator() => throw null;
}
class MyCollectionBuilder
{
    public static MyCollection Create(ReadOnlySpan<long> items) => new MyCollection() { Array = items.ToArray() };
}

class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    static void Test(params MyCollection a)
    {
        if (a.Array.Length == 0)
        {
            System.Console.WriteLine(a.Array.Length);
        }
        else
        {
            System.Console.WriteLine("{0}: {1} ... {2}", a.Array.Length, a.Array[0], a.Array[^1]);
        }
    }

    static void Test2()
    {
        Test([2, 3]);
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped,
                expectedOutput: ExpectedOutput(@"
0
1: 1 ... 1
2: 2 ... 3
")).VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void CreateMethod_InAttribute(bool asStruct)
        {
            var src = @"
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create))]
" + (asStruct ? "struct" : "class") + @" MyCollection
{
    public IEnumerator<long> GetEnumerator() => throw new InvalidOperationException();
}
class MyCollectionBuilder
{
    public static MyCollection Create(ReadOnlySpan<long> items) => new MyCollection();
}

[Test()]
class C1;

[Test(1)]
class C2;

[Test(2, 3)]
class C3;

class Test : System.Attribute
{
    public Test(params MyCollection a) {}
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (16,2): error CS0181: Attribute constructor parameter 'a' has type 'MyCollection', which is not a valid attribute parameter type
                // [Test()]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Test").WithArguments("a", "MyCollection").WithLocation(16, 2),
                // (19,2): error CS0181: Attribute constructor parameter 'a' has type 'MyCollection', which is not a valid attribute parameter type
                // [Test(1)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Test").WithArguments("a", "MyCollection").WithLocation(19, 2),
                // (22,2): error CS0181: Attribute constructor parameter 'a' has type 'MyCollection', which is not a valid attribute parameter type
                // [Test(2, 3)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Test").WithArguments("a", "MyCollection").WithLocation(22, 2)
                );

            assertAttributeData("C1");
            assertAttributeData("C2");
            assertAttributeData("C3");

            var tree = comp.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(3, nodes.Length);

            var model = comp.GetSemanticModel(tree);

            foreach (LiteralExpressionSyntax expression in nodes)
            {
                assertTypeInfo(expression);
            }

            void assertTypeInfo(LiteralExpressionSyntax expression)
            {
                var typeInfo = model.GetTypeInfo(expression);
                Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
                Assert.Equal("System.Int64", typeInfo.ConvertedType.ToTestDisplayString());

                Assert.True(model.GetConversion(expression).IsNumeric);
            }

            void assertAttributeData(string name)
            {
                var attributeData1 = comp.GetTypeByMetadataName(name).GetAttributes().Single();
                Assert.True(attributeData1.HasErrors);

                var c1Arg = attributeData1.ConstructorArguments.Single();
                Assert.Equal(TypedConstantKind.Error, c1Arg.Kind);
                Assert.Equal("MyCollection", c1Arg.Type.ToTestDisplayString());
                Assert.Null(c1Arg.Value);
                Assert.Throws<System.InvalidOperationException>(() => c1Arg.Values);
            }
        }

        [Fact]
        public void ImplementsIEnumerableT_01()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<long>
{
    public List<long> Array = new List<long>();
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(long l) => Array.Add(l);
}

class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    static void Test(params MyCollection a)
    {
        if (a.Array.Count == 0)
        {
            System.Console.WriteLine(a.Array.Count);
        }
        else
        {
            System.Console.WriteLine("{0}: {1} ... {2}", a.Array.Count, a.Array[0], a.Array[a.Array.Count - 1]);
        }
    }

    static void Test2()
    {
        Test([2, 3]);
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped,
                expectedOutput: @"
0
1: 1 ... 1
2: 2 ... 3
").VerifyDiagnostics();
        }

        [Fact]
        public void ImplementsIEnumerableT_02()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<long>
{
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public IEnumerator<string> GetEnumerator() => throw null; 

    public void Add(long l) => throw null; 
    public void Add(string l) => throw null; 
}

class Program
{
    static void Main()
    {
        Test("2", 3);
        Test(["2", 3]);
        Test("2");
        Test(["2"]);
        Test(3);
        Test([3]);

        MyCollection x1 = ["2"];
        MyCollection x2 = [3];
    }

    static void Test(params MyCollection a)
    {
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            // PROTOTYPE(ParamsCollections): inconsistencies in compiler's behavior between expanded form and explicit collection expressions are concerning.
            comp.VerifyDiagnostics(
                // (19,19): error CS1503: Argument 2: cannot convert from 'int' to 'string'
                //         Test("2", 3);
                Diagnostic(ErrorCode.ERR_BadArgType, "3").WithArguments("2", "int", "string").WithLocation(19, 19),
                // (20,14): error CS1503: Argument 1: cannot convert from 'collection expressions' to 'string'
                //         Test(["2", 3]);
                Diagnostic(ErrorCode.ERR_BadArgType, @"[""2"", 3]").WithArguments("1", "collection expressions", "string").WithLocation(20, 14),

                // PROTOTYPE(ParamsCollections): The next error looks unexpected
                // (21,14): error CS0029: Cannot implicitly convert type 'string' to 'long'
                //         Test("2");
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""2""").WithArguments("string", "long").WithLocation(21, 14),

                // (22,14): error CS1503: Argument 1: cannot convert from 'collection expressions' to 'string'
                //         Test(["2"]);
                Diagnostic(ErrorCode.ERR_BadArgType, @"[""2""]").WithArguments("1", "collection expressions", "string").WithLocation(22, 14),
                // (23,14): error CS1503: Argument 1: cannot convert from 'int' to 'string'
                //         Test(3);
                Diagnostic(ErrorCode.ERR_BadArgType, "3").WithArguments("1", "int", "string").WithLocation(23, 14),
                // (26,28): error CS0029: Cannot implicitly convert type 'string' to 'long'
                //         MyCollection x1 = ["2"];
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""2""").WithArguments("string", "long").WithLocation(26, 28)
                );
        }

        [Theory]
        [CombinatorialData]
        public void ImplementsIEnumerableT_03_InAttribute(bool asStruct)
        {
            var src = @"
using System;
using System.Collections;
using System.Collections.Generic;

" + (asStruct ? "struct" : "class") + @" MyCollection : IEnumerable<long>
{
    IEnumerator<long> IEnumerable<long>.GetEnumerator() => throw new InvalidOperationException();
    IEnumerator IEnumerable.GetEnumerator() => throw new InvalidOperationException();

    public void Add(long l) {}
}

[Test()]
class C1;

[Test(1)]
class C2;

[Test(2, 3)]
class C3;

class Test : System.Attribute
{
    public Test(params MyCollection a) {}
}
";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (14,2): error CS0181: Attribute constructor parameter 'a' has type 'MyCollection', which is not a valid attribute parameter type
                // [Test()]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Test").WithArguments("a", "MyCollection").WithLocation(14, 2),
                // (17,2): error CS0181: Attribute constructor parameter 'a' has type 'MyCollection', which is not a valid attribute parameter type
                // [Test(1)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Test").WithArguments("a", "MyCollection").WithLocation(17, 2),
                // (20,2): error CS0181: Attribute constructor parameter 'a' has type 'MyCollection', which is not a valid attribute parameter type
                // [Test(2, 3)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Test").WithArguments("a", "MyCollection").WithLocation(20, 2)
                );

            assertAttributeData("C1");
            assertAttributeData("C2");
            assertAttributeData("C3");

            var tree = comp.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(3, nodes.Length);

            var model = comp.GetSemanticModel(tree);

            foreach (LiteralExpressionSyntax expression in nodes)
            {
                assertTypeInfo(expression);
            }

            void assertTypeInfo(LiteralExpressionSyntax expression)
            {
                var typeInfo = model.GetTypeInfo(expression);
                Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
                Assert.Equal("System.Int64", typeInfo.ConvertedType.ToTestDisplayString());

                Assert.True(model.GetConversion(expression).IsNumeric);
            }

            void assertAttributeData(string name)
            {
                var attributeData1 = comp.GetTypeByMetadataName(name).GetAttributes().Single();
                Assert.True(attributeData1.HasErrors);

                var c1Arg = attributeData1.ConstructorArguments.Single();
                Assert.Equal(TypedConstantKind.Error, c1Arg.Kind);
                Assert.Equal("MyCollection", c1Arg.Type.ToTestDisplayString());
                Assert.Null(c1Arg.Value);
                Assert.Throws<System.InvalidOperationException>(() => c1Arg.Values);
            }
        }

        [Fact]
        public void ImplementsIEnumerable_01()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable
{
    public List<object> Array = new List<object>();
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(object l) => Array.Add(l);
}

class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    static void Test(params MyCollection a)
    {
        if (a.Array.Count == 0)
        {
            System.Console.WriteLine(a.Array.Count);
        }
        else
        {
            System.Console.WriteLine("{0}: {1} ... {2}", a.Array.Count, a.Array[0], a.Array[a.Array.Count - 1]);
        }
    }

    static void Test2()
    {
        Test([2, 3]);
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"
0
1: 1 ... 1
2: 2 ... 3
").VerifyDiagnostics();
        }

        [Fact]
        public void ImplementsIEnumerable_02()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public IEnumerator<string> GetEnumerator() => throw null; 
    public void Add(object l) => throw null;
}

class Program
{
    static void Main()
    {
        Test("2", 3);
        Test(["2", 3]);
    }

    static void Test(params MyCollection a)
    {
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            // PROTOTYPE(ParamsCollections): inconsistencies in compiler's behavior between expanded form and explicit collection expressions are concerning.
            comp.VerifyDiagnostics(
                // (16,19): error CS1503: Argument 2: cannot convert from 'int' to 'string'
                //         Test("2", 3);
                Diagnostic(ErrorCode.ERR_BadArgType, "3").WithArguments("2", "int", "string").WithLocation(16, 19)
                );
        }

        [Theory]
        [CombinatorialData]
        public void ImplementsIEnumerable_03_InAttribute(bool asStruct)
        {
            var src = @"
using System;
using System.Collections;

" + (asStruct ? "struct" : "class") + @" MyCollection : IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator() => throw new InvalidOperationException();

    public void Add(object l) {}
}

[Test()]
class C1;

[Test(1)]
class C2;

[Test(2, 3)]
class C3;

class Test : System.Attribute
{
    public Test(params MyCollection a) {}
}
";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (12,2): error CS0181: Attribute constructor parameter 'a' has type 'MyCollection', which is not a valid attribute parameter type
                // [Test()]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Test").WithArguments("a", "MyCollection").WithLocation(12, 2),
                // (15,2): error CS0181: Attribute constructor parameter 'a' has type 'MyCollection', which is not a valid attribute parameter type
                // [Test(1)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Test").WithArguments("a", "MyCollection").WithLocation(15, 2),
                // (18,2): error CS0181: Attribute constructor parameter 'a' has type 'MyCollection', which is not a valid attribute parameter type
                // [Test(2, 3)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Test").WithArguments("a", "MyCollection").WithLocation(18, 2)
                );

            assertAttributeData("C1");
            assertAttributeData("C2");
            assertAttributeData("C3");

            var tree = comp.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(3, nodes.Length);

            var model = comp.GetSemanticModel(tree);

            foreach (LiteralExpressionSyntax expression in nodes)
            {
                assertTypeInfo(expression);
            }

            void assertTypeInfo(LiteralExpressionSyntax expression)
            {
                var typeInfo = model.GetTypeInfo(expression);
                Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
                Assert.Equal("System.Object", typeInfo.ConvertedType.ToTestDisplayString());

                Assert.True(model.GetConversion(expression).IsBoxing);
            }

            void assertAttributeData(string name)
            {
                var attributeData1 = comp.GetTypeByMetadataName(name).GetAttributes().Single();
                Assert.True(attributeData1.HasErrors);

                var c1Arg = attributeData1.ConstructorArguments.Single();
                Assert.Equal(TypedConstantKind.Error, c1Arg.Kind);
                Assert.Equal("MyCollection", c1Arg.Type.ToTestDisplayString());
                Assert.Null(c1Arg.Value);
                Assert.Throws<System.InvalidOperationException>(() => c1Arg.Values);
            }
        }

        [Theory]
        [InlineData("IEnumerable<long>")]
        [InlineData("IReadOnlyCollection<long>")]
        [InlineData("IReadOnlyList<long>")]
        [InlineData("ICollection<long>")]
        [InlineData("IList<long>")]
        public void ArrayInterfaces(string @interface)
        {
            var src = """
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main()
    {
        Test();
        Test(1);
        Test(2, 3);
    }

    static void Test(params 
""" +
                            @interface +
"""
                                         a)
    {
        var array = a.ToArray();
        if (array.Length == 0)
        {
            System.Console.WriteLine(array.Length);
        }
        else
        {
            System.Console.WriteLine("{0}: {1} ... {2}", array.Length, array[0], array[^1]);
        }
    }

    static void Test2()
    {
        Test([2, 3]);
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped,
                expectedOutput: ExpectedOutput(@"
0
1: 1 ... 1
2: 2 ... 3
")).VerifyDiagnostics();
        }

        [Theory]
        [InlineData("IEnumerable")]
        [InlineData("IReadOnlyCollection")]
        [InlineData("IReadOnlyList")]
        [InlineData("ICollection")]
        [InlineData("IList")]
        public void ArrayInterfaces_InAttribute(string @interface)
        {
            var src = @"
using System.Collections.Generic;

[Test()]
class C1;

[Test(1)]
class C2;

[Test(2, 3)]
class C3;

class Test : System.Attribute
{
    public Test(params " + @interface + @"<long> a) {}
}
";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (4,2): error CS0181: Attribute constructor parameter 'a' has type 'ICollection<long>', which is not a valid attribute parameter type
                // [Test()]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Test").WithArguments("a", "System.Collections.Generic." + @interface + "<long>").WithLocation(4, 2),
                // (7,2): error CS0181: Attribute constructor parameter 'a' has type 'ICollection<long>', which is not a valid attribute parameter type
                // [Test(1)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Test").WithArguments("a", "System.Collections.Generic." + @interface + "<long>").WithLocation(7, 2),
                // (10,2): error CS0181: Attribute constructor parameter 'a' has type 'ICollection<long>', which is not a valid attribute parameter type
                // [Test(2, 3)]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Test").WithArguments("a", "System.Collections.Generic." + @interface + "<long>").WithLocation(10, 2)
                );

            assertAttributeData("C1");
            assertAttributeData("C2");
            assertAttributeData("C3");

            var tree = comp.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(3, nodes.Length);

            var model = comp.GetSemanticModel(tree);

            foreach (LiteralExpressionSyntax expression in nodes)
            {
                assertTypeInfo(expression);
            }

            void assertTypeInfo(LiteralExpressionSyntax expression)
            {
                var typeInfo = model.GetTypeInfo(expression);
                Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
                Assert.Equal("System.Int64", typeInfo.ConvertedType.ToTestDisplayString());

                Assert.True(model.GetConversion(expression).IsNumeric);
            }

            void assertAttributeData(string name)
            {
                var attributeData1 = comp.GetTypeByMetadataName(name).GetAttributes().Single();
                Assert.True(attributeData1.HasErrors);

                var c1Arg = attributeData1.ConstructorArguments.Single();
                Assert.Equal(TypedConstantKind.Error, c1Arg.Kind);
                Assert.Equal("System.Collections.Generic." + @interface + "<System.Int64>", c1Arg.Type.ToTestDisplayString());
                Assert.Null(c1Arg.Value);
                Assert.Throws<System.InvalidOperationException>(() => c1Arg.Values);
            }
        }

        [Fact]
        public void IEnumerable()
        {
            var src = """
using System.Collections;

class Program
{
    static void Test(params IEnumerable a)
    {
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (5,22): error CS0225: The params parameter must have a valid collection type
                //     static void Test(params IEnumerable a)
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(5, 22)
                );
        }

        [Fact]
        public void WRN_ParamsArrayInLambdaOnly()
        {
            var src = """
using System.Collections.Generic;

System.Action<IEnumerable<long>> l = (params IEnumerable<long> x) => {};
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (3,64): warning CS9100: Parameter 1 has params modifier in lambda but not in target delegate type.
                // System.Action<IEnumerable<long>> l = (params IEnumerable<long> x) => {};
                Diagnostic(ErrorCode.WRN_ParamsArrayInLambdaOnly, "x").WithArguments("1").WithLocation(3, 64)
                );
        }

        [Theory]
        [InlineData(@"$""Literal{1}""")]
        [InlineData(@"$""Literal"" + $""{1}""")]
        public void ConversionInParamsArguments_InterpolatedStringHandler(string expression)
        {
            var code = @"
using System;
using System.Linq;

M(" + expression + ", " + expression + @");

void M(params System.ReadOnlySpan<CustomHandler> handlers)
{
    Console.WriteLine(string.Join(Environment.NewLine, handlers.ToArray().Select(h => h.ToString())));
}
";

            var verifier = CompileAndVerify(new[] { code, GetInterpolatedStringCustomHandlerType("CustomHandler", "struct", useBoolReturns: false, includeOneTimeHelpers: false) }, targetFramework: TargetFramework.Net80,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ?
                            Verification.FailsILVerify with { ILVerifyMessage = "[InlineArrayAsReadOnlySpan]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0x11 }" }
                            : Verification.Skipped,
                expectedOutput: ExpectedOutput(@"
literal:Literal
value:1
alignment:0
format:

literal:Literal
value:1
alignment:0
format:
"));

            verifier.VerifyDiagnostics();
            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size      122 (0x7a)
  .maxstack  5
  .locals init (<>y__InlineArray2<CustomHandler> V_0,
                CustomHandler V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""<>y__InlineArray2<CustomHandler>""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.0
  IL_000b:  call       ""ref CustomHandler <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray2<CustomHandler>, CustomHandler>(ref <>y__InlineArray2<CustomHandler>, int)""
  IL_0010:  ldloca.s   V_1
  IL_0012:  ldc.i4.7
  IL_0013:  ldc.i4.1
  IL_0014:  call       ""CustomHandler..ctor(int, int)""
  IL_0019:  ldloca.s   V_1
  IL_001b:  ldstr      ""Literal""
  IL_0020:  call       ""void CustomHandler.AppendLiteral(string)""
  IL_0025:  ldloca.s   V_1
  IL_0027:  ldc.i4.1
  IL_0028:  box        ""int""
  IL_002d:  ldc.i4.0
  IL_002e:  ldnull
  IL_002f:  call       ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_0034:  ldloc.1
  IL_0035:  stobj      ""CustomHandler""
  IL_003a:  ldloca.s   V_0
  IL_003c:  ldc.i4.1
  IL_003d:  call       ""ref CustomHandler <PrivateImplementationDetails>.InlineArrayElementRef<<>y__InlineArray2<CustomHandler>, CustomHandler>(ref <>y__InlineArray2<CustomHandler>, int)""
  IL_0042:  ldloca.s   V_1
  IL_0044:  ldc.i4.7
  IL_0045:  ldc.i4.1
  IL_0046:  call       ""CustomHandler..ctor(int, int)""
  IL_004b:  ldloca.s   V_1
  IL_004d:  ldstr      ""Literal""
  IL_0052:  call       ""void CustomHandler.AppendLiteral(string)""
  IL_0057:  ldloca.s   V_1
  IL_0059:  ldc.i4.1
  IL_005a:  box        ""int""
  IL_005f:  ldc.i4.0
  IL_0060:  ldnull
  IL_0061:  call       ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_0066:  ldloc.1
  IL_0067:  stobj      ""CustomHandler""
  IL_006c:  ldloca.s   V_0
  IL_006e:  ldc.i4.2
  IL_006f:  call       ""System.ReadOnlySpan<CustomHandler> <PrivateImplementationDetails>.InlineArrayAsReadOnlySpan<<>y__InlineArray2<CustomHandler>, CustomHandler>(in <>y__InlineArray2<CustomHandler>, int)""
  IL_0074:  call       ""void Program.<<Main>$>g__M|0_0(System.ReadOnlySpan<CustomHandler>)""
  IL_0079:  ret
}
");
        }

        [Fact]
        public void OrderOfEvaluation_01_NamedArguments()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<int>
{
    public MyCollection()
    {
        System.Console.WriteLine("Create");
    }

    IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(int l)
    {
        System.Console.WriteLine("Add");
    }
}

class Program
{
    static void Main()
    {
        Test(b: GetB(), c: GetC(), a: GetA());
    }

    static void Test(int a, int b, params MyCollection c)
    {
    }

    static int GetA()
    {
        System.Console.WriteLine("GetA");
        return 0;
    }

    static int GetB()
    {
        System.Console.WriteLine("GetB");
        return 0;
    }

    static int GetC()
    {
        System.Console.WriteLine("GetC");
        return 0;
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(
                comp,
                expectedOutput: @"
GetB
Create
GetC
Add
GetA
").VerifyDiagnostics();

            // Note, the collection is created after the lexically previous argument is evaluated, 
            // but before the lexically following argument is evaluated. This differs from params
            // array case, which is created right before the target methos is invoked, after all
            // arguments are evaluated in their lexical order, which can be observed in a unit-test
            // Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen.CodeGenTests.NamedParamsOptimizationAndParams002​
            verifier.VerifyIL("Program.Main", @"
{
  // Code size       36 (0x24)
  .maxstack  3
  .locals init (int V_0,
            MyCollection V_1)
  IL_0000:  call       ""int Program.GetB()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""MyCollection..ctor()""
  IL_000b:  dup
  IL_000c:  call       ""int Program.GetC()""
  IL_0011:  callvirt   ""void MyCollection.Add(int)""
  IL_0016:  stloc.1
  IL_0017:  call       ""int Program.GetA()""
  IL_001c:  ldloc.0
  IL_001d:  ldloc.1
  IL_001e:  call       ""void Program.Test(int, int, params MyCollection)""
  IL_0023:  ret
}
");
        }

        [Fact]
        public void OrderOfEvaluation_02_CompoundAssignment()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<int>
{
    public MyCollection()
    {
        System.Console.WriteLine("Create");
    }

    IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(int l)
    {
        System.Console.WriteLine("Add");
    }
}

class Program
{
    private MyCollection _c;

    static void Main()
    {
        System.Console.WriteLine("---Test1");
        Test1(new Program());
        System.Console.WriteLine("---Test2");
        Test2(new Program());
        System.Console.WriteLine("---Test3");
        Test3(new Program());
    }

    static void Test1(Program p)
    {
        p[GetA()]++;
    }

    static void Test2(Program p)
    {
        p[GetA(), GetC()]++;
    }

    static void Test3(Program p)
    {
        p[GetA(), GetB(), GetC()]++;
    }

    int this[int a, params MyCollection c]
    {
        get
        {
            System.Console.WriteLine("Get_this {0}", c is not null);
            _c = c;
            return 0;
        }
        set
        {
            System.Console.WriteLine("Set_this {0}", (object)_c == c);
        }
    }


    static int GetA()
    {
        System.Console.WriteLine("GetA");
        return 0;
    }

    static int GetB()
    {
        System.Console.WriteLine("GetB");
        return 0;
    }

    static int GetC()
    {
        System.Console.WriteLine("GetC");
        return 0;
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(
                comp,
                expectedOutput: @"
---Test1
GetA
Create
Get_this True
Set_this True
---Test2
GetA
Create
GetC
Add
Get_this True
Set_this True
---Test3
GetA
Create
GetB
Add
GetC
Add
Get_this True
Set_this True
").VerifyDiagnostics();

            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       33 (0x21)
  .maxstack  5
  .locals init (int V_0,
            MyCollection V_1,
            int V_2)
  IL_0000:  ldarg.0
  IL_0001:  call       ""int Program.GetA()""
  IL_0006:  stloc.0
  IL_0007:  newobj     ""MyCollection..ctor()""
  IL_000c:  stloc.1
  IL_000d:  dup
  IL_000e:  ldloc.0
  IL_000f:  ldloc.1
  IL_0010:  callvirt   ""int Program.this[int, params MyCollection].get""
  IL_0015:  stloc.2
  IL_0016:  ldloc.0
  IL_0017:  ldloc.1
  IL_0018:  ldloc.2
  IL_0019:  ldc.i4.1
  IL_001a:  add
  IL_001b:  callvirt   ""void Program.this[int, params MyCollection].set""
  IL_0020:  ret

}
");

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size       44 (0x2c)
  .maxstack  5
  .locals init (int V_0,
            MyCollection V_1,
            int V_2)
  IL_0000:  ldarg.0
  IL_0001:  call       ""int Program.GetA()""
  IL_0006:  stloc.0
  IL_0007:  newobj     ""MyCollection..ctor()""
  IL_000c:  dup
  IL_000d:  call       ""int Program.GetC()""
  IL_0012:  callvirt   ""void MyCollection.Add(int)""
  IL_0017:  stloc.1
  IL_0018:  dup
  IL_0019:  ldloc.0
  IL_001a:  ldloc.1
  IL_001b:  callvirt   ""int Program.this[int, params MyCollection].get""
  IL_0020:  stloc.2
  IL_0021:  ldloc.0
  IL_0022:  ldloc.1
  IL_0023:  ldloc.2
  IL_0024:  ldc.i4.1
  IL_0025:  add
  IL_0026:  callvirt   ""void Program.this[int, params MyCollection].set""
  IL_002b:  ret
}
");

            verifier.VerifyIL("Program.Test3", @"
{
  // Code size       55 (0x37)
  .maxstack  5
  .locals init (int V_0,
            MyCollection V_1,
            int V_2)
  IL_0000:  ldarg.0
  IL_0001:  call       ""int Program.GetA()""
  IL_0006:  stloc.0
  IL_0007:  newobj     ""MyCollection..ctor()""
  IL_000c:  dup
  IL_000d:  call       ""int Program.GetB()""
  IL_0012:  callvirt   ""void MyCollection.Add(int)""
  IL_0017:  dup
  IL_0018:  call       ""int Program.GetC()""
  IL_001d:  callvirt   ""void MyCollection.Add(int)""
  IL_0022:  stloc.1
  IL_0023:  dup
  IL_0024:  ldloc.0
  IL_0025:  ldloc.1
  IL_0026:  callvirt   ""int Program.this[int, params MyCollection].get""
  IL_002b:  stloc.2
  IL_002c:  ldloc.0
  IL_002d:  ldloc.1
  IL_002e:  ldloc.2
  IL_002f:  ldc.i4.1
  IL_0030:  add
  IL_0031:  callvirt   ""void Program.this[int, params MyCollection].set""
  IL_0036:  ret
}
");
        }

        [Fact]
        public void OrderOfEvaluation_03_ObjectInitializer()
        {
            var src = """
using System.Collections;
using System.Collections.Generic;

class MyCollection : IEnumerable<int>
{
    public MyCollection()
    {
        System.Console.WriteLine("Create");
    }

    IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;

    public void Add(int l)
    {
        System.Console.WriteLine("Add");
    }
}

class C1
{
    public int F1;
    public int F2;
}

class Program
{
    private MyCollection _c;

    static void Main()
    {
        System.Console.WriteLine("---Test1");
        Test1();
        System.Console.WriteLine("---Test2");
        Test2();
        System.Console.WriteLine("---Test3");
        Test3();
    }

    static void Test1()
    {
        _ = new Program() { [GetA()] = { F1 = GetF1(), F2 = GetF2() } };
    }

    static void Test2()
    {
        _ = new Program() { [GetA(), GetC()] = { F1 = GetF1(), F2 = GetF2() } };
    }

    static void Test3()
    {
        _ = new Program() { [GetA(), GetB(), GetC()] = { F1 = GetF1(), F2 = GetF2() } };
    }

    C1 this[int a, params MyCollection c]
    {
        get
        {
            System.Console.WriteLine("Get_this {0}", c is not null && (_c is null || (object)_c == c));
            _c = c;
            return new C1();
        }
        set
        {
            System.Console.WriteLine("Set_this {0}", (object)_c == c);
        }
    }


    static int GetA()
    {
        System.Console.WriteLine("GetA");
        return 0;
    }

    static int GetB()
    {
        System.Console.WriteLine("GetB");
        return 0;
    }

    static int GetC()
    {
        System.Console.WriteLine("GetC");
        return 0;
    }

    static int GetF1()
    {
        System.Console.WriteLine("GetF1");
        return 0;
    }

    static int GetF2()
    {
        System.Console.WriteLine("GetF2");
        return 0;
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(
                comp,
                expectedOutput: @"
---Test1
GetA
Create
Get_this True
GetF1
Get_this True
GetF2
---Test2
GetA
Create
GetC
Add
Get_this True
GetF1
Get_this True
GetF2
---Test3
GetA
Create
GetB
Add
GetC
Add
Get_this True
GetF1
Get_this True
GetF2
").VerifyDiagnostics();

            // Note, the collection is created once and that same instance is used across multiple invocation of the indexer.
            // With params arrays, however, only individual elements are cached and each invocation of the indexer is getting
            // a new instance of an array (with the same values inside though). This can be observed in a unit-test
            // Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen.ObjectAndCollectionInitializerTests.DictionaryInitializerTestSideeffects001param
            verifier.VerifyIL("Program.Test1", @"
{
  // Code size       53 (0x35)
  .maxstack  4
  .locals init (int V_0,
            MyCollection V_1)
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  call       ""int Program.GetA()""
  IL_000a:  stloc.0
  IL_000b:  newobj     ""MyCollection..ctor()""
  IL_0010:  stloc.1
  IL_0011:  dup
  IL_0012:  ldloc.0
  IL_0013:  ldloc.1
  IL_0014:  callvirt   ""C1 Program.this[int, params MyCollection].get""
  IL_0019:  call       ""int Program.GetF1()""
  IL_001e:  stfld      ""int C1.F1""
  IL_0023:  ldloc.0
  IL_0024:  ldloc.1
  IL_0025:  callvirt   ""C1 Program.this[int, params MyCollection].get""
  IL_002a:  call       ""int Program.GetF2()""
  IL_002f:  stfld      ""int C1.F2""
  IL_0034:  ret
}
");

            verifier.VerifyIL("Program.Test2", @"
{
  // Code size       64 (0x40)
  .maxstack  4
  .locals init (int V_0,
            MyCollection V_1)
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  call       ""int Program.GetA()""
  IL_000a:  stloc.0
  IL_000b:  newobj     ""MyCollection..ctor()""
  IL_0010:  dup
  IL_0011:  call       ""int Program.GetC()""
  IL_0016:  callvirt   ""void MyCollection.Add(int)""
  IL_001b:  stloc.1
  IL_001c:  dup
  IL_001d:  ldloc.0
  IL_001e:  ldloc.1
  IL_001f:  callvirt   ""C1 Program.this[int, params MyCollection].get""
  IL_0024:  call       ""int Program.GetF1()""
  IL_0029:  stfld      ""int C1.F1""
  IL_002e:  ldloc.0
  IL_002f:  ldloc.1
  IL_0030:  callvirt   ""C1 Program.this[int, params MyCollection].get""
  IL_0035:  call       ""int Program.GetF2()""
  IL_003a:  stfld      ""int C1.F2""
  IL_003f:  ret
}
");

            verifier.VerifyIL("Program.Test3", @"
{
  // Code size       75 (0x4b)
  .maxstack  4
  .locals init (int V_0,
            MyCollection V_1)
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  call       ""int Program.GetA()""
  IL_000a:  stloc.0
  IL_000b:  newobj     ""MyCollection..ctor()""
  IL_0010:  dup
  IL_0011:  call       ""int Program.GetB()""
  IL_0016:  callvirt   ""void MyCollection.Add(int)""
  IL_001b:  dup
  IL_001c:  call       ""int Program.GetC()""
  IL_0021:  callvirt   ""void MyCollection.Add(int)""
  IL_0026:  stloc.1
  IL_0027:  dup
  IL_0028:  ldloc.0
  IL_0029:  ldloc.1
  IL_002a:  callvirt   ""C1 Program.this[int, params MyCollection].get""
  IL_002f:  call       ""int Program.GetF1()""
  IL_0034:  stfld      ""int C1.F1""
  IL_0039:  ldloc.0
  IL_003a:  ldloc.1
  IL_003b:  callvirt   ""C1 Program.this[int, params MyCollection].get""
  IL_0040:  call       ""int Program.GetF2()""
  IL_0045:  stfld      ""int C1.F2""
  IL_004a:  ret
}
");
        }

        [Fact]
        public void LanguageVersion_01_Declaration()
        {
            var src = @"
class Program
{
    static void Test1(params System.ReadOnlySpan<long> a) {}
    static void Test2(params long[] a) {}

    void Test()
    {
        Test1(1);
        Test2(2);
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularNext);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (4,23): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static void Test1(params System.ReadOnlySpan<long> a) {}
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "params System.ReadOnlySpan<long> a").WithArguments("params collections").WithLocation(4, 23)
                );
        }

        [Fact]
        public void LanguageVersion_02_CallSite()
        {
            var src1 = @"
public class Params
{
    static public void Test1(params System.ReadOnlySpan<long> a) {}
    static public void Test2(params long[] a) {}
}
";
            var src2 = @"
class Program
{
    void Test()
    {
        Params.Test1(1);
        Params.Test2(2);

        Params.Test1();
        Params.Test2();

        Params.Test1([1]);
        Params.Test2([2]);
    }
}
";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            verify(comp1.ToMetadataReference());
            verify(comp1.EmitToImageReference());

            void verify(MetadataReference comp1Ref)
            {
                var comp2 = CreateCompilation(src2, references: [comp1Ref], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
                comp2.VerifyDiagnostics();

                comp2 = CreateCompilation(src2, references: [comp1Ref], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularNext);
                comp2.VerifyDiagnostics();

                comp2 = CreateCompilation(src2, references: [comp1Ref], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular12);
                comp2.VerifyDiagnostics(
                    // (6,9): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         Params.Test1(1);
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "Params.Test1(1)").WithArguments("params collections").WithLocation(6, 9),
                    // (9,9): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         Params.Test1();
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "Params.Test1()").WithArguments("params collections").WithLocation(9, 9)
                    );
            }
        }

        [Fact]
        public void LanguageVersion_03_DelegateNaturalType()
        {
            var src1 = @"
public class Params
{
    static public void Test1(params System.ReadOnlySpan<long> a) {}
    static public void Test2(params long[] a) {}
}
";
            var src2 = @"
class Program
{
    void Test()
    {
        var x1 = Params.Test1;
        var x2 = Params.Test2;

        x1(1);
        x2(2);

        x1();
        x2();

        x1([1]);
        x2([2]);
    }
}
";
            var comp = CreateCompilation(src2 + src1, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src2 + src1, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularNext);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src2 + src1, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (22,30): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static public void Test1(params System.ReadOnlySpan<long> a) {}
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "params System.ReadOnlySpan<long> a").WithArguments("params collections").WithLocation(22, 30)
                );

            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll);

            verify(comp1.ToMetadataReference());
            verify(comp1.EmitToImageReference());

            void verify(MetadataReference comp1Ref)
            {
                var comp2 = CreateCompilation(src2, references: [comp1Ref], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
                comp2.VerifyDiagnostics();

                comp2 = CreateCompilation(src2, references: [comp1Ref], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularNext);
                comp2.VerifyDiagnostics();

                comp2 = CreateCompilation(src2, references: [comp1Ref], targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular12);
                comp2.VerifyDiagnostics(
                    // (6,18): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         var x1 = Params.Test1;
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "Params.Test1").WithArguments("params collections").WithLocation(6, 18)
                    );
            }
        }

        [Fact]
        public void LanguageVersion_04_DelegateNaturalType()
        {
            var src = @"
class Program
{
    void Test()
    {
        var x1 = (params System.ReadOnlySpan<long> a) => {};
        var x2 = (params long[] a) => {};

        x1(1);
        x2(2);

        x1();
        x2();

        x1([1]);
        x2([2]);

        M1(x1);
        M1(x2);

        M1((params System.ReadOnlySpan<long> b) => {});
        M1((params long[] b) => {});
    }

    static void M1<T>(T t) {}
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularNext);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (6,19): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var x1 = (params System.ReadOnlySpan<long> a) => {};
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "params System.ReadOnlySpan<long> a").WithArguments("params collections").WithLocation(6, 19),
                // (21,13): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         M1((params System.ReadOnlySpan<long> b) => {});
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "params System.ReadOnlySpan<long> b").WithArguments("params collections").WithLocation(21, 13)
                );
        }

        [Fact]
        public void LanguageVersion_05_DelegateNaturalType()
        {
            var src1 = @"
public class Params
{
    static public void Test1(params System.Collections.Generic.IEnumerable<long> a) {}
    static public void Test2(params long[] a) {}
}
";
            var src2 = @"
class Program
{
    void Test1()
    {
        var a = Params.Test1;
        M1(a); // See DelegateNaturalType_03 unit-test for an observable effect that 'params' modifier has for this invocation. 
        M1(Params.Test1);
    }

    static void M1<T>(T t) {}

    void Test2()
    {
        var b = Params.Test2;
        M1(b);
        M1(Params.Test2);
    }
}
";
            var comp = CreateCompilation(src2 + src1, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src2 + src1, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularNext);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src2 + src1, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (23,30): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     static public void Test1(params System.Collections.Generic.IEnumerable<long> a) {}
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "params System.Collections.Generic.IEnumerable<long> a").WithArguments("params collections").WithLocation(23, 30)
                );

            var comp1 = CreateCompilation(src1, options: TestOptions.ReleaseDll);

            verify(comp1.ToMetadataReference());
            verify(comp1.EmitToImageReference());

            void verify(MetadataReference comp1Ref)
            {
                var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
                comp2.VerifyDiagnostics();

                comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularNext);
                comp2.VerifyDiagnostics();

                comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular12);
                comp2.VerifyDiagnostics(
                    // (6,17): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         var a = Params.Test1;
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "Params.Test1").WithArguments("params collections").WithLocation(6, 17),
                    // (8,12): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         M1(Params.Test1);
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "Params.Test1").WithArguments("params collections").WithLocation(8, 12)
                    );
            }
        }

        [Fact]
        public void LanguageVersion_06_LambdaForDelegateWithParams()
        {
            var src1 = @"
public class Params
{
    static public void Test1(D1 d) {}
    static public void Test2(D2 d) {}
}

public delegate void D1(params System.Collections.Generic.IEnumerable<long> a);
public delegate void D2(params long[] a);
";
            var src2 = @"
class Program1
{
    void Test1()
    {
        Params.Test1(e1 => { });
    }
}
class Program2
{
    void Test2()
    {
        Params.Test2(e2 => { });
    }
}
";
            var comp = CreateCompilation(src2 + src1, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src2 + src1, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularNext);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src2 + src1, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics(
                // (23,25): error CS8652: The feature 'params collections' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // public delegate void D1(params System.Collections.Generic.IEnumerable<long> a);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "params System.Collections.Generic.IEnumerable<long> a").WithArguments("params collections").WithLocation(23, 25)
                );

            var comp1 = CreateCompilation(src1, options: TestOptions.ReleaseDll);

            verify(comp1.ToMetadataReference());
            verify(comp1.EmitToImageReference());

            void verify(MetadataReference comp1Ref)
            {
                var comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularPreview);
                comp2.VerifyDiagnostics();

                comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularNext);
                comp2.VerifyDiagnostics();

                comp2 = CreateCompilation(src2, references: [comp1Ref], options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), parseOptions: TestOptions.Regular12);

                var tree = comp2.SyntaxTrees.Single();
                var model = comp2.GetSemanticModel(tree);

                var parameter = (IParameterSymbol)model.GetDeclaredSymbol(tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().First());
                AssertEx.Equal("System.Collections.Generic.IEnumerable<System.Int64> e1", parameter.ToTestDisplayString());
                Assert.False(parameter.IsParams);

                CompileAndVerify(comp2,
                    symbolValidator: (m) =>
                    {
                        var lambda = m.GlobalNamespace.GetMember<MethodSymbol>("Program1.<>c.<Test1>b__0_0");
                        ParameterSymbol parameter = lambda.Parameters.Single();

                        Assert.False(parameter.IsParams);
                        WellKnownAttributesTestBase.VerifyParamArrayAttribute(parameter, expected: false);
                        Assert.Equal("System.Collections.Generic.IEnumerable<System.Int64> e1", parameter.ToTestDisplayString());
                    }
                    ).VerifyDiagnostics(); // No language version diagnostics as expected. The 'params' modifier doesn't even make it to symbol and metadata.
            }
        }

        [Fact]
        public void DelegateNaturalType_01()
        {
            var src = @"
public class Params
{
    static public void Test1(params System.ReadOnlySpan<long> a) { System.Console.WriteLine(a.Length); }
    static public void Test2(params long[] a) { System.Console.WriteLine(a.Length); }
}

class Program
{
    static void Main()
    {
        var x1 = Params.Test1;
        var x2 = Params.Test2;

        x1(1);
        x2(2);

        x1();
        x2();
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped,
                expectedOutput: ExpectedOutput(@"
1
1
0
0
")).VerifyDiagnostics();
        }

        [Fact]
        public void DelegateNaturalType_02()
        {
            var src = @"
class Program
{
    static void Main()
    {
        var x1 = (params System.ReadOnlySpan<long> a) => System.Console.WriteLine(a.Length);
        var x2 = (params long[] a) => System.Console.WriteLine(a.Length);

        x1(1);
        x2(2);

        x1();
        x2();
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped,
                expectedOutput: ExpectedOutput(@"
1
1
0
0
")).VerifyDiagnostics();
        }

        [Fact]
        public void DelegateNaturalType_03()
        {
            var src = @"
class Program
{
    static public void Test1(System.Collections.Generic.IEnumerable<long> a) { System.Console.WriteLine("" {0}"", a is not null); }
    static public void Test2(params System.Collections.Generic.IEnumerable<long> a) { System.Console.WriteLine("" {0}"", a is not null); }
    static public void Test3(params System.Collections.Generic.List<long> a) { System.Console.WriteLine("" {0}"", a is not null); }
    static public void Test4(params long[] a) { System.Console.WriteLine("" {0}"", a is not null); }

    static void Main()
    {
        DoTest1();
        DoTest21();
        DoTest22();
        DoTest3();
        DoTest4();
    }

    static void DoTest1()
    {
        var a1 = Test1;
        M(a1);
    }

    static void DoTest21()
    {
        var a2 = Test2;
        M(a2)();
    }

    static void DoTest22()
    {
        var a2 = Test2;
        M(a2)();
    }

    static void DoTest3()
    {
        var a3 = Test3;
        M(a3)();
    }

    static void DoTest4()
    {
        var a4 = Test4;
        M(a4)();
    }

    static T M<T>(T t) { System.Console.WriteLine(typeof(T)); return t; }
    static void M(System.Action<System.Collections.Generic.IEnumerable<long>> t) => System.Console.WriteLine(""Action"");
    static void M(System.Action<System.Collections.Generic.List<long>> t) => System.Console.WriteLine(""Action"");
}
";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(
                comp,
                symbolValidator: (m) =>
                {
                    AssertEx.Equal("void <>f__AnonymousDelegate0.Invoke(params System.Collections.Generic.IEnumerable<System.Int64> arg)", m.ContainingAssembly.GetTypeByMetadataName("<>f__AnonymousDelegate0").DelegateInvokeMethod.ToTestDisplayString());
                    AssertEx.Equal("void <>f__AnonymousDelegate1.Invoke(params System.Collections.Generic.List<System.Int64> arg)", m.ContainingAssembly.GetTypeByMetadataName("<>f__AnonymousDelegate1").DelegateInvokeMethod.ToTestDisplayString());
                    AssertEx.Equal("void <>f__AnonymousDelegate2<T1>.Invoke(params T1[] arg)", m.ContainingAssembly.GetTypeByMetadataName("<>f__AnonymousDelegate2`1").DelegateInvokeMethod.ToTestDisplayString());
                },
                expectedOutput: ExpectedOutput(@"
Action
<>f__AnonymousDelegate0
 True
<>f__AnonymousDelegate0
 True
<>f__AnonymousDelegate1
 True
<>f__AnonymousDelegate2`1[System.Int64]
 True
")).VerifyDiagnostics();
        }

        [Fact]
        public void BetterNess_01_ElementType()
        {
            var src = @"
class Program
{
    static void Main()
    {
        int x = 1;
        Test(x);

        byte y = 1;
        Test(y);
    }

    static void Test(params System.Span<long> a)
    {
        System.Console.WriteLine(""long"");
    }

    static void Test(params System.Span<int> a)
    {
        System.Console.WriteLine(""int"");
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ?
                            Verification.FailsILVerify with { ILVerifyMessage = "[InlineArrayAsSpan]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0xc }" }
                            : Verification.Skipped,
                expectedOutput: ExpectedOutput(@"
int
int")).VerifyDiagnostics();
        }

        [Fact]
        public void BetterNess_02_ElementType()
        {
            var src = @"
class Program
{
    static void Main()
    {
        Test(new C2());

        Test(new C3());
    }

    static void Test(params System.Span<C1> a)
    {
        System.Console.WriteLine(""C1"");
    }

    static void Test(params System.Span<C2> a)
    {
        System.Console.WriteLine(""C2"");
    }
}

class C1 {}
class C2 : C1 {}
class C3 : C2 {}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ?
                            Verification.FailsILVerify with { ILVerifyMessage = "[InlineArrayAsSpan]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0xc }" }
                            : Verification.Skipped,
                expectedOutput: ExpectedOutput(@"
C2
C2")).VerifyDiagnostics();
        }

        [Fact]
        public void BetterNess_03_ElementType()
        {
            var src = @"
using System.Collections;
using System.Collections.Generic;

class C1 : IEnumerable<char>
{
    public static void M1(params C1 x)
    {
    }
    public static void M1(params ushort[] x)
    {
    }

    void Test()
    {
        M1('a', 'b');
        M2('a', 'b');
    }

    public static void M2(params ushort[] x)
    {
    }

    IEnumerator<char> IEnumerable<char>.GetEnumerator() => throw null;
    IEnumerator IEnumerable.GetEnumerator() => throw null;
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);

            comp.VerifyDiagnostics(
                // (16,12): error CS1061: 'C1' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'C1' could be found (are you missing a using directive or an assembly reference?)
                //         M1('a', 'b');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'a'").WithArguments("C1", "Add").WithLocation(16, 12),
                // (16,17): error CS1061: 'C1' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'C1' could be found (are you missing a using directive or an assembly reference?)
                //         M1('a', 'b');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'b'").WithArguments("C1", "Add").WithLocation(16, 17)
                );
        }

        [Theory]
        [InlineData("System.Span<T>", "T[]", "System.Span<System.Int32>")]
        [InlineData("System.Span<T>", "System.Collections.Generic.IEnumerable<T>", "System.Span<System.Int32>")]
        [InlineData("System.Span<T>", "System.Collections.Generic.IReadOnlyCollection<T>", "System.Span<System.Int32>")]
        [InlineData("System.Span<T>", "System.Collections.Generic.IReadOnlyList<T>", "System.Span<System.Int32>")]
        [InlineData("System.Span<T>", "System.Collections.Generic.ICollection<T>", "System.Span<System.Int32>")]
        [InlineData("System.Span<T>", "System.Collections.Generic.IList<T>", "System.Span<System.Int32>")]
        [InlineData("System.ReadOnlySpan<T>", "T[]", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<T>", "System.Collections.Generic.IEnumerable<T>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<T>", "System.Collections.Generic.IReadOnlyCollection<T>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<T>", "System.Collections.Generic.IReadOnlyList<T>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<T>", "System.Collections.Generic.ICollection<T>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<T>", "System.Collections.Generic.IList<T>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.Span<T>", "System.Collections.Generic.HashSet<T>", null)] // rule requires array or array interface

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.Span<T>", "System.ReadOnlySpan<object>", "System.Span<System.Int32>")] // cannot convert from object to int

        [InlineData("RefStructCollection<T>", "T[]", null, new[] { CollectionExpressionTests.example_RefStructCollection })] // rule requires span

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("RefStructCollection<T>", "RefStructCollection<object>", "RefStructCollection<System.Int32>", new[] { CollectionExpressionTests.example_RefStructCollection })] // rule requires span
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("RefStructCollection<int>", "GenericClassCollection<object>", "RefStructCollection<System.Int32>", new[] { CollectionExpressionTests.example_RefStructCollection, CollectionExpressionTests.example_GenericClassCollection })] // rule requires span
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("RefStructCollection<object>", "GenericClassCollection<int>", "GenericClassCollection<System.Int32>", new[] { CollectionExpressionTests.example_RefStructCollection, CollectionExpressionTests.example_GenericClassCollection })] // cannot convert object to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("RefStructCollection<int>", "NonGenericClassCollection", "RefStructCollection<System.Int32>", new[] { CollectionExpressionTests.example_RefStructCollection, CollectionExpressionTests.example_NonGenericClassCollection })] // rule requires span

        [InlineData("GenericClassCollection<T>", "T[]", null, new[] { CollectionExpressionTests.example_GenericClassCollection })] // rule requires span
        [InlineData("NonGenericClassCollection", "object[]", null, new[] { CollectionExpressionTests.example_NonGenericClassCollection })] // rule requires span
        [InlineData("System.ReadOnlySpan<T>", "object[]", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<T>", "long[]", "System.ReadOnlySpan<System.Int32>")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'short' in params case (exact target)
        [InlineData("System.ReadOnlySpan<T>", "short[]", "System.ReadOnlySpan<System.Int32>")] // cannot convert int to short
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'long' in params case
        [InlineData("System.ReadOnlySpan<long>", "T[]", "System.Int32[]")] // cannot convert long to int
        // Ambiguous for inline collection expression, but 'long' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "long[]", "System.Int64[]")] // cannot convert object to long

        [InlineData("System.ReadOnlySpan<long>", "object[]", "System.ReadOnlySpan<System.Int64>")]
        [InlineData("System.ReadOnlySpan<long>", "string[]", "System.ReadOnlySpan<System.Int64>")]
        [InlineData("System.ReadOnlySpan<int>", "System.ReadOnlySpan<string>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<T>", "System.Span<T>", "System.ReadOnlySpan<System.Int32>")]

        // PROTOTYPE(ParamsCollections): Inline collection expression picks "System.ReadOnlySpan<System.Int32>", but that params candidate is worse because it is generic
        [InlineData("System.ReadOnlySpan<T>", "System.Span<int>", "System.Span<System.Int32>")]

        [InlineData("System.ReadOnlySpan<T>", "System.Span<object>", "System.ReadOnlySpan<System.Int32>")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'short' in params case (exact target)
        [InlineData("System.ReadOnlySpan<T>", "System.Span<short>", "System.ReadOnlySpan<System.Int32>")]

        [InlineData("System.ReadOnlySpan<T>", "System.ReadOnlySpan<int>", "System.ReadOnlySpan<System.Int32>")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<T>", "System.ReadOnlySpan<object>", "System.ReadOnlySpan<System.Int32>")]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'long' in params case
        [InlineData("System.ReadOnlySpan<T>", "System.ReadOnlySpan<long>", "System.ReadOnlySpan<System.Int32>")]

        [InlineData("System.Span<T>", "System.Span<int>", "System.Span<System.Int32>")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.Span<T>", "System.Span<object>", "System.Span<System.Int32>")]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'short' in params case (exact target)
        [InlineData("System.Span<T>", "System.Span<short>", "System.Span<System.Int32>")]

        [InlineData("System.Span<T>", "System.Span<string>", "System.Span<System.Int32>")]
        [InlineData("T[]", "int[]", "System.Int32[]")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("T[]", "object[]", "System.Int32[]")]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("T[]", "int?[]", "System.Int32[]")]

        [InlineData("System.Collections.Generic.ICollection<T>", "System.Collections.Generic.ICollection<int>", "System.Collections.Generic.ICollection<System.Int32>")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.Collections.Generic.ICollection<T>", "System.Collections.Generic.ICollection<object>", "System.Collections.Generic.ICollection<System.Int32>")]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'short' in params case (exact target)
        [InlineData("System.Collections.Generic.ICollection<T>", "System.Collections.Generic.ICollection<short>", "System.Collections.Generic.ICollection<System.Int32>")]

        [InlineData("System.Collections.Generic.ICollection<T>", "System.Collections.Generic.IReadOnlyCollection<T>", null)]
        [InlineData("MyCollectionA<T>", "MyCollectionB<T>", "MyCollectionB<System.Int32>", new[] { CollectionExpressionTests.example_GenericClassesWithConversion })]

        // PROTOTYPE(ParamsCollections): Inline collection expression picks "MyCollectionB<System.Int32>", but that params candidate is worse because it is generic
        [InlineData("MyCollectionA<int>", "MyCollectionB<T>", "MyCollectionA<System.Int32>", new[] { CollectionExpressionTests.example_GenericClassesWithConversion })]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'long' in params case
        [InlineData("MyCollectionA<T>", "MyCollectionB<long>", "MyCollectionA<System.Int32>", new[] { CollectionExpressionTests.example_GenericClassesWithConversion })]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("MyCollectionA<T>", "MyCollectionB<object>", "MyCollectionA<System.Int32>", new[] { CollectionExpressionTests.example_GenericClassesWithConversion })]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'long' in params case
        [InlineData("MyCollectionB<T>", "MyCollectionB<long>", "MyCollectionB<System.Int32>", new[] { CollectionExpressionTests.example_GenericClassesWithConversion })]

        [InlineData("RefStructConvertibleFromArray<T>", "T[]", "System.Int32[]", new[] { CollectionExpressionTests.example_RefStructConvertibleFromArray })]
        [InlineData("RefStructConvertibleFromArray<T>", "int[]", "System.Int32[]", new[] { CollectionExpressionTests.example_RefStructConvertibleFromArray })]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("RefStructConvertibleFromArray<object>", "T[]", "System.Int32[]", new[] { CollectionExpressionTests.example_RefStructConvertibleFromArray })]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("RefStructConvertibleFromArray<T>", "object[]", "RefStructConvertibleFromArray<System.Int32>", new[] { CollectionExpressionTests.example_RefStructConvertibleFromArray })]
        public void BetterConversionFromExpression_01A(string type1, string type2, string expectedType, string[] additionalSources = null) // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using System;
                class Program
                {
                    {{generateMethod("F1", type1)}}
                    {{generateMethod("F1", type2)}}
                    {{generateMethod("F2", type2)}}
                    {{generateMethod("F2", type1)}}
                    static void Main()
                    {
                        var x = F1(1, 2, 3);
                        Console.WriteLine(x.GetTypeName());
                        var y = F2(4, 5);
                        Console.WriteLine(y.GetTypeName());
                    }
                }
                """;
            var comp = CreateCompilation(
                getSources(source, additionalSources),
                targetFramework: TargetFramework.Net80,
                options: TestOptions.ReleaseExe);
            if (expectedType is { })
            {
                CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: ExpectedOutput($"""
                    {expectedType}
                    {expectedType}
                    """));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // 0.cs(10,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F1(ReadOnlySpan<long>)' and 'Program.F1(ReadOnlySpan<object>)'
                    //         var x = F1(1, 2, 3);
                    Diagnostic(ErrorCode.ERR_AmbigCall, "F1").WithArguments(generateMethodSignature("F1", type1), generateMethodSignature("F1", type2)).WithLocation(10, 17),
                    // 0.cs(12,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F2(ReadOnlySpan<object>)' and 'Program.F2(ReadOnlySpan<long>)'
                    //         var y = F2(4, 5);
                    Diagnostic(ErrorCode.ERR_AmbigCall, "F2").WithArguments(generateMethodSignature("F2", type2), generateMethodSignature("F2", type1)).WithLocation(12, 17));
            }

            static string getTypeParameters(string type) =>
                type.Contains("T[]") || type.Contains("<T>") ? "<T>" : "";

            static string generateMethod(string methodName, string parameterType) =>
                $"static Type {methodName}{getTypeParameters(parameterType)}(params {parameterType} value) => typeof({parameterType});";

            static string generateMethodSignature(string methodName, string parameterType) =>
                $"Program.{methodName}{getTypeParameters(parameterType)}(params {parameterType})";

            static string[] getSources(string source, string[] additionalSources)
            {
                var builder = ArrayBuilder<string>.GetInstance();
                builder.Add(source);
                builder.Add(CollectionExpressionTests.s_collectionExtensions);
                if (additionalSources is { }) builder.AddRange(additionalSources);
                return builder.ToArrayAndFree();
            }
        }

        [Theory]
        [InlineData("System.ReadOnlySpan<int>", "System.Span<int>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Span<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Span<int?>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<object>", "System.Span<int>", null)] // cannot convert object to int
        [InlineData("System.ReadOnlySpan<int?>", "System.Span<int>", null)] // cannot convert int? to int
        [InlineData("System.ReadOnlySpan<int>", "System.ReadOnlySpan<object>", null)]
        [InlineData("System.ReadOnlySpan<int>", "System.ReadOnlySpan<int?>", null)]
        [InlineData("System.ReadOnlySpan<object>", "System.ReadOnlySpan<int?>", null)]
        [InlineData("System.Span<int>", "System.Span<object>", null)]
        [InlineData("System.Span<int>", "System.Span<int?>", null)]
        [InlineData("System.Span<object>", "System.Span<int?>", null)]
        [InlineData("System.ReadOnlySpan<object>", "System.ReadOnlySpan<long>", null)]
        [InlineData("System.Span<int>", "int?[]", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.IEnumerable<int?>", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.IReadOnlyCollection<int?>", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.IReadOnlyList<int?>", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.ICollection<int?>", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.IList<int?>", "System.Span<System.Int32>")]
        [InlineData("System.Span<int?>", "int[]", null)] // cannot convert int? to int
        [InlineData("System.Span<int?>", "System.Collections.Generic.IEnumerable<int>", null)] // cannot convert int? to int
        [InlineData("System.Span<int?>", "System.Collections.Generic.IReadOnlyCollection<int>", null)] // cannot convert int? to int
        [InlineData("System.Span<int?>", "System.Collections.Generic.IReadOnlyList<int>", null)] // cannot convert int? to int
        [InlineData("System.Span<int?>", "System.Collections.Generic.ICollection<int>", null)] // cannot convert int? to int
        [InlineData("System.Span<int?>", "System.Collections.Generic.IList<int>", null)] // cannot convert int? to int
        [InlineData("System.ReadOnlySpan<int>", "object[]", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.IEnumerable<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.IReadOnlyCollection<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.IReadOnlyList<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.ICollection<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.IList<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<object>", "int[]", null)] // cannot convert object to int
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.IEnumerable<int>", null)] // cannot convert object to int
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.IReadOnlyCollection<int>", null)] // cannot convert object to int
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.IReadOnlyList<int>", null)] // cannot convert object to int
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.ICollection<int>", null)] // cannot convert object to int
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.IList<int>", null)] // cannot convert object to int
        [InlineData("System.Collections.Generic.List<int>", "System.Collections.Generic.IEnumerable<int>", "System.Collections.Generic.List<System.Int32>")]
        [InlineData("int[]", "object[]", null)] // rule requires span
        [InlineData("int[]", "System.Collections.Generic.IReadOnlyList<object>", null)] // rule requires span
        public void BetterConversionFromExpression_01B_Empty(string type1, string type2, string expectedType) // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using System;
                class Program
                {
                    {{generateMethod("F1", type1)}}
                    {{generateMethod("F1", type2)}}
                    {{generateMethod("F2", type2)}}
                    {{generateMethod("F2", type1)}}
                    static void Main()
                    {
                        var a = F1();
                        Console.WriteLine(a.GetTypeName());
                        var b = F2();
                        Console.WriteLine(b.GetTypeName());
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { source, CollectionExpressionTests.s_collectionExtensions },
                targetFramework: TargetFramework.Net80,
                options: TestOptions.ReleaseExe);
            if (expectedType is { })
            {
                CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: ExpectedOutput($"""
                    {expectedType}
                    {expectedType}
                    """));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // 0.cs(10,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F1(int[])' and 'Program.F1(object[])'
                    //         var a = F1();
                    Diagnostic(ErrorCode.ERR_AmbigCall, "F1").WithArguments(generateMethodSignature("F1", type1), generateMethodSignature("F1", type2)).WithLocation(10, 17),
                    // 0.cs(12,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F2(object[])' and 'Program.F2(int[])'
                    //         var b = F2();
                    Diagnostic(ErrorCode.ERR_AmbigCall, "F2").WithArguments(generateMethodSignature("F2", type2), generateMethodSignature("F2", type1)).WithLocation(12, 17));
            }

            static string generateMethod(string methodName, string parameterType) =>
                $"static Type {methodName}(params {parameterType} value) => typeof({parameterType});";

            static string generateMethodSignature(string methodName, string parameterType) =>
                $"Program.{methodName}(params {parameterType})";
        }

        [Theory]
        [InlineData("System.ReadOnlySpan<int>", "System.Span<int>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Span<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Span<int?>", "System.ReadOnlySpan<System.Int32>")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "System.Span<int>", "System.Span<System.Int32>")] // cannot convert object to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("System.ReadOnlySpan<int?>", "System.Span<int>", "System.Span<System.Int32>")] // cannot convert int? to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<int>", "System.ReadOnlySpan<object>", "System.ReadOnlySpan<System.Int32>")]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("System.ReadOnlySpan<int>", "System.ReadOnlySpan<int?>", "System.ReadOnlySpan<System.Int32>")]
        // Ambiguous for inline collection expression, but 'int?' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "System.ReadOnlySpan<int?>", "System.ReadOnlySpan<System.Nullable<System.Int32>>")]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.Span<int>", "System.Span<object>", "System.Span<System.Int32>")]
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("System.Span<int>", "System.Span<int?>", "System.Span<System.Int32>")]
        // Ambiguous for inline collection expression, but 'int?' is a better conversion target than 'object' in params case
        [InlineData("System.Span<object>", "System.Span<int?>", "System.Span<System.Nullable<System.Int32>>")]
        // Ambiguous for inline collection expression, but 'long' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "System.ReadOnlySpan<long>", "System.ReadOnlySpan<System.Int64>")]

        [InlineData("System.Span<int>", "int?[]", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.IEnumerable<int?>", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.IReadOnlyCollection<int?>", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.IReadOnlyList<int?>", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.ICollection<int?>", "System.Span<System.Int32>")]
        [InlineData("System.Span<int>", "System.Collections.Generic.IList<int?>", "System.Span<System.Int32>")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("System.Span<int?>", "int[]", "System.Int32[]")] // cannot convert int? to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("System.Span<int?>", "System.Collections.Generic.IEnumerable<int>", "System.Collections.Generic.IEnumerable<System.Int32>")] // cannot convert int? to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("System.Span<int?>", "System.Collections.Generic.IReadOnlyCollection<int>", "System.Collections.Generic.IReadOnlyCollection<System.Int32>")] // cannot convert int? to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("System.Span<int?>", "System.Collections.Generic.IReadOnlyList<int>", "System.Collections.Generic.IReadOnlyList<System.Int32>")] // cannot convert int? to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("System.Span<int?>", "System.Collections.Generic.ICollection<int>", "System.Collections.Generic.ICollection<System.Int32>")] // cannot convert int? to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'int?' in params case
        [InlineData("System.Span<int?>", "System.Collections.Generic.IList<int>", "System.Collections.Generic.IList<System.Int32>")] // cannot convert int? to int

        [InlineData("System.ReadOnlySpan<int>", "object[]", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.IEnumerable<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.IReadOnlyCollection<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.IReadOnlyList<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.ICollection<object>", "System.ReadOnlySpan<System.Int32>")]
        [InlineData("System.ReadOnlySpan<int>", "System.Collections.Generic.IList<object>", "System.ReadOnlySpan<System.Int32>")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "int[]", "System.Int32[]")] // cannot convert object to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.IEnumerable<int>", "System.Collections.Generic.IEnumerable<System.Int32>")] // cannot convert object to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.IReadOnlyCollection<int>", "System.Collections.Generic.IReadOnlyCollection<System.Int32>")] // cannot convert object to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.IReadOnlyList<int>", "System.Collections.Generic.IReadOnlyList<System.Int32>")] // cannot convert object to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.ICollection<int>", "System.Collections.Generic.ICollection<System.Int32>")] // cannot convert object to int
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("System.ReadOnlySpan<object>", "System.Collections.Generic.IList<int>", "System.Collections.Generic.IList<System.Int32>")] // cannot convert object to int

        [InlineData("System.Collections.Generic.List<int>", "System.Collections.Generic.IEnumerable<int>", "System.Collections.Generic.List<System.Int32>")]

        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("int[]", "object[]", "System.Int32[]")] // rule requires span
        // Ambiguous for inline collection expression, but 'int' is a better conversion target than 'object' in params case
        [InlineData("int[]", "System.Collections.Generic.IReadOnlyList<object>", "System.Int32[]")] // rule requires span
        public void BetterConversionFromExpression_01B_NotEmpty(string type1, string type2, string expectedType) // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using System;
                class Program
                {
                    {{generateMethod("F1", type1)}}
                    {{generateMethod("F1", type2)}}
                    {{generateMethod("F2", type2)}}
                    {{generateMethod("F2", type1)}}
                    static void Main()
                    {
                        var c = F1(1, 2, 3);
                        Console.WriteLine(c.GetTypeName());
                        var d = F2(4, 5);
                        Console.WriteLine(d.GetTypeName());
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { source, CollectionExpressionTests.s_collectionExtensions },
                targetFramework: TargetFramework.Net80,
                options: TestOptions.ReleaseExe);
            if (expectedType is { })
            {
                CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: ExpectedOutput($"""
                    {expectedType}
                    {expectedType}
                    """));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // 0.cs(10,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F1(int[])' and 'Program.F1(object[])'
                    //         var c = F1(1, 2, 3);
                    Diagnostic(ErrorCode.ERR_AmbigCall, "F1").WithArguments(generateMethodSignature("F1", type1), generateMethodSignature("F1", type2)).WithLocation(10, 17),
                    // 0.cs(12,17): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F2(object[])' and 'Program.F2(int[])'
                    //         var d = F2(4, 5);
                    Diagnostic(ErrorCode.ERR_AmbigCall, "F2").WithArguments(generateMethodSignature("F2", type2), generateMethodSignature("F2", type1)).WithLocation(12, 17));
            }

            static string generateMethod(string methodName, string parameterType) =>
                $"static Type {methodName}(params {parameterType} value) => typeof({parameterType});";

            static string generateMethodSignature(string methodName, string parameterType) =>
                $"Program.{methodName}(params {parameterType})";
        }

        [Fact]
        public void BetterConversionFromExpression_02() // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string sourceA = """
                using System;
                using static System.Console;

                partial class Program
                {
                    static void Generic<T>(params Span<T> value) { WriteLine("Span<T>"); }
                    static void Generic<T>(params T[] value)     { WriteLine("T[]"); }

                    static void Identical(params Span<string> value) { WriteLine("Span<string>"); }
                    static void Identical(params string[] value)     { WriteLine("string[]"); }

                    static void SpanDerived(params Span<string> value) { WriteLine("Span<string>"); }
                    static void SpanDerived(params object[] value)     { WriteLine("object[]"); }

                    static void ArrayDerived(params Span<object> value) { WriteLine("Span<object>"); }
                    static void ArrayDerived(params string[] value)     { WriteLine("string[]"); }
                }
                """;

            string sourceB1 = """
                partial class Program
                {
                    static void Main()
                    {
                        Generic(new[] { string.Empty }); // string[]
                        Identical(new[] { string.Empty }); // string[]
                        ArrayDerived(new[] { string.Empty }); // string[]

                        Generic(string.Empty); // Span<string>
                        Identical(string.Empty); // Span<string>
                        SpanDerived(string.Empty); // Span<string>

                        // Ambiguous for inline collection expression, but 'string' is a better conversion target than 'object' in params case
                        ArrayDerived(string.Empty);
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { sourceA, sourceB1 },
                targetFramework: TargetFramework.Net80,
                options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: ExpectedOutput("""
                T[]
                string[]
                string[]
                Span<T>
                Span<string>
                Span<string>
                string[]
                """));
        }

        [WorkItem("https://github.com/dotnet/roslyn/issues/69634")]
        [Fact]
        public void BetterConversionFromExpression_03() // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string sourceA = """
                using System;
                using static System.Console;

                partial class Program
                {
                    static void Unrelated(params Span<int> value) { WriteLine("Span<int>"); }
                    static void Unrelated(params string[] value)     { WriteLine("string[]"); }
                }
                """;

            string sourceB1 = """
                partial class Program
                {
                    static void Main()
                    {
                        Unrelated(new[] { 1 }); // Span<int>
                        Unrelated(new[] { string.Empty }); // string[]

                        Unrelated(2); // Span<int>
                        Unrelated(string.Empty); // string[]
                    }
                }
                """;
            var comp = CreateCompilation(
                new[] { sourceA, sourceB1 },
                targetFramework: TargetFramework.Net80,
                options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: ExpectedOutput("""
                Span<int>
                string[]
                Span<int>
                string[]
                """));

            string sourceB2 = """
                partial class Program
                {
                    static void Main()
                    {
                        Unrelated(new[] { default }); // error
                        Unrelated(default); // ambiguous
                    }
                }
                """;
            comp = CreateCompilation(
                new[] { sourceA, sourceB2 },
                targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // 1.cs(5,19): error CS0826: No best type found for implicitly-typed array
                //         Unrelated(new[] { default }); // error
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new[] { default }").WithLocation(5, 19),
                // 1.cs(5,19): error CS1503: Argument 1: cannot convert from '?[]' to 'int'
                //         Unrelated(new[] { default }); // error
                Diagnostic(ErrorCode.ERR_BadArgType, "new[] { default }").WithArguments("1", "?[]", "int").WithLocation(5, 19),
                // 1.cs(6,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.Unrelated(params Span<int>)' and 'Program.Unrelated(params string[])'
                //         Unrelated(default); // ambiguous
                Diagnostic(ErrorCode.ERR_AmbigCall, "Unrelated").WithArguments("Program.Unrelated(params System.Span<int>)", "Program.Unrelated(params string[])").WithLocation(6, 9)
                );
        }

        [Fact]
        public void BetterConversionFromExpression_04() // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = """
                using System;
                class Program
                {
                    static void F1(int[] x, params int[] y) { throw null; }
                    static void F1(Span<object> x, params ReadOnlySpan<int> y) { x.Report(); y.Report(); }
                    static void F2(object x, params string[] y) { throw null; }
                    static void F2(string x, params Span<object> y) { y.Report(); }
                    static void Main()
                    {
                        F1([1], 2);
                        F2("3", "4");
                    }
                }
                """;
            CreateCompilation(
                new[] { source, CollectionExpressionTests.s_collectionExtensionsWithSpan },
                targetFramework: TargetFramework.Net80).VerifyDiagnostics(
                // PROTOTYPE(ParamsCollections): Inline collection expression works in this case.
                //                               For 'params' case it fails because:
                //                                  - For the first argument, 'int[]' and 'Span<object>' -> neither is better
                //                                  - For the second argument, 'int' and 'int' -> neither is better vs. 'int[]' and 'ReadOnlySpan<int>' -> ReadOnlySpan<int> for a collection expression 
                //                               Parameters type sequences are different, tie-breaking rules do not apply.   

                // 0.cs(10,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F1(int[], params int[])' and 'Program.F1(Span<object>, params ReadOnlySpan<int>)'
                //         F1([1], 2);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F1").WithArguments("Program.F1(int[], params int[])", "Program.F1(System.Span<object>, params System.ReadOnlySpan<int>)").WithLocation(10, 9),

                // PROTOTYPE(ParamsCollections): Inline collection expression works in this case.
                //                               For 'params' case it fails because:
                //                                  - For the first argument, 'object' and 'string' -> string
                //                                  - For the second argument, 'string' and 'object' -> string (different direction) vs. 'string[]' and 'Span<object>' -> neither is better for a collection expression 
                //                               Parameters type sequences are different, tie-breaking rules do not apply.   

                // 0.cs(11,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F2(object, params string[])' and 'Program.F2(string, params Span<object>)'
                //         F2("3", "4");
                Diagnostic(ErrorCode.ERR_AmbigCall, "F2").WithArguments("Program.F2(object, params string[])", "Program.F2(string, params System.Span<object>)").WithLocation(11, 9)
                );
        }

        [Fact]
        public void BetterConversionFromExpression_05() // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = """
                using System;
                class Program
                {
                    static void F1(Span<int> x, params int[] y)  { x.Report(); y.Report(); }
                    static void F1(int[] x, params ReadOnlySpan<int> y) { throw null; }
                    static void F2(string x, params string[] y) { y.Report(); }
                    static void F2(object x, params Span<string> y) { throw null; }
                    static void Main()
                    {
                        F1([1], 2);
                        F2("3", "4");
                    }
                }
                """;

            // Both calls are ambiguous for inline collection expressions, due to better-ness in different directions among arguments.
            // For params case, there is no difference in the target type for the second argument
            CompileAndVerify(
                new[] { source, CollectionExpressionTests.s_collectionExtensionsWithSpan },
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: ExpectedOutput("[1], [2], [4], "));
        }

        // Two ref struct collection types, with an implicit conversion from one to the other.
        [Fact]
        public void BetterConversionFromExpression_06() // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = """
                using System;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create1))]
                ref struct MyCollection1<T>
                {
                    private readonly List<T> _list;
                    public MyCollection1(List<T> list) { _list = list; }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                    public static implicit operator MyCollection2<T>(MyCollection1<T> c) => new(c._list);
                }
                [CollectionBuilder(typeof(MyCollectionBuilder), nameof(MyCollectionBuilder.Create2))]
                ref struct MyCollection2<T>
                {
                    private readonly List<T> _list;
                    public MyCollection2(List<T> list) { _list = list; }
                    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
                }
                static class MyCollectionBuilder
                {
                    public static MyCollection1<T> Create1<T>(scoped ReadOnlySpan<T> items)
                    {
                        return new MyCollection1<T>(new List<T>(items.ToArray()));
                    }
                    public static MyCollection2<T> Create2<T>(scoped ReadOnlySpan<T> items)
                    {
                        return new MyCollection2<T>(new List<T>(items.ToArray()));
                    }
                }
                class Program
                {
                    static void F1<T>(params MyCollection1<T> c) { Console.WriteLine("MyCollection1<T>"); }
                    static void F1<T>(params MyCollection2<T> c) { Console.WriteLine("MyCollection2<T>"); }
                    static void F2(params MyCollection2<object> c) { Console.WriteLine("MyCollection2<object>"); }
                    static void F2(params MyCollection1<object> c) { Console.WriteLine("MyCollection1<object>"); }
                    static void Main()
                    {
                        F1(1, 2, 3);
                        F2(4, null);
                        F1((MyCollection1<object>)[6]);
                        F1((MyCollection2<int>)[7]);
                        F2((MyCollection2<object>)[8]);
                    }
                }
                """;
            CompileAndVerify(
                source,
                targetFramework: TargetFramework.Net80,
                verify: Verification.Skipped,
                expectedOutput: ExpectedOutput("""
                    MyCollection1<T>
                    MyCollection1<object>
                    MyCollection1<T>
                    MyCollection2<T>
                    MyCollection2<object>
                    """));
        }

        [Fact]
        public void BetterConversionFromExpression_07() // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = """
                using System;
                class Program
                {
                    static void F1(params ReadOnlySpan<int> value) { Console.WriteLine("int"); }
                    static void F1(params ReadOnlySpan<object> value) { }
                    static void F2(params Span<string> value) { Console.WriteLine("string"); }
                    static void F2(params Span<object> value) { }
                    static void Main()
                    {
                        F1(1, 2, 3);
                        F2("a", "b");
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                targetFramework: TargetFramework.Net80,
                options: TestOptions.ReleaseExe);

            // Ambiguity in case of inline collection expression
            CompileAndVerify(
                comp,
                verify: ExecutionConditionUtil.IsMonoOrCoreClr ?
                            Verification.FailsILVerify with { ILVerifyMessage = "[InlineArrayAsSpan]: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator. { Offset = 0xc }" }
                            : Verification.Skipped,
                expectedOutput: ExpectedOutput(@"
int
string
")).VerifyDiagnostics();
        }

        [Fact]
        public void BetterConversionFromExpression_08A() // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = """
                class Program
                {
                    static void F1(params int[] value) { System.Console.WriteLine("int"); }
                    static void F1(params object[] value) { }
                    static void Main()
                    {
                        F1(1, 2, 3);
                    }
                }
                """;
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            // No behavior change (param arrays). Ambiguity in case of inline collection expression
            CompileAndVerify(
                comp,
                expectedOutput: @"int").VerifyDiagnostics();
        }

        [Fact]
        public void BetterConversionFromExpression_08B() // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = """
                using System;
                class Program
                {
                    static void F2(params string[] value) { Console.WriteLine("string[]"); }
                    static void F2(params object[] value) { Console.WriteLine("object[]"); }
                    static void Main()
                    {
                        F2("a", "b");
                    }
                }
                """;
            CompileAndVerify(source, expectedOutput: "string[]");
        }

        [Theory]
        [InlineData("System.ReadOnlySpan<char>")]
        [InlineData("System.Span<char>")]
        public void BetterConversionFromExpression_String_01(string spanType) // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using System;
                using static System.Console;

                class Program
                {
                    static void F1(params {{spanType}} value) { WriteLine("F1({{spanType}})"); }
                    static void F1(params string value) { WriteLine("F1(string)"); }
                    static void F2(params string value) { WriteLine("F2(string)"); }
                    static void F2(params {{spanType}} value) { WriteLine("F2({{spanType}})"); }

                    static void Main()
                    {
                        F1();
                        F2();
                        F1('a', 'b', 'c');
                        F2('1', '2', '3');
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                targetFramework: TargetFramework.Net80,
                options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: ExpectedOutput($$"""
                F1({{spanType}})
                F2({{spanType}})
                F1({{spanType}})
                F2({{spanType}})
                """));
        }

        [Theory]
        [InlineData("System.ReadOnlySpan<int>")]
        [InlineData("System.Span<int>")]
        [InlineData("System.ReadOnlySpan<object>")]
        [InlineData("System.Span<object>")]
        public void BetterConversionFromExpression_String_02_Empty(string spanType) // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using System;
                using static System.Console;

                class Program
                {
                    static void F1(params {{spanType}} value) { WriteLine("F1({{spanType}})"); }
                    static void F1(params string value) { WriteLine("F1(string)"); }
                    static void F2(params string value) { WriteLine("F2(string)"); }
                    static void F2(params {{spanType}} value) { WriteLine("F2({{spanType}})"); }

                    static void Main()
                    {
                        F1();
                        F2();
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (13,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F1(params ReadOnlySpan<int>)' and 'Program.F1(params string)'
                //         F1();
                Diagnostic(ErrorCode.ERR_AmbigCall, "F1").WithArguments($"Program.F1(params {spanType})", "Program.F1(params string)").WithLocation(13, 9),
                // (14,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F2(params string)' and 'Program.F2(params ReadOnlySpan<int>)'
                //         F2();
                Diagnostic(ErrorCode.ERR_AmbigCall, "F2").WithArguments("Program.F2(params string)", $"Program.F2(params {spanType})").WithLocation(14, 9));
        }

        [Theory]
        [InlineData("System.ReadOnlySpan<int>")]
        [InlineData("System.Span<int>")]
        [InlineData("System.ReadOnlySpan<object>")]
        [InlineData("System.Span<object>")]
        public void BetterConversionFromExpression_String_02_NotEmpty(string spanType) // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using System;
                using static System.Console;

                class Program
                {
                    static void F1(params {{spanType}} value) { WriteLine("F1({{spanType}})"); }
                    static void F1(params string value) { WriteLine("F1(string)"); }
                    static void F2(params string value) { WriteLine("F2(string)"); }
                    static void F2(params {{spanType}} value) { WriteLine("F2({{spanType}})"); }

                    static void Main()
                    {
                        F1('a', 'b', 'c');
                        F2('1', '2', '3');
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                targetFramework: TargetFramework.Net80);

            // Inline collection expression results in an ambiguity.
            comp.VerifyEmitDiagnostics(
                // (13,9): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         F1('a', 'b', 'c');
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "F1('a', 'b', 'c')").WithArguments("string", "0").WithLocation(13, 9),
                // (13,12): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         F1('a', 'b', 'c');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'a'").WithArguments("string", "Add").WithLocation(13, 12),
                // (13,17): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         F1('a', 'b', 'c');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'b'").WithArguments("string", "Add").WithLocation(13, 17),
                // (13,22): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         F1('a', 'b', 'c');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'c'").WithArguments("string", "Add").WithLocation(13, 22),
                // (14,9): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         F2('1', '2', '3');
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "F2('1', '2', '3')").WithArguments("string", "0").WithLocation(14, 9),
                // (14,12): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         F2('1', '2', '3');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'1'").WithArguments("string", "Add").WithLocation(14, 12),
                // (14,17): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         F2('1', '2', '3');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'2'").WithArguments("string", "Add").WithLocation(14, 17),
                // (14,22): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         F2('1', '2', '3');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'3'").WithArguments("string", "Add").WithLocation(14, 22)
                );
        }

        [Theory]
        [InlineData("System.ReadOnlySpan<byte>")]
        [InlineData("System.Span<byte>")]
        public void BetterConversionFromExpression_String_03(string spanType) // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using System;
                using static System.Console;

                class Program
                {
                    static void F1(params {{spanType}} value) { WriteLine("F1({{spanType}})"); }
                    static void F1(params string value) { WriteLine("F1(string)"); }
                    static void F2(params string value) { WriteLine("F2(string)"); }
                    static void F2(params {{spanType}} value) { WriteLine("F2({{spanType}})"); }

                    static void Main()
                    {
                        F1();
                        F2();
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                targetFramework: TargetFramework.Net80);
            comp.VerifyEmitDiagnostics(
                // (13,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F1(params ReadOnlySpan<byte>)' and 'Program.F1(params string)'
                //         F1();
                Diagnostic(ErrorCode.ERR_AmbigCall, "F1").WithArguments($"Program.F1(params {spanType})", $"Program.F1(params string)").WithLocation(13, 9),
                // (14,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.F2(params string)' and 'Program.F2(params ReadOnlySpan<byte>)'
                //         F2();
                Diagnostic(ErrorCode.ERR_AmbigCall, "F2").WithArguments($"Program.F2(params string)", $"Program.F2(params {spanType})").WithLocation(14, 9));
        }

        [Theory]
        [InlineData("System.ReadOnlySpan<MyChar>")]
        [InlineData("System.Span<MyChar>")]
        public void BetterConversionFromExpression_String_04_Empty(string spanType) // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using System;
                using static System.Console;

                class MyChar
                {
                    private readonly int _i;
                    public MyChar(int i) { _i = i; }
                    public static implicit operator MyChar(int i) => new MyChar(i);
                    public static implicit operator char(MyChar c) => (char)c._i;
                }

                class Program
                {
                    static void F1(params {{spanType}} value) { WriteLine("F1({{spanType}})"); }
                    static void F1(params string value) { WriteLine("F1(string)"); }
                    static void F2(params string value) { WriteLine("F2(string)"); }
                    static void F2(params {{spanType}} value) { WriteLine("F2({{spanType}})"); }

                    static void Main()
                    {
                        F1();
                        F2();
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                targetFramework: TargetFramework.Net80,
                options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput: ExpectedOutput($$"""
                F1({{spanType}})
                F2({{spanType}})
                """));
        }

        [Theory]
        [InlineData("System.ReadOnlySpan<MyChar>")]
        [InlineData("System.Span<MyChar>")]
        public void BetterConversionFromExpression_String_04_NotEmpty(string spanType) // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using static System.Console;

                class MyChar
                {
                    private readonly int _i;
                    public MyChar(int i) { _i = i; }
                    public static implicit operator MyChar(int i) => new MyChar(i);
                    public static implicit operator char(MyChar c) => (char)c._i;
                }

                class Program
                {
                    static void F1(params {{spanType}} value) { WriteLine("F1({{spanType}})"); }
                    static void F1(params string value) { WriteLine("F1(string)"); }
                    static void F2(params string value) { WriteLine("F2(string)"); }
                    static void F2(params {{spanType}} value) { WriteLine("F2({{spanType}})"); }

                    static void Main()
                    {
                        F1('a', 'b', 'c');
                        F2('1', '2', '3');
                    }
                }
                """;
            var comp = CreateCompilation(
                source,
                targetFramework: TargetFramework.Net80,
                options: TestOptions.ReleaseExe);

            // PROTOTYPE(ParamsCollections): Inline collection expression picks a different overload and succeeds.
            comp.VerifyDiagnostics(
                // (20,9): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         F1('a', 'b', 'c');
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "F1('a', 'b', 'c')").WithArguments("string", "0").WithLocation(20, 9),
                // (20,12): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         F1('a', 'b', 'c');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'a'").WithArguments("string", "Add").WithLocation(20, 12),
                // (20,17): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         F1('a', 'b', 'c');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'b'").WithArguments("string", "Add").WithLocation(20, 17),
                // (20,22): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         F1('a', 'b', 'c');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'c'").WithArguments("string", "Add").WithLocation(20, 22),
                // (21,9): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         F2('1', '2', '3');
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "F2('1', '2', '3')").WithArguments("string", "0").WithLocation(21, 9),
                // (21,12): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         F2('1', '2', '3');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'1'").WithArguments("string", "Add").WithLocation(21, 12),
                // (21,17): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         F2('1', '2', '3');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'2'").WithArguments("string", "Add").WithLocation(21, 17),
                // (21,22): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         F2('1', '2', '3');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'3'").WithArguments("string", "Add").WithLocation(21, 22)
                );
        }

        [Fact]
        public void BetterConversionFromExpression_String_05() // This is a clone of a unit-test from CollectionExpressionTests.cs
        {
            string source = $$"""
                using System;
                using System.Collections.Generic;
                using static System.Console;

                class Program
                {
                    static void F(params IEnumerable<char> value) { WriteLine("F(IEnumerable<char>)"); }
                    static void F(params string value) { WriteLine("F(string)"); }

                    static void Main()
                    {
                        F();
                        F('a');
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (12,9): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         F();
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "F()").WithArguments("string", "0").WithLocation(12, 9),
                // (13,9): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                //         F('a');
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "F('a')").WithArguments("string", "0").WithLocation(13, 9),
                // (13,11): error CS1061: 'string' does not contain a definition for 'Add' and no accessible extension method 'Add' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         F('a');
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "'a'").WithArguments("string", "Add").WithLocation(13, 11));
        }

        [Fact]
        public void BetterOverload_01_BetterParameterPassing()
        {
            // the better parameter-passing choice (https://github.com/dotnet/csharpstandard/blob/draft-v9/standard/expressions.md#12644-better-parameter-passing-mode)
            // should come before collection better-ness, but after argument conversion better-ness.
            // Expected output below matches legacy behavior of param arrays.

            var src = """
class Program
{
    static void Main()
    {
        Test(1);
        Test(1, new C2());
    }

    static void Test(in int x, params C2[] y)
    {
        System.Console.Write("In");
    }

    static void Test(int x, params C1[] y)
    {
        System.Console.Write("Val");
    }
}

class C1 {}
class C2 : C1 {}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: ExpectedOutput(@"ValIn")).VerifyDiagnostics();
        }

        [Fact]
        public void BetterOverload_02_NotSameCollectionElements()
        {
            var src = """
class Program
{
    static void Main()
    {
        Test(x: 1, y: 2);
    }

    static void Test(int x, params System.ReadOnlySpan<int> y)
    {
        System.Console.Write("ReadOnlySpan");
    }

    static void Test(int y, params System.Span<int> x)
    {
        System.Console.Write("Span");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (5,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.Test(int, params ReadOnlySpan<int>)' and 'Program.Test(int, params Span<int>)'
                //         Test(x: 1, y: 2);
                Diagnostic(ErrorCode.ERR_AmbigCall, "Test").WithArguments("Program.Test(int, params System.ReadOnlySpan<int>)", "Program.Test(int, params System.Span<int>)").WithLocation(5, 9)
                );
        }

        [Fact]
        public void BetterOverload_03_NotSameCollectionElements()
        {
            var src = """
class Program
{
    static void Main()
    {
        Test(x: 1, y: 2);
    }

    static void Test(long x, params System.ReadOnlySpan<int> y)
    {
        System.Console.Write("ReadOnlySpan");
    }

    static void Test(int y, params long[] x)
    {
        System.Console.Write("Span");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (5,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.Test(long, params ReadOnlySpan<int>)' and 'Program.Test(int, params long[])'
                //         Test(x: 1, y: 2);
                Diagnostic(ErrorCode.ERR_AmbigCall, "Test").WithArguments("Program.Test(long, params System.ReadOnlySpan<int>)", "Program.Test(int, params long[])").WithLocation(5, 9)
                );
        }

        [Fact]
        public void GenericInference()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        long l = 1;
        int i = 2;
        byte b = 3;
        Test(i, b, l);
    }

    static void Test<T>(params IEnumerable<T> b)
    {
        System.Console.Write(typeof(T));
    }
}
""";
            var comp = CreateCompilation(src, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: ExpectedOutput(@"System.Int64")).VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_01()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(d);
        Test(d, 1);
        Test(d, 2, 3);
        Test(2, d, 3);
        Test(2, 3, d);
        Test(d, [3, 4]);

        Test2(d, d);
        Test2(d, 1);
        Test2(d, 2, 3);
        Test2(2, d, 3);
        Test2(2, 3, d);

        Test2<int>(d);
        Test2<int>(d, d);
        Test2<int>(d, 1);
        Test2<int>(d, 2, 3);
        Test2<int>(2, d, 3);
        Test2<int>(2, 3, d);
        Test2<int>(d, [3, 4]);
    }

    static void Test(int a, params IEnumerable<int> b)
    {
        System.Console.Write("Called");
    }

    static void Test2<T>(int a, params T[] b)
    {
        System.Console.Write("Called2");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"CalledCalledCalledCalledCalledCalledCalled2Called2Called2Called2Called2Called2Called2Called2Called2Called2Called2Called2").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_02_AmbiguousDynamicParamsArgument()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(d);
    }

    static void Test(params IEnumerable<int> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,14): error CS9502: Ambiguity between expanded and normal forms of non-array params collection parameter of 'Program.Test(params IEnumerable<int>)', the only corresponding argument has the type 'dynamic'. Consider casting the dynamic argument.
                //         Test(d);
                Diagnostic(ErrorCode.ERR_ParamsCollectionAmbiguousDynamicArgument, "d").WithArguments("Program.Test(params System.Collections.Generic.IEnumerable<int>)").WithLocation(8, 14)
                );
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_03_Warning()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d1 = System.DateTime.Now;
        Test1(d1);                  // Called2
        
        dynamic d2 = new[] { 1 };
        Test1(d2);                  // Called1
        Test2(1, d1);               // Called3
        Test2(1, d2);               // Called5
        
        int x = 1;
        Test2(x, d1);               // Called3
        Test2(x, d2);               // Called4

        dynamic d3 = (byte)1;
        Test3(d3, 1, 2);            // Called7
        Test3(d3, x, x);            // Called6

        dynamic d4 = x;
        Test4((byte)d3, x, x);      // Called8
        Test4(d3, x, x);            // Called9
        Test4(d3, d4, d4);          // Called9
    }

    static void Test1(params IEnumerable<int> b) => System.Console.Write("Called1");
    static void Test1(System.DateTime b) => System.Console.Write("Called2");

    static void Test2(int x, System.DateTime b) => System.Console.Write("Called3");
    static void Test2(long x, IEnumerable<int> b) => System.Console.Write("Called4");
    static void Test2(byte x, params IEnumerable<int> b) => System.Console.Write("Called5");

    static void Test3(byte x, params IEnumerable<int> b) => System.Console.Write("Called6");
    static void Test3(byte x, byte y, byte z) => System.Console.Write("Called7");

    static void Test4(byte x, params IEnumerable<int> b) => System.Console.Write("Called8");
    static void Test4(byte x, long y, long z) => System.Console.Write("Called9");
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called2Called1Called3Called5Called3Called4Called7Called6Called8Called9Called9").
            VerifyDiagnostics(
                // (8,9): warning CS9503: One or more overloads of method 'Test1' having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         Test1(d1);                  // Called2
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod, "Test1(d1)").WithArguments("Test1").WithLocation(8, 9),
                // (11,9): warning CS9503: One or more overloads of method 'Test1' having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         Test1(d2);                  // Called1
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod, "Test1(d2)").WithArguments("Test1").WithLocation(11, 9),
                // (12,9): warning CS9503: One or more overloads of method 'Test2' having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         Test2(1, d1);               // Called3
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod, "Test2(1, d1)").WithArguments("Test2").WithLocation(12, 9),
                // (13,9): warning CS9503: One or more overloads of method 'Test2' having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         Test2(1, d2);               // Called5
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod, "Test2(1, d2)").WithArguments("Test2").WithLocation(13, 9),
                // (20,9): warning CS9503: One or more overloads of method 'Test3' having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         Test3(d3, 1, 2);            // Called7
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod, "Test3(d3, 1, 2)").WithArguments("Test3").WithLocation(20, 9),
                // (25,9): warning CS9503: One or more overloads of method 'Test4' having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         Test4(d3, x, x);            // Called9
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod, "Test4(d3, x, x)").WithArguments("Test4").WithLocation(25, 9),
                // (26,9): warning CS9503: One or more overloads of method 'Test4' having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         Test4(d3, d4, d4);          // Called9
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod, "Test4(d3, d4, d4)").WithArguments("Test4").WithLocation(26, 9)
                );
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_04()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(d, 2);
    }

    static void Test(int a, params IEnumerable<int> b)
    {
        System.Console.Write("Called {0}", b is not null);
    }

    static void Test(int a, System.DateTime b)
    {
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called True").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_05()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(d, 2, 3);
    }

    static void Test(params IEnumerable<int> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_06_TypeArgumentInferenceError()
        {
            var src1 = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(d, 2, 3);
    }

    static void Test<T>(params IEnumerable<T> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp1.VerifyDiagnostics(
                // (8,9): error CS9501: The type arguments for method 'Program.Test<T>(params IEnumerable<T>)' cannot be inferred from the usage because an argument with dynamic type is used and the method has a non-array params collection parameter. Try specifying the type arguments explicitly.
                //         Test(d, 2, 3);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs_DynamicArgumentWithParamsCollections, "Test(d, 2, 3)").WithArguments("Program.Test<T>(params System.Collections.Generic.IEnumerable<T>)").WithLocation(8, 9)
                );

            var src2 = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test<int>(d, 2, 3);
    }

    static void Test<T>(params IEnumerable<T> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp2 = CreateCompilation(src2, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp2,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_07_TypeArgumentInferenceError()
        {
            var src1 = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(0, d, 2, 3);
    }

    static void Test<T>(T a, params IEnumerable<long> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp1.VerifyDiagnostics(
                // (8,9): error CS9501: The type arguments for method 'Program.Test<T>(T, params IEnumerable<long>)' cannot be inferred from the usage because an argument with dynamic type is used and the method has a non-array params collection parameter. Try specifying the type arguments explicitly.
                //         Test(0, d, 2, 3);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs_DynamicArgumentWithParamsCollections, "Test(0, d, 2, 3)").WithArguments("Program.Test<T>(T, params System.Collections.Generic.IEnumerable<long>)").WithLocation(8, 9)
                );

            var src2 = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test<int>(0, d, 2, 3);
    }

    static void Test<T>(T a, params IEnumerable<long> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp2 = CreateCompilation(src2, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp2,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_08_HideByOverride()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new C2().Test(d, 2, 3);
    }
}

class C1
{
    public virtual void Test(params IEnumerable<int> b){}
}

class C2 : C1
{
    public override void Test(params IEnumerable<int> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_09_HideBySignature()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new C2().Test<int>(d, 2, 3);
    }
}

class C1
{
    public void Test<T>(params IEnumerable<T> b){}
}

class C2 : C1
{
    new public void Test<T>(params IEnumerable<T> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_10_HideBySignature()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new C2().Test(d, 2, 3);
    }
}

class C0
{
    public virtual void Test(params IEnumerable<int> b){}
}

class C1 : C0
{
    public override void Test(params IEnumerable<int> b){}
}

class C2 : C1
{
    new public void Test(params IEnumerable<int> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_11_HideBySignature()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new C3().Test(d, 2, 3);
    }
}

class C1
{
    public void Test(params IEnumerable<int> b){}
}

class C2 : C1
{
    new public virtual void Test(params IEnumerable<int> b) {}
}

class C3 : C2
{
    public override void Test(params IEnumerable<int> b)
    {
        System.Console.Write("Called");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_12_HideByName()
        {
            var src1 = """
using System.Collections.Generic;

public class C1
{
    public void Test(params IEnumerable<int> b){}
}
""";

            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseDll);

            var src2 = """
Public Class C2
    Inherits C1

    Public Shadows Sub Test(ParamArray b As Long())
        System.Console.Write("Called")
    End Sub
End Class
""";

            MetadataReference comp1Ref = comp1.EmitToImageReference();
            var comp2 = CreateVisualBasicCompilation(src2, referencedAssemblies: TargetFrameworkUtil.GetReferences(TargetFramework.Standard).Concat(comp1Ref));

            var src = """
class Program
{
    static void Main()
    {
        dynamic d = 1;
        new C2().Test(d, 2, 3);
    }
}
""";
            var comp = CreateCompilation(src, references: new[] { comp1Ref, comp2.EmitToImageReference() }, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_13_DoNotHideByApplicability()
        {
            var src = """
class Program
{
    static void Main()
    {
        dynamic d = 1L;
        new C2().Test(d);
    }
}

class C1
{
    public void Test(long a)
    {
        System.Console.Write("long");
    }
}

class C2 : C1
{
    public void Test(int b)
    {
        System.Console.Write("int");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"long").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_14_DoNotFilterBasedOnBetterFunctionMember()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1L;
        new C1().Test(1, d, 2);
    }
}

class C1
{
    public void Test(long a1, params IEnumerable<long> a2)
    {
        System.Console.Write("long");
    }

    public void Test(int b1, int b2, int b3)
    {
        System.Console.Write("int");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,9): warning CS9503: One or more overloads of method 'Test' having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         new C1().Test(1, d, 2);
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod, "new C1().Test(1, d, 2)").WithArguments("Test").WithLocation(8, 9)
                );
        }

        [Fact]
        public void DynamicInvocation_OrdinaryMethod_15_Warning()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d1 = 1;
        Test1((int)d1);
        try
        {
            Test1(d1);
        }
        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
        {
            System.Console.Write("Failed");
        }
    }

    static void Test1(params IEnumerable<int> b) => System.Console.Write("Called1");
    static void Test1(System.DateTime b) => System.Console.Write("Called2");
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called1Failed").
            VerifyDiagnostics(
                // (11,13): warning CS9503: One or more overloads of method 'Test1' having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //             Test1(d1);
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod, "Test1(d1)").WithArguments("Test1").WithLocation(11, 13)
                );
        }

        [Fact]
        public void DynamicInvocation_LocalFunction_01()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(d);
        Test(d, 1);
        Test(d, 2, 3);
        Test(2, d, 3);
        Test(2, 3, d);
        Test(2, [3, d]);

        Test2(d);
        Test2(d, 1);
        Test2(d, 2, 3);
        Test2(2, d, 3);
        Test2(2, 3, d);
        Test2(d, [3, 4]);

        void Test(int a, params IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }

        void Test2(int a, params int[] b)
        {
            System.Console.Write("Called2");
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"CalledCalledCalledCalledCalledCalledCalled2Called2Called2Called2Called2Called2").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_LocalFunction_02_AmbiguousDynamicParamsArgument()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(d);

        static void Test(params IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,14): error CS9502: Ambiguity between expanded and normal forms of non-array params collection parameter of 'Test(params IEnumerable<int>)', the only corresponding argument has the type 'dynamic'. Consider casting the dynamic argument.
                //         Test(d);
                Diagnostic(ErrorCode.ERR_ParamsCollectionAmbiguousDynamicArgument, "d").WithArguments("Test(params System.Collections.Generic.IEnumerable<int>)").WithLocation(8, 14)
                );
        }

        [Fact]
        public void DynamicInvocation_LocalFunction_06_TypeArgumentInferenceError()
        {
            var src1 = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(d, 2, 3);

        void Test<T>(params IEnumerable<T> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp1.VerifyDiagnostics(
                // (8,9): error CS9501: The type arguments for method 'Test<T>(params IEnumerable<T>)' cannot be inferred from the usage because an argument with dynamic type is used and the method has a non-array params collection parameter. Try specifying the type arguments explicitly.
                //         Test(d, 2, 3);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs_DynamicArgumentWithParamsCollections, "Test(d, 2, 3)").WithArguments("Test<T>(params System.Collections.Generic.IEnumerable<T>)").WithLocation(8, 9)
                );

            var src2 = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test<int>(d, 2, 3);

        void Test<T>(params IEnumerable<T> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp2 = CreateCompilation(src2, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp2,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_LocalFunction_07_TypeArgumentInferenceError()
        {
            var src1 = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test(0, d, 2, 3);

        void Test<T>(T a, params IEnumerable<long> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp1.VerifyDiagnostics(
                // (8,9): error CS9501: The type arguments for method 'Test<T>(T, params IEnumerable<long>)' cannot be inferred from the usage because an argument with dynamic type is used and the method has a non-array params collection parameter. Try specifying the type arguments explicitly.
                //         Test(0, d, 2, 3);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs_DynamicArgumentWithParamsCollections, "Test(0, d, 2, 3)").WithArguments("Test<T>(T, params System.Collections.Generic.IEnumerable<long>)").WithLocation(8, 9)
                );

            var src2 = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        Test<int>(0, d, 2, 3);

        void Test<T>(T a, params IEnumerable<long> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp2 = CreateCompilation(src2, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp2,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Delegate_01()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        D test = Test;
        dynamic d = 1;
        test(d);
        test(d, 1);
        test(d, 2, 3);
        test(2, d, 3);
        test(2, 3, d);
        test(2, [3, d]);

        D2 test2 = Test2;
        test2(d);
        test2(d, d);
        test2(d, 1);
        test2(d, 2, 3);
        test2(2, d, 3);
        test2(2, 3, d);
        test2(d, [3, 4]);

        void Test(int a, IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
        void Test2(int a, int[] b)
        {
            System.Console.Write("Called2");
        }
    }
}

delegate void D(int a, params IEnumerable<int> b);
delegate void D2(int a, params int[] b);
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"CalledCalledCalledCalledCalledCalledCalled2Called2Called2Called2Called2Called2Called2").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Delegate_02_AmbiguousDynamicParamsArgument()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        D test = Test;
        dynamic d = 1;
        test(d);

        static void Test(IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
    }
}

delegate void D(params IEnumerable<int> b);
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (9,14): error CS9502: Ambiguity between expanded and normal forms of non-array params collection parameter of 'D.Invoke(params IEnumerable<int>)', the only corresponding argument has the type 'dynamic'. Consider casting the dynamic argument.
                //         test(d);
                Diagnostic(ErrorCode.ERR_ParamsCollectionAmbiguousDynamicArgument, "d").WithArguments("D.Invoke(params System.Collections.Generic.IEnumerable<int>)").WithLocation(9, 14)
                );
        }

        [Fact]
        public void DynamicInvocation_Indexer_01()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var c1 = new C1();
        dynamic d = 1;
        _ = c1[d];
        _ = c1[d, 1];
        _ = c1[d, 2, 3];
        _ = c1[2, d, 3];
        _ = c1[2, 3, d];
        _ = c1[d, [3, 4]];

        var c2 = new C2();

        _ = c2[d];
        _ = c2[d, d];
        _ = c2[d, 1];
        _ = c2[d, 2, 3];
        _ = c2[2, d, 3];
        _ = c2[2, 3, d];
        _ = c2[d, [3, 4]];
    }
}

class C1
{
    public int this[int a, params IEnumerable<int> b] 
    {
        get
        {
            System.Console.Write("Called");
            return 0;
        }
    }
}
class C2
{
    public int this[int a, params int[] b] 
    {
        get
        {
            System.Console.Write("Called2");
            return 0;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"CalledCalledCalledCalledCalledCalledCalled2Called2Called2Called2Called2Called2Called2").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Indexer_02_AmbiguousDynamicParamsArgument()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        _ = new Program()[d];
    }

    int this[params IEnumerable<int> b]
    {
        get 
        {
            System.Console.Write("Called");
            return 0;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,27): error CS9502: Ambiguity between expanded and normal forms of non-array params collection parameter of 'Program.this[params IEnumerable<int>]', the only corresponding argument has the type 'dynamic'. Consider casting the dynamic argument.
                //         _ = new Program()[d];
                Diagnostic(ErrorCode.ERR_ParamsCollectionAmbiguousDynamicArgument, "d").WithArguments("Program.this[params System.Collections.Generic.IEnumerable<int>]").WithLocation(8, 27)
                );
        }

        [Fact]
        public void DynamicInvocation_Indexer_03_Warning()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d1 = System.DateTime.Now;
        _ = new Test1()[d1];                  // Called2
        
        dynamic d2 = new[] { 1 };
        _ = new Test1()[d2];                  // Called1
        _ = new Test2()[1, d1];               // Called3
        _ = new Test2()[1, d2];               // Called5
        
        int x = 1;
        _ = new Test2()[x, d1];               // Called3
        _ = new Test2()[x, d2];               // Called4

        dynamic d3 = (byte)1;
        _ = new Test3()[d3, 1, 2];            // Called7
        _ = new Test3()[d3, x, x];            // Called6

        dynamic d4 = x;
        _ = new Test4()[(byte)d3, x, x];      // Called8
        _ = new Test4()[d3, x, x];            // Called9
        _ = new Test4()[d3, d4, d4];          // Called9
    }

    class Test1
    {
        public int this[params IEnumerable<int> b] { get { System.Console.Write("Called1"); return 0; } }
        public int this[System.DateTime b] { get { System.Console.Write("Called2"); return 0; } }
    }
    class Test2
    {
        public int this[int x, System.DateTime b] { get { System.Console.Write("Called3"); return 0; } }
        public int this[long x, IEnumerable<int> b] { get { System.Console.Write("Called4"); return 0; } }
        public int this[byte x, params IEnumerable<int> b] { get { System.Console.Write("Called5"); return 0; } }
    }
    class Test3
    {
        public int this[byte x, params IEnumerable<int> b] { get { System.Console.Write("Called6"); return 0; } }
        public int this[byte x, byte y, byte z] { get { System.Console.Write("Called7"); return 0; } }
    }
    class Test4
    {
        public int this[byte x, params IEnumerable<int> b] { get { System.Console.Write("Called8"); return 0; } }
        public int this[byte x, long y, long z] { get { System.Console.Write("Called9"); return 0; } }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called2Called1Called3Called5Called3Called4Called7Called6Called8Called9Called9").
            VerifyDiagnostics(
                // (8,13): warning CS9504: One or more indexer overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         _ = new Test1()[d1];                  // Called2
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionIndexer, "new Test1()[d1]").WithLocation(8, 13),
                // (11,13): warning CS9504: One or more indexer overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         _ = new Test1()[d2];                  // Called1
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionIndexer, "new Test1()[d2]").WithLocation(11, 13),
                // (12,13): warning CS9504: One or more indexer overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         _ = new Test2()[1, d1];               // Called3
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionIndexer, "new Test2()[1, d1]").WithLocation(12, 13),
                // (13,13): warning CS9504: One or more indexer overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         _ = new Test2()[1, d2];               // Called5
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionIndexer, "new Test2()[1, d2]").WithLocation(13, 13),
                // (20,13): warning CS9504: One or more indexer overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         _ = new Test3()[d3, 1, 2];            // Called7
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionIndexer, "new Test3()[d3, 1, 2]").WithLocation(20, 13),
                // (25,13): warning CS9504: One or more indexer overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         _ = new Test4()[d3, x, x];            // Called9
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionIndexer, "new Test4()[d3, x, x]").WithLocation(25, 13),
                // (26,13): warning CS9504: One or more indexer overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         _ = new Test4()[d3, d4, d4];          // Called9
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionIndexer, "new Test4()[d3, d4, d4]").WithLocation(26, 13)
                );
        }

        [Fact]
        public void DynamicInvocation_Indexer_04()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        _ = new Program()[d, 2];
    }

    int this[int a, params IEnumerable<int> b]
    {
        get
        {
            System.Console.Write("Called {0}", b is not null);
            return 0;
        }
    }

    int this[int a, System.DateTime b] => 0;
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called True").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Indexer_05()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        _ = new Program()[d, 2, 3];
    }

    int this[params IEnumerable<int> b]
    {
        get
        {
            System.Console.Write("Called");
            return 0;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Indexer_08_HideByOverride()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        _ = new C2<int>()[d, 2, 3];
    }
}

class C1<T>
{
    public virtual T this[params IEnumerable<T> b] => default;
}

class C2<T> : C1<T>
{
    public override T this[params IEnumerable<T> b]
    {
        get
        {
            System.Console.Write("Called");
            return default;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Indexer_09_HideBySignature()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        _ = new C2<int>()[d, 2, 3];
    }
}

class C1<T>
{
    public T this[params IEnumerable<T> b] => default;
}

class C2<T> : C1<T>
{
    new public T this[params IEnumerable<T> b]
    {
        get
        {
            System.Console.Write("Called");
            return default;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Indexer_10_HideBySignature()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        _ = new C2<int>()[d, 2, 3];
    }
}

class C0<T>
{
    public virtual T this[params IEnumerable<T> b] => default;
}

class C1<T> : C0<T>
{
    public override T this[params IEnumerable<T> b] => default;
}

class C2<T> : C1<T>
{
    new public T this[params IEnumerable<T> b]
    {
        get
        {
            System.Console.Write("Called");
            return default;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Indexer_11_HideBySignature()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        _ = new C3<int>()[d, 2, 3];
    }
}

class C1<T>
{
    public T this[params IEnumerable<T> b] => default;
}

class C2<T> : C1<T>
{
    new public virtual T this[params IEnumerable<T> b] => default;
}

class C3<T> : C2<T>
{
    public override T this[params IEnumerable<T> b]
    {
        get
        {
            System.Console.Write("Called");
            return default;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Indexer_12_HideByName()
        {
            var src1 = """
using System.Collections.Generic;

public class C1
{
    public int this[int x, params IEnumerable<int> b] => default;
}
""";

            var comp1 = CreateCompilation(src1, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseDll);

            var src2 = """
Public Class C2
    Inherits C1

    Public Shadows Readonly Default Property Item(x as Integer, ParamArray b As Long()) As Integer
        Get
            System.Console.Write("Called")
            Return 0
        End Get
    End Property
End Class
""";

            MetadataReference comp1Ref = comp1.EmitToImageReference();
            var comp2 = CreateVisualBasicCompilation(src2, referencedAssemblies: TargetFrameworkUtil.GetReferences(TargetFramework.Standard).Concat(comp1Ref));

            var src = """
class Program
{
    static void Main()
    {
        dynamic d = 1;
        _ = new C2()[4, d, 2, 3];
    }
}
""";
            var comp = CreateCompilation(src, references: new[] { comp1Ref, comp2.EmitToImageReference() }, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Indexer_13_DoNotHideByApplicability()
        {
            var src = """
class Program
{
    static void Main()
    {
        dynamic d = 1L;
        _ = new C2()[d];
    }
}

class C1
{
    public long this[long a]
    {
        get
        {
            System.Console.Write("long");
            return a;
        }
    }
}

class C2 : C1
{
    public int this[int b]
    {
        get
        {
            System.Console.Write("int");
            return b;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"long").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Indexer_14_DoNotFilterBasedOnBetterFunctionMember()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1L;
        _ = new C1()[1, d, 2];
    }
}

class C1
{
    public long this[long a1, params IEnumerable<long> a2]
    {
        get
        {
            System.Console.Write("long");
            return a1;
        }
    }

    public int this[int b1, int b2, int b3]
    {
        get
        {
            System.Console.Write("int");
            return b1;
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,13): warning CS9504: One or more indexer overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         _ = new C1()[1, d, 2];
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionIndexer, "new C1()[1, d, 2]").WithLocation(8, 13)
                );
        }

        [Fact]
        public void DynamicInvocation_Constructor_01()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new Test(d);
        new Test(d, 1);
        new Test(d, 2, 3);
        new Test(2, d, 3);
        new Test(2, 3, d);
        new Test(d, [3, 4]);

        new Test2(d);
        new Test2(d, d);
        new Test2(d, 1);
        new Test2(d, 2, 3);
        new Test2(2, d, 3);
        new Test2(2, 3, d);
        new Test2(d, [3, 4]);
    }

    class Test
    {
        public Test(int a, params IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
    }

    class Test2
    {
        public Test2(int a, params int[] b)
        {
            System.Console.Write("Called2");
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"CalledCalledCalledCalledCalledCalledCalled2Called2Called2Called2Called2Called2Called2").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Constructor_02_AmbiguousDynamicParamsArgument()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new Test(d);
    }

    class Test
    {
        public Test(params IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,18): error CS9502: Ambiguity between expanded and normal forms of non-array params collection parameter of 'Program.Test.Test(params IEnumerable<int>)', the only corresponding argument has the type 'dynamic'. Consider casting the dynamic argument.
                //         new Test(d);
                Diagnostic(ErrorCode.ERR_ParamsCollectionAmbiguousDynamicArgument, "d").WithArguments("Program.Test.Test(params System.Collections.Generic.IEnumerable<int>)").WithLocation(8, 18)
                );
        }

        [Fact]
        public void DynamicInvocation_Constructor_03_Warning()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d1 = System.DateTime.Now;
        new Test1(d1);                  // Called2
        
        dynamic d2 = new[] { 1 };
        new Test1(d2);                  // Called1
        new Test2(1, d1);               // Called3
        new Test2(1, d2);               // Called5
        
        int x = 1;
        new Test2(x, d1);               // Called3
        new Test2(x, d2);               // Called4

        dynamic d3 = (byte)1;
        new Test3(d3, 1, 2);            // Called7
        new Test3(d3, x, x);            // Called6

        dynamic d4 = x;
        new Test4((byte)d3, x, x);      // Called8
        new Test4(d3, x, x);            // Called9
        new Test4(d3, d4, d4);          // Called9
    }

    class Test1
    {
        public Test1(params IEnumerable<int> b) => System.Console.Write("Called1");
        public Test1(System.DateTime b) => System.Console.Write("Called2");
    }

    class Test2
    {
        public Test2(int x, System.DateTime b) => System.Console.Write("Called3");
        public Test2(long x, IEnumerable<int> b) => System.Console.Write("Called4");
        public Test2(byte x, params IEnumerable<int> b) => System.Console.Write("Called5");
    }

    class Test3
    {
        public Test3(byte x, params IEnumerable<int> b) => System.Console.Write("Called6");
        public Test3(byte x, byte y, byte z) => System.Console.Write("Called7");
    }

    class Test4
    {
        public Test4(byte x, params IEnumerable<int> b) => System.Console.Write("Called8");
        public Test4(byte x, long y, long z) => System.Console.Write("Called9");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called2Called1Called3Called5Called3Called4Called7Called6Called8Called9Called9").
            VerifyDiagnostics(
                // (8,9): warning CS9505: One or more constructor overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         new Test1(d1);                  // Called2
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionConstructor, "new Test1(d1)").WithLocation(8, 9),
                // (11,9): warning CS9505: One or more constructor overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         new Test1(d2);                  // Called1
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionConstructor, "new Test1(d2)").WithLocation(11, 9),
                // (12,9): warning CS9505: One or more constructor overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         new Test2(1, d1);               // Called3
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionConstructor, "new Test2(1, d1)").WithLocation(12, 9),
                // (13,9): warning CS9505: One or more constructor overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         new Test2(1, d2);               // Called5
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionConstructor, "new Test2(1, d2)").WithLocation(13, 9),
                // (20,9): warning CS9505: One or more constructor overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         new Test3(d3, 1, 2);            // Called7
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionConstructor, "new Test3(d3, 1, 2)").WithLocation(20, 9),
                // (25,9): warning CS9505: One or more constructor overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         new Test4(d3, x, x);            // Called9
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionConstructor, "new Test4(d3, x, x)").WithLocation(25, 9),
                // (26,9): warning CS9505: One or more constructor overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         new Test4(d3, d4, d4);          // Called9
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionConstructor, "new Test4(d3, d4, d4)").WithLocation(26, 9)
                );
        }

        [Fact]
        public void DynamicInvocation_Constructor_04()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new Test(d, 2);
    }

    class Test
    {
        public Test(int a, params IEnumerable<int> b)
        {
            System.Console.Write("Called {0}", b is not null);
        }

        public Test(int a, System.DateTime b)
        {
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called True").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Constructor_05()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new Test(d, 2, 3);
    }

    class Test
    {
        public Test(params IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_Constructor_14_DoNotFilterBasedOnBetterFunctionMember()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1L;
        new Test(1, d, 2);
    }
}

class Test
{
    public Test(long a1, params IEnumerable<long> a2)
    {
        System.Console.Write("long");
    }

    public Test(int b1, int b2, int b3)
    {
        System.Console.Write("int");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,9): warning CS9505: One or more constructor overloads having non-array params collection parameter might be applicable only in expanded form which is not supported during dynamic dispatch.
                //         new Test(1, d, 2);
                Diagnostic(ErrorCode.WRN_DynamicDispatchToParamsCollectionConstructor, "new Test(1, d, 2)").WithLocation(8, 9)
                );
        }

        [Fact]
        public void DynamicInvocation_Constructor_16_Abstract()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new Test(d);
    }

    abstract class Test
    {
        public Test(int a, params IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (8,9): error CS0144: Cannot create an instance of the abstract type or interface 'Program.Test'
                //         new Test(d);
                Diagnostic(ErrorCode.ERR_NoNewAbstract, "new Test(d)").WithArguments("Program.Test").WithLocation(8, 9)
                );
        }

        [Fact]
        public void DynamicInvocation_ConstructorInitializer_01()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new C01(d);
        new C02(d);
        new C03(d);
        new C04(d);
        new C05(d);
        new C06(d);

        new C07(d);
        new C09(d);
        new C10(d);
        new C11(d);
        new C12(d);
        new C13(d);
    }

    class C01(dynamic d) : Test(d);
    class C02(dynamic d) : Test(d, 1);
    class C03(dynamic d) : Test(d, 2, 3);
    class C04(dynamic d) : Test(2, d, 3);
    class C05(dynamic d) : Test(2, 3, d);
    class C06(dynamic d) : Test(d, [3, 4]);

    class C07(dynamic d) : Test2(d);
    class C09(dynamic d) : Test2(d, 1);
    class C10(dynamic d) : Test2(d, 2, 3);
    class C11(dynamic d) : Test2(2, d, 3);
    class C12(dynamic d) : Test2(2, 3, d);
    class C13(dynamic d) : Test2(d, [3, 4]);

    class Test
    {
        public Test(int a, params IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
    }

    class Test2
    {
        public Test2(int a, params int[] b)
        {
            System.Console.Write("Called2");
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);
            CompileAndVerify(
                comp,
                expectedOutput: @"CalledCalledCalledCalledCalledCalledCalled2Called2Called2Called2Called2Called2").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_ConstructorInitializer_02_AmbiguousDynamicParamsArgument()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new C(d);
    }

    class C(dynamic d) : Test(d);

    class Test
    {
        public Test(params IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (11,30): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C(dynamic d) : Test(d);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(d)").WithLocation(11, 30)
                );
        }

        [Fact]
        public void DynamicInvocation_ConstructorInitializer_03_MultipleCandidates()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d1 = System.DateTime.Now;
        new C01(d1);
        
        dynamic d2 = new[] { 1 };
        new C02(d2);
        new C03(d1);
        new C04(d2);
        
        int x = 1;
        new C05(x, d1);
        new C06(x, d2);

        dynamic d3 = (byte)1;
        new C07(d3);
        new C08(d3, x);

        dynamic d4 = x;
        new C09(d3, x);
        new C10(d3, x);
        new C11(d3, d4);
    }

    class C01(dynamic d1) : Test1(d1);
        
    class C02(dynamic d2) : Test1(d2);
    class C03(dynamic d1) : Test2(1, d1);
    class C04(dynamic d2) : Test2(1, d2);
        
    class C05(int x, dynamic d1) : Test2(x, d1);
    class C06(int x, dynamic d2) : Test2(x, d2);

    class C07(dynamic d3) : Test3(d3, 1, 2);
    class C08(int x, dynamic d3) : Test3(d3, x, x);            // Called6

    class C09(dynamic d3, int x) : Test4((byte)d3, x, x);      // Called8
    class C10(dynamic d3, int x) : Test4(d3, x, x);
    class C11(dynamic d3, dynamic d4) : Test4(d3, d4, d4);

    class Test1
    {
        public Test1(params IEnumerable<int> b) => System.Console.Write("Called1");
        public Test1(System.DateTime b) => System.Console.Write("Called2");
    }

    class Test2
    {
        public Test2(int x, System.DateTime b) => System.Console.Write("Called3");
        public Test2(long x, IEnumerable<int> b) => System.Console.Write("Called4");
        public Test2(byte x, params IEnumerable<int> b) => System.Console.Write("Called5");
    }

    class Test3
    {
        public Test3(byte x, params IEnumerable<int> b) => System.Console.Write("Called6");
        public Test3(byte x, byte y, byte z) => System.Console.Write("Called7");
    }

    class Test4
    {
        public Test4(byte x, params IEnumerable<int> b) => System.Console.Write("Called8");
        public Test4(byte x, long y, long z) => System.Console.Write("Called9");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (29,34): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C01(dynamic d1) : Test1(d1);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(d1)").WithLocation(29, 34),
                // (31,34): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C02(dynamic d2) : Test1(d2);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(d2)").WithLocation(31, 34),
                // (32,34): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C03(dynamic d1) : Test2(1, d1);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(1, d1)").WithLocation(32, 34),
                // (33,34): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C04(dynamic d2) : Test2(1, d2);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(1, d2)").WithLocation(33, 34),
                // (35,41): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C05(int x, dynamic d1) : Test2(x, d1);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(x, d1)").WithLocation(35, 41),
                // (36,41): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C06(int x, dynamic d2) : Test2(x, d2);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(x, d2)").WithLocation(36, 41),
                // (38,34): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C07(dynamic d3) : Test3(d3, 1, 2);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(d3, 1, 2)").WithLocation(38, 34),
                // (42,41): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C10(dynamic d3, int x) : Test4(d3, x, x);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(d3, x, x)").WithLocation(42, 41),
                // (43,46): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                //     class C11(dynamic d3, dynamic d4) : Test4(d3, d4, d4);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(d3, d4, d4)").WithLocation(43, 46)
                );
        }

        [Fact]
        public void DynamicInvocation_ConstructorInitializer_04()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new C(d);
    }

    class C(dynamic d) : Test(d, 2);

    class Test
    {
        public Test(int a, params IEnumerable<int> b)
        {
            System.Console.Write("Called {0}", b is not null);
        }

        public Test(int a, System.DateTime b)
        {
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called True").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_ConstructorInitializer_05()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1;
        new C(d);
    }

    class C(dynamic d) : Test(d, 2, 3);

    class Test
    {
        public Test(params IEnumerable<int> b)
        {
            System.Console.Write("Called");
        }
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            CompileAndVerify(
                comp,
                expectedOutput: @"Called").VerifyDiagnostics();
        }

        [Fact]
        public void DynamicInvocation_ConstructorInitializer_14_DoNotFilterBasedOnBetterFunctionMember()
        {
            var src = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        dynamic d = 1L;
        new C(d);
    }
}

class C(dynamic d) : Test(1, d, 2);

class Test
{
    public Test(long a1, params IEnumerable<long> a2)
    {
        System.Console.Write("long");
    }

    public Test(int b1, int b2, int b3)
    {
        System.Console.Write("int");
    }
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.StandardAndCSharp, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (12,26): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                // class C(dynamic d) : Test(1, d, 2);
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(1, d, 2)").WithLocation(12, 26)
                );
        }

        [Fact]
        public void ExpressionTree()
        {
            var src = @"
using System.Linq.Expressions;

class Program
{
    static void Main()
    {
        Expression<System.Action> e1 = () => Test();
        Expression<System.Action> e2 = () => Test(1);
        Expression<System.Action> e3 = () => Test(2, 3);
        Expression<System.Action> e4 = () => Test([]);
    }

    static void Test(params System.Collections.Generic.IEnumerable<long> a)
    {
    }
}
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80, options: TestOptions.ReleaseExe);

            // PROTOTYPE(ParamsCollections): report more specific error.
            comp.VerifyDiagnostics(
                // (8,46): error CS9175: An expression tree may not contain a collection expression.
                //         Expression<System.Action> e1 = () => Test();
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsCollectionExpression, "Test()").WithLocation(8, 46),
                // (9,46): error CS9175: An expression tree may not contain a collection expression.
                //         Expression<System.Action> e2 = () => Test(1);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsCollectionExpression, "Test(1)").WithLocation(9, 46),
                // (10,46): error CS9175: An expression tree may not contain a collection expression.
                //         Expression<System.Action> e3 = () => Test(2, 3);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsCollectionExpression, "Test(2, 3)").WithLocation(10, 46),
                // (11,51): error CS9175: An expression tree may not contain a collection expression.
                //         Expression<System.Action> e4 = () => Test([]);
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsCollectionExpression, "[]").WithLocation(11, 51)
                );
        }
    }
}
