// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class SafeFixedSizeBufferTests : CompilingTestBase
    {
        public const string InlineArrayAttributeDefinition =
@"
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class InlineArrayAttribute : Attribute
    {
        public InlineArrayAttribute (int length)
        {
            Length = length;
        }

        public int Length { get; }
    }
}";

        /* Equivalent C# code:

using System.Diagnostics.CodeAnalysis;

[System.Runtime.CompilerServices.InlineArray(10)]
public struct Buffer10<T>
{
    private T _element0;

    [UnscopedRef]
    public System.Span<T> AsSpan()
    {
        return System.Runtime.InteropServices.MemoryMarshal.CreateSpan(ref _element0, 10);
    }

    [UnscopedRef]
    public readonly System.ReadOnlySpan<T> AsReadOnlySpan()
    {
        return System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpan(ref _element0, 10);  
    }
}
         */
        public const string Buffer10IL =
@"
.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 7:0:0:0
}
.assembly extern System.Memory
{
  .publickeytoken = (CC 7B 13 FF CD 2D DD 51 )                         // .{...-.Q
  .ver 7:0:0:0
}

.assembly '<<GeneratedFileName>>'
{
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.module '<<GeneratedFileName>>.dll'
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY

.class private auto ansi '<Module>'
{
} // end of class <Module>

.class public sequential ansi sealed beforefieldinit Buffer10`1<T>
    extends [System.Runtime]System.ValueType
{
    .custom instance void System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 0a 00 00 00 00 00
    )
    // Fields
    .field private !T _element0

    // Methods
    .method public hidebysig 
        instance valuetype [System.Runtime]System.Span`1<!T> AsSpan () cil managed 
    {
        .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x2067
        // Code size 14 (0xe)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: ldflda !0 valuetype Buffer10`1<!T>::_element0
        IL_0006: ldc.i4.s 10
        IL_0008: call valuetype [System.Runtime]System.Span`1<!!0> [System.Memory]System.Runtime.InteropServices.MemoryMarshal::CreateSpan<!T>(!!0&, int32)
        IL_000d: ret
    } // end of method Buffer10`1::AsSpan

    .method public hidebysig 
        instance valuetype [System.Runtime]System.ReadOnlySpan`1<!T> AsReadOnlySpan () cil managed 
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsReadOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.UnscopedRefAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x2076
        // Code size 14 (0xe)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: ldflda !0 valuetype Buffer10`1<!T>::_element0
        IL_0006: ldc.i4.s 10
        IL_0008: call valuetype [System.Runtime]System.ReadOnlySpan`1<!!0> [System.Memory]System.Runtime.InteropServices.MemoryMarshal::CreateReadOnlySpan<!T>(!!0&, int32)
        IL_000d: ret
    } // end of method Buffer10`1::AsReadOnlySpan

} // end of class Buffer10`1

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.InlineArrayAttribute
    extends [System.Runtime]System.Attribute
{
    .custom instance void [System.Runtime]System.AttributeUsageAttribute::.ctor(valuetype [System.Runtime]System.AttributeTargets) = (
        01 00 08 00 00 00 01 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00
    )
    // Fields
    .field private initonly int32 '<Length>k__BackingField'
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
        01 00 00 00
    )

    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 length
        ) cil managed 
    {
        // Method begins at RVA 0x2079
        // Code size 14 (0xe)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Attribute::.ctor()
        IL_0006: ldarg.0
        IL_0007: ldarg.1
        IL_0008: stfld int32 System.Runtime.CompilerServices.InlineArrayAttribute::'<Length>k__BackingField'
        IL_000d: ret
    } // end of method InlineArrayAttribute::.ctor

    .method public hidebysig specialname 
        instance int32 get_Length () cil managed 
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x2088
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: ldfld int32 System.Runtime.CompilerServices.InlineArrayAttribute::'<Length>k__BackingField'
        IL_0006: ret
    } // end of method InlineArrayAttribute::get_Length

    // Properties
    .property instance int32 Length()
    {
        .get instance int32 System.Runtime.CompilerServices.InlineArrayAttribute::get_Length()
    }

} // end of class System.Runtime.CompilerServices.InlineArrayAttribute
";

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static void M2(C x) => x.F[0] = 111;
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref int System.Span<int>.this[int].get""
  IL_0014:  ldind.i4
  IL_0015:  ret
}
");

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref int System.Span<int>.this[int].get""
  IL_0014:  ldc.i4.s   111
  IL_0016:  stind.i4
  IL_0017:  ret
}
");
#if false // PROTOTYPE(SafeFixedSizeBuffers):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M1").Single();
            var m1Operation = model.GetOperation(m1);
            VerifyOperationTree(comp, m1Operation,
@"
");

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_02()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(x) = 111;
        System.Console.Write(x.F[0]);
    }

    static ref int M2(C x) => ref x.F[0];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref int System.Span<int>.this[int].get""
  IL_0014:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_03()
        {
            var src = @"
class C
{
    public Buffer10<int> F;

    public ref int M2() => ref F[0];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2() = 111;
        System.Console.Write(x.F[0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref int System.Span<int>.this[int].get""
  IL_0014:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_04()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(x) = 111;
        System.Console.Write(x.F[0]);
    }

    static ref int M2(C x) => ref x.F[0];

    static ref int M3(C x)
    {
        ref int y = ref x.F[0];
        return ref y;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (16,35): error CS8167: Cannot return by reference a member of parameter 'x' because it is not a ref or out parameter
                //     static ref int M2(C x) => ref x.F[0];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "x").WithArguments("x").WithLocation(16, 35),
                // (21,20): error CS8157: Cannot return 'y' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref y;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "y").WithArguments("y").WithLocation(21, 20)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_05()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public ref int M2() => ref F[0];

    public ref int M3()
    {
        ref int y = ref F[0];
        return ref y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2() = 111;
        System.Console.Write(x.F[0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,32): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public ref int M2() => ref F[0];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F[0]").WithLocation(6, 32),
                // (11,20): error CS8157: Cannot return 'y' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref y;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "y").WithArguments("y").WithLocation(11, 20)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_06()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x) = 111;
        System.Console.Write(x.F[0]);
    }

    static ref int M2(ref C x) => ref x.F[0];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref int System.Span<int>.this[int].get""
  IL_0014:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_07()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public ref int M2() => ref F[0];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2() = 111;
        System.Console.Write(x.F[0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref int System.Span<int>.this[int].get""
  IL_0014:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_08()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x) = 111;
        System.Console.Write(x.F[0]);
    }

    static ref int M2(ref C x)
    {
        ref int y = ref x.F[0];
        return ref y;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref int System.Span<int>.this[int].get""
  IL_0014:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_09()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public ref int M2()
    {
        ref int y = ref F[0];
        return ref y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2() = 111;
        System.Console.Write(x.F[0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref int System.Span<int>.this[int].get""
  IL_0014:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_10()
        {
            var src = @"
class C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0][0];
    static void M2(C x) => x.F[0][0] = 111;
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_11()
        {
            var src = @"
class C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(x) = 111;
        System.Console.Write(x.F[0][0]);
    }

    static ref int M2(C x) => ref x.F[0][0];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_12()
        {
            var src = @"
class C
{
    public Buffer10<Buffer10<int>> F;

    public ref int M2() => ref F[0][0];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2() = 111;
        System.Console.Write(x.F[0][0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_13()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(x) = 111;
        System.Console.Write(x.F[0][0]);
    }

    static ref int M2(C x) => ref x.F[0][0];

    static ref int M3(C x)
    {
        ref int y = ref x.F[0][0];
        return ref y;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (16,35): error CS8167: Cannot return by reference a member of parameter 'x' because it is not a ref or out parameter
                //     static ref int M2(C x) => ref x.F[0][0];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "x").WithArguments("x").WithLocation(16, 35),
                // (21,20): error CS8157: Cannot return 'y' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref y;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "y").WithArguments("y").WithLocation(21, 20)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_14()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public ref int M2() => ref F[0][0];

    public ref int M3()
    {
        ref int y = ref F[0][0];
        return ref y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2() = 111;
        System.Console.Write(x.F[0][0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,32): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public ref int M2() => ref F[0][0];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F[0][0]").WithLocation(6, 32),
                // (11,20): error CS8157: Cannot return 'y' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref y;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "y").WithArguments("y").WithLocation(11, 20)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_15()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x) = 111;
        System.Console.Write(x.F[0][0]);
    }

    static ref int M2(ref C x) => ref x.F[0][0];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_16()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public ref int M2() => ref F[0][0];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2() = 111;
        System.Console.Write(x.F[0][0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_17()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x) = 111;
        System.Console.Write(x.F[0][0]);
    }

    static ref int M2(ref C x)
    {
        ref int y = ref x.F[0][0];
        return ref y;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_18()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public ref int M2()
    {
        ref int y = ref F[0][0];
        return ref y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2() = 111;
        System.Console.Write(x.F[0][0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Index_Variable_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[^10];
    static void M2(C x) => x.F[^10] = 111;
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M1",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref int System.Span<int>.this[int].get""
  IL_0014:  ldind.i4
  IL_0015:  ret
}
");

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref int System.Span<int>.this[int].get""
  IL_0014:  ldc.i4.s   111
  IL_0016:  stind.i4
  IL_0017:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Dynamic_Variable_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static void M2(C x) => x.F[(dynamic)0] = 111;
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111").VerifyDiagnostics();

            // PROTOTYPE(SafeFixedSizeBuffers): Dynamic index is always converted to 'int'. Confirm this is what we want.
            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       93 (0x5d)
  .maxstack  4
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> Program.<>o__2.<>p__0""
  IL_0013:  brtrue.s   IL_003a
  IL_0015:  ldc.i4.s   32
  IL_0017:  ldtoken    ""int""
  IL_001c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0021:  ldtoken    ""Program""
  IL_0026:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_002b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.Convert(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Type, System.Type)""
  IL_0030:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0035:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> Program.<>o__2.<>p__0""
  IL_003a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> Program.<>o__2.<>p__0""
  IL_003f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>>.Target""
  IL_0044:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>> Program.<>o__2.<>p__0""
  IL_0049:  ldc.i4.0
  IL_004a:  box        ""int""
  IL_004f:  callvirt   ""int System.Func<System.Runtime.CompilerServices.CallSite, dynamic, int>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0054:  call       ""ref int System.Span<int>.this[int].get""
  IL_0059:  ldc.i4.s   111
  IL_005b:  stind.i4
  IL_005c:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x)[0] = 111;
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static System.Span<int> M2(C x) => x.F[..5];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.5
  IL_0010:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0015:  ret
}
");
#if false // PROTOTYPE(SafeFixedSizeBuffers):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_03()
        {
            var src = @"
class C
{
    public Buffer10<int> F;

    public System.Span<int> M2() => F[..5];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0] = 111;
        System.Console.Write(x.F[0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.5
  IL_0010:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0015:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_04()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(x)[0] = 111;
        System.Console.Write(x.F[0]);
    }

    static int M1(C x) => x.F[0];
    static System.Span<int> M2(C x) => x.F[..5];
    static System.Span<int> M3(C x)
    { 
        System.Span<int> y = x.F[..5];
        return y;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (17,40): error CS8167: Cannot return by reference a member of parameter 'x' because it is not a ref or out parameter
                //     static System.Span<int> M2(C x) => x.F[..5];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "x").WithArguments("x").WithLocation(17, 40),
                // (21,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(21, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_05()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public System.Span<int> M2() => F[..5];

    public System.Span<int> M3()
    { 
        System.Span<int> y = F[..5];
        return y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0] = 111;
        System.Console.Write(x.F[0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,37): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public System.Span<int> M2() => F[..5];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F[..5]").WithLocation(6, 37),
                // (11,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(11, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_06()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x)[0] = 111;
        System.Console.Write(x.F[0]);
    }

    static int M1(C x) => x.F[0];
    static System.Span<int> M2(ref C x) => x.F[..5];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.5
  IL_0010:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0015:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_07()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.Span<int> M2() => F[..5];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0] = 111;
        System.Console.Write(x.F[0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.5
  IL_0010:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0015:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_08()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x)[0] = 111;
        System.Console.Write(x.F[0]);
    }

    static int M1(C x) => x.F[0];
    static System.Span<int> M2(ref C x)
    {
        System.Span<int> y = x.F[..5];
        return y;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.5
  IL_0010:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0015:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_09()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.Span<int> M2()
    {
        System.Span<int> y = F[..5];
        return y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0] = 111;
        System.Console.Write(x.F[0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.5
  IL_0010:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0015:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_10()
        {
            var src = @"
class C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x)[0][0] = 111;
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0][0];
    static System.Span<Buffer10<int>> M2(C x) => x.F[..5][..3];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_12()
        {
            var src = @"
class C
{
    public Buffer10<Buffer10<int>> F;

    public System.Span<Buffer10<int>> M2() => F[..5][..3];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0][0] = 111;
        System.Console.Write(x.F[0][0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_13()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(x)[0][0] = 111;
        System.Console.Write(x.F[0][0]);
    }

    static int M1(C x) => x.F[0][0];
    static System.Span<Buffer10<int>> M2(C x) => x.F[..5][..3];
    static System.Span<Buffer10<int>> M3(C x)
    { 
        System.Span<Buffer10<int>> y = x.F[..5][..3];
        return y;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (17,50): error CS8167: Cannot return by reference a member of parameter 'x' because it is not a ref or out parameter
                //     static System.Span<Buffer10<int>> M2(C x) => x.F[..5][..3];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "x").WithArguments("x").WithLocation(17, 50),
                // (21,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(21, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_14()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public System.Span<Buffer10<int>> M2() => F[..5][..3];

    public System.Span<Buffer10<int>> M3()
    { 
        System.Span<Buffer10<int>> y = F[..5][..3];
        return y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0][0] = 111;
        System.Console.Write(x.F[0][0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,47): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public System.Span<Buffer10<int>> M2() => F[..5][..3];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F[..5]").WithLocation(6, 47),
                // (11,16): error CS8352: Cannot use variable 'y' in this context because it may expose referenced variables outside of their declaration scope
                //         return y;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "y").WithArguments("y").WithLocation(11, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_15()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x)[0][0] = 111;
        System.Console.Write(x.F[0][0]);
    }

    static int M1(C x) => x.F[0][0];
    static System.Span<Buffer10<int>> M2(ref C x) => x.F[..5][..3];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_16()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.Span<Buffer10<int>> M2() => F[..5][..3];
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0][0] = 111;
        System.Console.Write(x.F[0][0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_17()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        M2(ref x)[0][0] = 111;
        System.Console.Write(x.F[0][0]);
    }

    static int M1(C x) => x.F[0][0];
    static System.Span<Buffer10<int>> M2(ref C x)
    {
        System.Span<Buffer10<int>> y = x.F[..5][..3];
        return y;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_18()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public System.Span<Buffer10<int>> M2()
    {
        System.Span<Buffer10<int>> y = F[..5][..3];
        return y;
    }
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.M2()[0][0] = 111;
        System.Console.Write(x.F[0][0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_IsRValue()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.F[..5] = default;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (12,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         x.F[..5] = default;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "x.F[..5]").WithLocation(12, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Range_Variable_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        System.Console.Write(M1(x));
        M2(x, ..5)[0] = 111;
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static System.Span<int> M2(C x, System.Range y) => GetBuffer(x)[GetRange(y)];
    static ref Buffer10<int> GetBuffer(C x) => ref x.F;
    static System.Range GetRange(System.Range y) => y;
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "0 111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       68 (0x44)
  .maxstack  3
  .locals init (System.Range V_0,
                int V_1,
                int V_2,
                System.Index V_3,
                System.Span<int> V_4)
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref Buffer10<int> Program.GetBuffer(C)""
  IL_0006:  ldarg.1
  IL_0007:  call       ""System.Range Program.GetRange(System.Range)""
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       ""System.Index System.Range.Start.get""
  IL_0014:  stloc.3
  IL_0015:  ldloca.s   V_3
  IL_0017:  ldc.i4.s   10
  IL_0019:  call       ""int System.Index.GetOffset(int)""
  IL_001e:  stloc.1
  IL_001f:  ldloca.s   V_0
  IL_0021:  call       ""System.Index System.Range.End.get""
  IL_0026:  stloc.3
  IL_0027:  ldloca.s   V_3
  IL_0029:  ldc.i4.s   10
  IL_002b:  call       ""int System.Index.GetOffset(int)""
  IL_0030:  ldloc.1
  IL_0031:  sub
  IL_0032:  stloc.2
  IL_0033:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_0038:  stloc.s    V_4
  IL_003a:  ldloca.s   V_4
  IL_003c:  ldloc.1
  IL_003d:  ldloc.2
  IL_003e:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0043:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ObjectInitializer_Int()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        System.Console.Write(M2().F[0]);
    }

    static C M2() => new C() { F = {[0] = 111} };
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);

            // PROTOTYPE(SafeFixedSizeBuffers): Should work?
            comp.VerifyDiagnostics(
                // (14,37): error CS1913: Member '[0]' cannot be initialized. It is not a field or property.
                //     static C M2() => new C() { F = {[0] = 111} };
                Diagnostic(ErrorCode.ERR_MemberCannotBeInitialized, "[0]").WithArguments("[0]").WithLocation(14, 37)
                );

#if false // PROTOTYPE(SafeFixedSizeBuffers):
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
}
");
#endif

#if false // PROTOTYPE(SafeFixedSizeBuffers):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ConditionalAccess_Variable()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var c = new C();
        c.F[0] = 111;
        System.Console.Write(M2(c));
    }

    static int? M2(C c) => c?.F[0];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (int? V_0,
                System.Span<int> V_1)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000d
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    ""int?""
  IL_000b:  ldloc.0
  IL_000c:  ret
  IL_000d:  ldarg.0
  IL_000e:  ldflda     ""Buffer10<int> C.F""
  IL_0013:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_0018:  stloc.1
  IL_0019:  ldloca.s   V_1
  IL_001b:  ldc.i4.0
  IL_001c:  call       ""ref int System.Span<int>.this[int].get""
  IL_0021:  ldind.i4
  IL_0022:  newobj     ""int?..ctor(int)""
  IL_0027:  ret
}
");

#if false // PROTOTYPE(SafeFixedSizeBuffers):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ConditionalAccess_Variable()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var c = new C();
        c.F[0] = 111;
        System.Console.Write(M2(c));
    }

    static int? M2(C c) => c?.F[..5][0];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       50 (0x32)
  .maxstack  3
  .locals init (int? V_0,
            System.Span<int> V_1)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000d
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    ""int?""
  IL_000b:  ldloc.0
  IL_000c:  ret
  IL_000d:  ldarg.0
  IL_000e:  ldflda     ""Buffer10<int> C.F""
  IL_0013:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_0018:  stloc.1
  IL_0019:  ldloca.s   V_1
  IL_001b:  ldc.i4.0
  IL_001c:  ldc.i4.5
  IL_001d:  call       ""System.Span<int> System.Span<int>.Slice(int, int)""
  IL_0022:  stloc.1
  IL_0023:  ldloca.s   V_1
  IL_0025:  ldc.i4.0
  IL_0026:  call       ""ref int System.Span<int>.this[int].get""
  IL_002b:  ldind.i4
  IL_002c:  newobj     ""int?..ctor(int)""
  IL_0031:  ret
}
");

#if false // PROTOTYPE(SafeFixedSizeBuffers):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ConditionalAccess_Value_01()
        {
            var src = @"
class C
{
    public Buffer10<int>? F;
}

class Program
{
    static void Main()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        var c = new C() { F = b };
        System.Console.Write(M2(c));
    }

    static int? M2(C c) => c.F?[0];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (int? V_0,
            Buffer10<int> V_1,
            System.ReadOnlySpan<int> V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int>? C.F""
  IL_0006:  dup
  IL_0007:  call       ""readonly bool Buffer10<int>?.HasValue.get""
  IL_000c:  brtrue.s   IL_0019
  IL_000e:  pop
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""int?""
  IL_0017:  ldloc.0
  IL_0018:  ret
  IL_0019:  call       ""readonly Buffer10<int> Buffer10<int>?.GetValueOrDefault()""
  IL_001e:  stloc.1
  IL_001f:  ldloca.s   V_1
  IL_0021:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_0026:  stloc.2
  IL_0027:  ldloca.s   V_2
  IL_0029:  ldc.i4.0
  IL_002a:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_002f:  ldind.i4
  IL_0030:  newobj     ""int?..ctor(int)""
  IL_0035:  ret
}
");

#if false // PROTOTYPE(SafeFixedSizeBuffers):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ConditionalAccess_Value_02()
        {
            var src = @"
class C
{
    public Buffer10<int>? F;
}

class Program
{
    static void Main()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        var c = new C() { F = b };
        System.Console.Write(M2(c));
    }

    static int? M2(C c) => c.F?[M3(default)];

    static int M3(Buffer10<int> x) => 0;
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       67 (0x43)
  .maxstack  2
  .locals init (int? V_0,
            Buffer10<int> V_1,
            System.ReadOnlySpan<int> V_2,
            Buffer10<int> V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int>? C.F""
  IL_0006:  dup
  IL_0007:  call       ""readonly bool Buffer10<int>?.HasValue.get""
  IL_000c:  brtrue.s   IL_0019
  IL_000e:  pop
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""int?""
  IL_0017:  ldloc.0
  IL_0018:  ret
  IL_0019:  call       ""readonly Buffer10<int> Buffer10<int>?.GetValueOrDefault()""
  IL_001e:  stloc.1
  IL_001f:  ldloca.s   V_1
  IL_0021:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_0026:  stloc.2
  IL_0027:  ldloca.s   V_2
  IL_0029:  ldloca.s   V_3
  IL_002b:  initobj    ""Buffer10<int>""
  IL_0031:  ldloc.3
  IL_0032:  call       ""int Program.M3(Buffer10<int>)""
  IL_0037:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_003c:  ldind.i4
  IL_003d:  newobj     ""int?..ctor(int)""
  IL_0042:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ConditionalAccess_Value_03()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        var c = new C() { F = b };
        System.Console.Write(M2(c));
    }

    static int? M2(C? c) => c?.F[0];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (int? V_0,
            Buffer10<int> V_1,
            System.ReadOnlySpan<int> V_2)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly bool C?.HasValue.get""
  IL_0007:  brtrue.s   IL_0013
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    ""int?""
  IL_0011:  ldloc.0
  IL_0012:  ret
  IL_0013:  ldarga.s   V_0
  IL_0015:  call       ""readonly C C?.GetValueOrDefault()""
  IL_001a:  ldfld      ""Buffer10<int> C.F""
  IL_001f:  stloc.1
  IL_0020:  ldloca.s   V_1
  IL_0022:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_0027:  stloc.2
  IL_0028:  ldloca.s   V_2
  IL_002a:  ldc.i4.0
  IL_002b:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0030:  ldind.i4
  IL_0031:  newobj     ""int?..ctor(int)""
  IL_0036:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ConditionalAccess_Value_04()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        var c = new C() { F = b };
        System.Console.Write(M2(c));
    }

    static int? M2(C? c) => c?.F[M3(default)];

    static int M3(Buffer10<int> x) => 0;
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       68 (0x44)
  .maxstack  2
  .locals init (int? V_0,
            Buffer10<int> V_1,
            System.ReadOnlySpan<int> V_2,
            Buffer10<int> V_3)
  IL_0000:  ldarga.s   V_0
  IL_0002:  call       ""readonly bool C?.HasValue.get""
  IL_0007:  brtrue.s   IL_0013
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    ""int?""
  IL_0011:  ldloc.0
  IL_0012:  ret
  IL_0013:  ldarga.s   V_0
  IL_0015:  call       ""readonly C C?.GetValueOrDefault()""
  IL_001a:  ldfld      ""Buffer10<int> C.F""
  IL_001f:  stloc.1
  IL_0020:  ldloca.s   V_1
  IL_0022:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_0027:  stloc.2
  IL_0028:  ldloca.s   V_2
  IL_002a:  ldloca.s   V_3
  IL_002c:  initobj    ""Buffer10<int>""
  IL_0032:  ldloc.3
  IL_0033:  call       ""int Program.M3(Buffer10<int>)""
  IL_0038:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_003d:  ldind.i4
  IL_003e:  newobj     ""int?..ctor(int)""
  IL_0043:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ConditionalAccess_Value_01()
        {
            var src = @"
class C
{
    public Buffer10<int>? F;
}

class Program
{
    static void Main()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        var c = new C() { F = b };
        System.Console.Write(M2(c));
    }

    static int? M2(C c) => c.F?[..5][0];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (17,28): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     static int? M2(C c) => c.F?[..5][0];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "c.F?").WithLocation(17, 28)
                );

#if false // PROTOTYPE(SafeFixedSizeBuffers):
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            var m2Operation = model.GetOperation(m2);
            VerifyOperationTree(comp, m2Operation,
@"
");
#endif
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ConditionalAccess_Value_02()
        {
            var src = @"
class C
{
    public Buffer10<int>? F;
}

class Program
{
    static void Main()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        var c = new C() { F = b };
        System.Console.Write(M2(c));
    }

    static int? M2(C c) => c.F?[M3(default)..][M3(default)];

    static int M3(Buffer10<int> x) => 0;
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (17,28): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     static int? M2(C c) => c.F?[M3(default)..][M3(default)];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "c.F?").WithLocation(17, 28)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ConditionalAccess_Value_03()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        var c = new C() { F = b };
        System.Console.Write(M2(c));
    }

    static int? M2(C? c) => c?.F[..5][0];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (17,31): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     static int? M2(C? c) => c?.F[..5][0];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, ".F").WithLocation(17, 31)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ConditionalAccess_Value_04()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        var c = new C() { F = b };
        System.Console.Write(M2(c));
    }

    static int? M2(C? c) => c?.F[M3(default)..][M3(default)];

    static int M3(Buffer10<int> x) => 0;
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (17,31): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     static int? M2(C? c) => c?.F[M3(default)..][M3(default)];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, ".F").WithLocation(17, 31)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_NotValue()
        {
            var src = @"
class Program
{
    static void Main()
    {
        _ = Buffer10<int>[0];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,13): error CS0119: 'Buffer10<int>' is a type, which is not valid in the given context
                //         _ = Buffer10<int>[0];
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Buffer10<int>").WithArguments("Buffer10<int>", "type").WithLocation(6, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Value_01()
        {
            var src = @"
class Program
{
    static void Main()
    {
        System.Console.Write(M2());
    }

    static int M2() => M4(M3()[0], default);

    static Buffer10<int> M3()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        return b;
    }

    static int M4(in int x, Buffer10<int> y)
    {
        return x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
   // Code size       41 (0x29)
  .maxstack  2
  .locals init (Buffer10<int> V_0,
                System.ReadOnlySpan<int> V_1,
                int V_2)
  IL_0000:  call       ""Buffer10<int> Program.M3()""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_1
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0016:  ldind.i4
  IL_0017:  stloc.2
  IL_0018:  ldloca.s   V_2
  IL_001a:  ldloca.s   V_0
  IL_001c:  initobj    ""Buffer10<int>""
  IL_0022:  ldloc.0
  IL_0023:  call       ""int Program.M4(in int, Buffer10<int>)""
  IL_0028:  ret
}
");

            // PROTOTYPE(SafeFixedSizeBuffers)
            //var tree = comp.SyntaxTrees.First();
            //var model = comp.GetSemanticModel(tree);

            //            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            //            var m2Operation = model.GetOperation(m2);
            //            VerifyOperationTree(comp, m2Operation,
            //@"
            //");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Value_02()
        {
            var src = @"
class Program
{
    static void Main()
    {
        System.Console.Write(M2());
    }

    static int M2() => M3()[M4(default)];

    static Buffer10<int> M3()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        return b;
    }

    static int M4(Buffer10<int> y)
    {
        return 0;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       37 (0x25)
  .maxstack  2
  .locals init (Buffer10<int> V_0,
            System.ReadOnlySpan<int> V_1,
            Buffer10<int> V_2)
  IL_0000:  call       ""Buffer10<int> Program.M3()""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_1
  IL_0010:  ldloca.s   V_2
  IL_0012:  initobj    ""Buffer10<int>""
  IL_0018:  ldloc.2
  IL_0019:  call       ""int Program.M4(Buffer10<int>)""
  IL_001e:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0023:  ldind.i4
  IL_0024:  ret
}
");

            // PROTOTYPE(SafeFixedSizeBuffers)
            //var tree = comp.SyntaxTrees.First();
            //var model = comp.GetSemanticModel(tree);

            //            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            //            var m2Operation = model.GetOperation(m2);
            //            VerifyOperationTree(comp, m2Operation,
            //@"
            //");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Value_03()
        {
            var src = @"
class Program
{
    static void Main()
    {
        System.Console.Write(M2());
    }

    static int M2()
    {
        ref readonly int x = M3()[0];
        return x;
    }

    static Buffer10<int> M3()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        return b;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (11,26): error CS8172: Cannot initialize a by-reference variable with a value
                //         ref readonly int x = M3()[0];
                Diagnostic(ErrorCode.ERR_InitializeByReferenceVariableWithValue, "x = M3()[0]").WithLocation(11, 26),
                // (11,30): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         ref readonly int x = M3()[0];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "M3()[0]").WithLocation(11, 30)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Value_04()
        {
            var src = @"
class Program
{
    static void Main()
    {
        System.Console.Write(M2());
    }

    static int M2()
    {
        return M4(ref M3()[0]);
    }

    static Buffer10<int> M3()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        return b;
    }

    static int M4(ref int x)
    {
        return x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (11,23): error CS1510: A ref or out value must be an assignable variable
                //         return M4(ref M3()[0]);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "M3()[0]").WithLocation(11, 23)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Value_05()
        {
            var src = @"
class Program
{
    static void Main()
    {
        System.Console.Write(M2());
    }

    static int M2()
    {
        return M4(in M3()[0]);
    }

    static Buffer10<int> M3()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        return b;
    }

    static int M4(in int x)
    {
        return x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                    // (11,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                    //         return M4(in M3()[0]);
                    Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "M3()[0]").WithLocation(11, 22)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Value_06()
        {
            var src = @"
class Program
{
    static void Main()
    {
        System.Console.Write(M2());
    }

    static int M2()
    {
        return M4(M3()[0]);
    }

    static Buffer10<int> M3()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        return b;
    }

    static int M4(in int x)
    {
        return x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (Buffer10<int> V_0,
                System.ReadOnlySpan<int> V_1,
                int V_2)
  IL_0000:  call       ""Buffer10<int> Program.M3()""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_1
  IL_0010:  ldc.i4.0
  IL_0011:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0016:  ldind.i4
  IL_0017:  stloc.2
  IL_0018:  ldloca.s   V_2
  IL_001a:  call       ""int Program.M4(in int)""
  IL_001f:  ret
}
");

            // PROTOTYPE(SafeFixedSizeBuffers)
            //var tree = comp.SyntaxTrees.First();
            //var model = comp.GetSemanticModel(tree);

            //            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            //            var m2Operation = model.GetOperation(m2);
            //            VerifyOperationTree(comp, m2Operation,
            //@"
            //");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_NotValue()
        {
            var src = @"
class Program
{
    static void Main()
    {
        _ = Buffer10<int>[..5];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (6,13): error CS0119: 'Buffer10<int>' is a type, which is not valid in the given context
                //         _ = Buffer10<int>[..5];
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Buffer10<int>").WithArguments("Buffer10<int>", "type").WithLocation(6, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Value_01()
        {
            var src = @"
class Program
{
    static void Main()
    {
        System.Console.Write(M2());
    }

    static int M2() => M4(M3()[..], default);

    static Buffer10<int> M3()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        return b;
    }

    static int M4(System.ReadOnlySpan<int> x, Buffer10<int> y)
    {
        return x[0];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (9,27): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //     static int M2() => M4(M3()[..], default);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "M3()").WithLocation(9, 27)
                );

            // PROTOTYPE(SafeFixedSizeBuffers)
            //var tree = comp.SyntaxTrees.First();
            //var model = comp.GetSemanticModel(tree);

            //            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            //            var m2Operation = model.GetOperation(m2);
            //            VerifyOperationTree(comp, m2Operation,
            //@"
            //");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_01()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(C c) => c.F[0];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0014:  ldind.i4
  IL_0015:  ret
}
");

            // PROTOTYPE(SafeFixedSizeBuffers)
            //            var tree = comp.SyntaxTrees.First();
            //            var model = comp.GetSemanticModel(tree);

            //            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            //            var m2Operation = model.GetOperation(m2);
            //            VerifyOperationTree(comp, m2Operation,
            //@"
            //");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_02()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(C c)
    {
        ref readonly int x = ref c.F[0];
        return x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0014:  ldind.i4
  IL_0015:  ret
}
");

            // PROTOTYPE(SafeFixedSizeBuffers)
            //            var tree = comp.SyntaxTrees.First();
            //            var model = comp.GetSemanticModel(tree);

            //            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            //            var m2Operation = model.GetOperation(m2);
            //            VerifyOperationTree(comp, m2Operation,
            //@"
            //");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_03()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(C c)
    {
        return M4(in c.F[0]);
    }

    static int M4(in int x)
    {
        return x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0014:  call       ""int Program.M4(in int)""
  IL_0019:  ret
}
");

            // PROTOTYPE(SafeFixedSizeBuffers)
            //            var tree = comp.SyntaxTrees.First();
            //            var model = comp.GetSemanticModel(tree);

            //            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(m => m.Identifier.ValueText == "M2").Single();
            //            var m2Operation = model.GetOperation(m2);
            //            VerifyOperationTree(comp, m2Operation,
            //@"
            //");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_04()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(C c)
    {
        return M4(ref c.F[0]);
    }

    static int M4(ref int x)
    {
        return x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (23,23): error CS8329: Cannot use method 'this.get' as a ref or out value because it is a readonly variable
                //         return M4(ref c.F[0]);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "c.F[0]").WithArguments("method", "this.get").WithLocation(23, 23)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_05()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static ref readonly int M2(C c)
    {
        ref readonly int x = ref c.F[0];
        return ref x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0014:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_06()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static ref readonly int M2(C c)
    {
        return ref c.F[0];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0014:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_07()
        {
            var src = @"
struct C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static ref readonly int M2(C c)
    {
        ref readonly int x = ref c.F[0];
        return ref x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (24,20): error CS8157: Cannot return 'x' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref x;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "x").WithArguments("x").WithLocation(24, 20)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_08()
        {
            var src = @"
struct C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static ref readonly int M2(C c)
    {
        return ref c.F[0];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (23,20): error CS8167: Cannot return by reference a member of parameter 'c' because it is not a ref or out parameter
                //         return ref c.F[0];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "c").WithArguments("c").WithLocation(23, 20)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_09()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly int M2() => F[0];
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2());
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0014:  ldind.i4
  IL_0015:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_10()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly int M2()
    {
        ref readonly int x = ref F[0];
        return x;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2());
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0014:  ldind.i4
  IL_0015:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_11()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly int M2()
    {
        return M4(in F[0]);
    }

    static int M4(in int x)
    {
        return x;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2());
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0014:  call       ""int C.M4(in int)""
  IL_0019:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_12()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly int M2()
    {
        return M4(ref F[0]);
    }

    static int M4(ref int x)
    {
        return x;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2());
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (15,23): error CS8329: Cannot use method 'this.get' as a ref or out value because it is a readonly variable
                //         return M4(ref F[0]);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "F[0]").WithArguments("method", "this.get").WithLocation(15, 23)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_13()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly ref readonly int M2()
    {
        ref readonly int x = ref F[0];
        return ref x;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2());
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (16,20): error CS8157: Cannot return 'x' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref x;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "x").WithArguments("x").WithLocation(16, 20)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_14()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public readonly ref readonly int M2()
    {
        ref readonly int x = ref F[0];
        return ref x;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2());
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0014:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_15()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly ref readonly int M2()
    {
        return ref F[0];
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2());
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (15,20): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //         return ref F[0];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F[0]").WithLocation(15, 20)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_16()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public readonly ref readonly int M2()
    {
        return ref F[0];
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2());
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0014:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_17()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        c.F[0] = 1;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (18,9): error CS8331: Cannot assign to method 'this.get' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         c.F[0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.F[0]").WithArguments("method", "this.get").WithLocation(18, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_ReadonlyContext_18()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    readonly void Main()
    {
        F[0] = 1;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (15,9): error CS8331: Cannot assign to method 'this.get' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         F[0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F[0]").WithArguments("method", "this.get").WithLocation(15, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_01()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[..][0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c)[0]);
    }

    static System.ReadOnlySpan<int> M2(C c) => c.F[..5];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.5
  IL_0010:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.Slice(int, int)""
  IL_0015:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_02()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[..][0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c)[0]);
    }

    static System.ReadOnlySpan<int> M2(C c)
    {
        System.ReadOnlySpan<int> x = c.F[..5];
        return x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.5
  IL_0010:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.Slice(int, int)""
  IL_0015:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_04()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[..][0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(C c)
    {
        return M4(c.F[..]);
    }

    static int M4(System.Span<int> x)
    {
        return x[0];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (23,19): error CS1503: Argument 1: cannot convert from 'System.ReadOnlySpan<int>' to 'System.Span<int>'
                //         return M4(c.F[..]);
                Diagnostic(ErrorCode.ERR_BadArgType, "c.F[..]").WithArguments("1", "System.ReadOnlySpan<int>", "System.Span<int>").WithLocation(23, 19)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_07()
        {
            var src = @"
struct C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[..][0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c)[0]);
    }

    static System.ReadOnlySpan<int> M2(C c)
    {
        System.ReadOnlySpan<int> x = c.F[..5];
        return x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (24,16): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         return x;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(24, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_08()
        {
            var src = @"
struct C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[..][0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c)[0]);
    }

    static System.ReadOnlySpan<int> M2(C c)
    {
        return c.F[..5];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (23,16): error CS8167: Cannot return by reference a member of parameter 'c' because it is not a ref or out parameter
                //         return c.F[..5];
                Diagnostic(ErrorCode.ERR_RefReturnParameter2, "c").WithArguments("c").WithLocation(23, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_09()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly System.ReadOnlySpan<int> M2() => F[..5];
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2()[0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (13,54): error CS8170: Struct members cannot return 'this' or other instance members by reference
                //     public readonly System.ReadOnlySpan<int> M2() => F[..5];
                Diagnostic(ErrorCode.ERR_RefReturnStructThis, "F[..5]").WithLocation(13, 54)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_10()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly System.ReadOnlySpan<int> M2()
    {
        System.ReadOnlySpan<int> x = F[..5];
        return x;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2()[0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (16,16): error CS8352: Cannot use variable 'x' in this context because it may expose referenced variables outside of their declaration scope
                //         return x;
                Diagnostic(ErrorCode.ERR_EscapeVariable, "x").WithArguments("x").WithLocation(16, 16)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_12()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    public readonly int M2()
    {
        return M4(F[..]);
    }

    static int M4(System.Span<int> x)
    {
        return x[0];
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2());
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (15,19): error CS1503: Argument 1: cannot convert from 'System.ReadOnlySpan<int>' to 'System.Span<int>'
                //         return M4(F[..]);
                Diagnostic(ErrorCode.ERR_BadArgType, "F[..]").WithArguments("1", "System.ReadOnlySpan<int>", "System.Span<int>").WithLocation(15, 19)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_14()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public readonly System.ReadOnlySpan<int> M2()
    {
        System.ReadOnlySpan<int> x = F[..5];
        return x;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2()[0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.5
  IL_0010:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.Slice(int, int)""
  IL_0015:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_16()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    [System.Diagnostics.CodeAnalysis.UnscopedRef]
    public readonly System.ReadOnlySpan<int> M2() => F[..5];
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(c.M2()[0]);
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C.M2",
@"
{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.5
  IL_0010:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.Slice(int, int)""
  IL_0015:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_17()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[..][0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        c.F[..][0] = 1;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (18,9): error CS8331: Cannot assign to property 'this' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         c.F[..][0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.F[..][0]").WithArguments("property", "this").WithLocation(18, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_18()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }

    readonly void Main()
    {
        F[..][0] = 1;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (15,9): error CS8331: Cannot assign to property 'this' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         F[..][0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "F[..][0]").WithArguments("property", "this").WithLocation(15, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_ReadonlyContext_IsRValue()
        {
            var src = @"
class C
{
    public readonly Buffer10<int> F;

    public C()
    {
        F = new Buffer10<int>();
        F[..][0] = 111;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        c.F[..5] = default;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (18,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         c.F[..5] = default;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "c.F[..5]").WithLocation(18, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_01()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c) => c.F[0];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0014:  ldind.i4
  IL_0015:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_02()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c)
    {
        ref readonly int x = ref c.F[0];
        return x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0014:  ldind.i4
  IL_0015:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_03()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c)
    {
        return M4(in c.F[0]);
    }

    static int M4(in int x)
    {
        return x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0014:  call       ""int Program.M4(in int)""
  IL_0019:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_04()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c)
    {
        return M4(ref c.F[0]);
    }

    static int M4(ref int x)
    {
        return x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (24,23): error CS8329: Cannot use method 'this.get' as a ref or out value because it is a readonly variable
                //         return M4(ref c.F[0]);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "c.F[0]").WithArguments("method", "this.get").WithLocation(24, 23)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_05()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static ref readonly int M2(in C c)
    {
        ref readonly int x = ref c.F[0];
        return ref x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0014:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_06()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static ref readonly int M2(in C c)
    {
        return ref c.F[0];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref readonly int System.ReadOnlySpan<int>.this[int].get""
  IL_0014:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_07()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        M2(c);
    }

    static void M2(in C c)
    {
        c.F[0] = 1;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (24,9): error CS8331: Cannot assign to method 'this.get' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         c.F[0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.F[0]").WithArguments("method", "this.get").WithLocation(24, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_08()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public C()
    {
        var b = new Buffer10<Buffer10<int>>();
        b[0][0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c) => c.F[0][0];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_09()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public C()
    {
        var b = new Buffer10<Buffer10<int>>();
        b[0][0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c)
    {
        ref readonly int x = ref c.F[0][0];
        return x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_10()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public C()
    {
        var b = new Buffer10<Buffer10<int>>();
        b[0][0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c)
    {
        return M4(in c.F[0][0]);
    }

    static int M4(in int x)
    {
        return x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_11()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public C()
    {
        var b = new Buffer10<Buffer10<int>>();
        b[0][0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c)
    {
        return M4(ref c.F[0][0]);
    }

    static int M4(ref int x)
    {
        return x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (24,23): error CS8329: Cannot use method 'this.get' as a ref or out value because it is a readonly variable
                //         return M4(ref c.F[0][0]);
                Diagnostic(ErrorCode.ERR_RefReadonlyNotField, "c.F[0][0]").WithArguments("method", "this.get").WithLocation(24, 23)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_12()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public C()
    {
        var b = new Buffer10<Buffer10<int>>();
        b[0][0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static ref readonly int M2(in C c)
    {
        ref readonly int x = ref c.F[0][0];
        return ref x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_13()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public C()
    {
        var b = new Buffer10<Buffer10<int>>();
        b[0][0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static ref readonly int M2(in C c)
    {
        return ref c.F[0][0];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111").VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ElementAccess_Variable_Readonly_14()
        {
            var src = @"
struct C
{
    public Buffer10<Buffer10<int>> F;

    public C()
    {
        var b = new Buffer10<Buffer10<int>>();
        b[0][0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        M2(c);
    }

    static void M2(in C c)
    {
        c.F[0][0] = 1;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (24,9): error CS8331: Cannot assign to method 'this.get' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         c.F[0][0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.F[0][0]").WithArguments("method", "this.get").WithLocation(24, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_Readonly_01()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c)[0]);
    }

    static System.ReadOnlySpan<int> M2(in C c) => c.F[..5];
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.5
  IL_0010:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.Slice(int, int)""
  IL_0015:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_Readonly_02()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c)[0]);
    }

    static System.ReadOnlySpan<int> M2(in C c)
    {
        System.ReadOnlySpan<int> x = c.F[..5];
        return x;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "111", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init (System.ReadOnlySpan<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""readonly System.ReadOnlySpan<int> Buffer10<int>.AsReadOnlySpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.5
  IL_0010:  call       ""System.ReadOnlySpan<int> System.ReadOnlySpan<int>.Slice(int, int)""
  IL_0015:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_Readonly_04()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        System.Console.Write(M2(c));
    }

    static int M2(in C c)
    {
        return M4(c.F[..]);
    }

    static int M4(System.Span<int> x)
    {
        return x[0];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics(
                // (24,19): error CS1503: Argument 1: cannot convert from 'System.ReadOnlySpan<int>' to 'System.Span<int>'
                //         return M4(c.F[..]);
                Diagnostic(ErrorCode.ERR_BadArgType, "c.F[..]").WithArguments("1", "System.ReadOnlySpan<int>", "System.Span<int>").WithLocation(24, 19)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void Slice_Variable_Readonly_07()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        var b = new Buffer10<int>();
        b[0] = 111;
        F = b;
    }
}

class Program
{
    static void Main()
    {
        var c = new C();
        M2(c);
    }

    static void M2(in C c)
    {
        c.F[..][0] = 1;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (24,9): error CS8331: Cannot assign to property 'this' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         c.F[..][0] = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "c.F[..][0]").WithArguments("property", "this").WithLocation(24, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void ListPattern()
        {
            var src = @"
struct C
{
    public Buffer10<int> F = default;
    public C() {}
}

class Program
{
    static void M3(C x)
    {
        if (x.F is [0, ..])
        {}
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (12,20): error CS8985: List patterns may not be used for a value of type 'Buffer10<int>'. No suitable 'Length' or 'Count' property was found.
                //         if (x.F is [0, ..])
                Diagnostic(ErrorCode.ERR_ListPatternRequiresLength, "[0, ..]").WithArguments("Buffer10<int>").WithLocation(12, 20),
                // (12,20): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10<int>'
                //         if (x.F is [0, ..])
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "[0, ..]").WithArguments("Buffer10<int>").WithLocation(12, 20)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void NoIndex()
        {
            var src = @"
struct C
{
    public Buffer10<int> F = default;
    public C() {}
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (12,17): error CS0443: Syntax error; value expected
                //         _ = x.F[];
                Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(12, 17)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void TooManyIndexes()
        {
            var src = @"
struct C
{
    public Buffer10<int> F = default;
    public C() {}
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[0, 1];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);

            // PROTOTYPE(SafeFixedSizeBuffer): The wording is somewhat misleading. Adjust?
            comp.VerifyDiagnostics(
                // (12,13): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10<int>'
                //         _ = x.F[0, 1];
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "x.F[0, 1]").WithArguments("Buffer10<int>").WithLocation(12, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void WrongIndexType_01()
        {
            var src = @"
struct C
{
    public Buffer10<int> F = default;
    public C() {}
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[""a""];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);

            // PROTOTYPE(SafeFixedSizeBuffer): The wording is somewhat misleading. Adjust?
            comp.VerifyDiagnostics(
                // (12,13): error CS0021: Cannot apply indexing with [] to an expression of type 'Buffer10<int>'
                //         _ = x.F["a"];
                Diagnostic(ErrorCode.ERR_BadIndexLHS, @"x.F[""a""]").WithArguments("Buffer10<int>").WithLocation(12, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void NamedIndex()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[x: 1];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);

            // PROTOTYPE(SafeFixedSizeBuffers): Adjust wording of the message?
            comp.VerifyDiagnostics(
                // (11,13): error CS1742: An array access may not have a named argument specifier
                //         _ = x.F[x: 1];
                Diagnostic(ErrorCode.ERR_NamedArgumentForArray, "x.F[x: 1]").WithLocation(11, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void RefOutInIndex()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x, int y)
    {
        _ = x.F[ref y];
        _ = x.F[in y];
        _ = x.F[out y];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (11,21): error CS1615: Argument 1 may not be passed with the 'ref' keyword
                //         _ = x.F[ref y];
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "y").WithArguments("1", "ref").WithLocation(11, 21),
                // (12,20): error CS1615: Argument 1 may not be passed with the 'in' keyword
                //         _ = x.F[in y];
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "y").WithArguments("1", "in").WithLocation(12, 20),
                // (13,21): error CS1615: Argument 1 may not be passed with the 'out' keyword
                //         _ = x.F[out y];
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "y").WithArguments("1", "out").WithLocation(13, 21)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void AlwaysDefault_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (4,26): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value 
                //     public Buffer10<int> F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "").WithLocation(4, 26)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void AlwaysDefault_02()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void M(C c)
    {
        ref int x = ref c.F[0];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void AlwaysDefault_03()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void M(C c)
    {
        ref readonly int x = ref c.F[0];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);

            // PROTOTYPE(SafeFixedSizeBuffers): Report ErrorCode.WRN_UnassignedInternalField
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void AlwaysDefault_04()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void M(in C c)
    {
        ref readonly int x = ref c.F[0];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);

            // PROTOTYPE(SafeFixedSizeBuffers): Report ErrorCode.WRN_UnassignedInternalField
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void AlwaysDefault_05()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void M(C c)
    {
        System.Span<int> x = c.F[..5];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void AlwaysDefault_06()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void M(C c)
    {
        System.ReadOnlySpan<int> x = c.F[..5];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);

            // PROTOTYPE(SafeFixedSizeBuffers): Report ErrorCode.WRN_UnassignedInternalField?
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void AlwaysDefault_07()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void M(in C c)
    {
        System.ReadOnlySpan<int> x = c.F[..5];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (4,26): warning CS0649: Field 'C.F' is never assigned to, and will always have its default value 
                //     public Buffer10<int> F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "").WithLocation(4, 26)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void DefiniteAssignment_01()
        {
            var src = @"
public struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void M()
    {
        C c;
        _ = c.F[0];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (12,13): error CS0170: Use of possibly unassigned field 'F'
                //         _ = c.F[0];
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "c.F").WithArguments("F").WithLocation(12, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void DefiniteAssignment_02()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void M()
    {
        C c;
        c.F[0] = 1;
        _ = c.F;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (12,9): error CS0170: Use of possibly unassigned field 'F'
                //         c.F[0] = 1;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "c.F").WithArguments("F").WithLocation(12, 9)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void DefiniteAssignment_03()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;

    public C()
    {
        F[0] = 1;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);
            var verifier = CompileAndVerify(comp).VerifyDiagnostics();

            verifier.VerifyIL("C..ctor",
@"
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  initobj    ""Buffer10<int>""
  IL_000c:  ldarg.0
  IL_000d:  ldflda     ""Buffer10<int> C.F""
  IL_0012:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_0017:  stloc.0
  IL_0018:  ldloca.s   V_0
  IL_001a:  ldc.i4.0
  IL_001b:  call       ""ref int System.Span<int>.this[int].get""
  IL_0020:  ldc.i4.1
  IL_0021:  stind.i4
  IL_0022:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[0];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Range__get_Start);
            comp.MakeMemberMissing(WellKnownMember.System_Range__get_End);
            comp.MakeMemberMissing(WellKnownMember.System_Index__GetOffset);

            comp.VerifyEmitDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_02()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[^10];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Range__get_Start);
            comp.MakeMemberMissing(WellKnownMember.System_Range__get_End);

            comp.VerifyEmitDiagnostics();
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_03()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[^10];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Index__GetOffset);

            comp.VerifyEmitDiagnostics(
                // (11,13): error CS0656: Missing compiler required member 'System.Index.GetOffset'
                //         _ = x.F[^10];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[^10]").WithArguments("System.Index", "GetOffset").WithLocation(11, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_04()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[..3];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Range__get_Start);

            comp.VerifyEmitDiagnostics(
                // (11,13): error CS0656: Missing compiler required member 'System.Range.get_Start'
                //         _ = x.F[..3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[..3]").WithArguments("System.Range", "get_Start").WithLocation(11, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_05()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[..3];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Range__get_End);

            comp.VerifyEmitDiagnostics(
                // (11,13): error CS0656: Missing compiler required member 'System.Range.get_End'
                //         _ = x.F[..3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[..3]").WithArguments("System.Range", "get_End").WithLocation(11, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_06()
        {
            var src = @"
class C
{
    public Buffer10<int> F = default;
}

class Program
{
    static void M3(C x)
    {
        _ = x.F[..3];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Index__GetOffset);

            comp.VerifyEmitDiagnostics(
                // (11,13): error CS0656: Missing compiler required member 'System.Index.GetOffset'
                //         _ = x.F[..3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[..3]").WithArguments("System.Index", "GetOffset").WithLocation(11, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_07()
        {
            /* Equivalent to:
            [System.Runtime.CompilerServices.InlineArray(10)]
            public struct Buffer10<T>
            {
                private T _element0;
            }
            */
            var ilSource = @"
.class public sequential ansi sealed beforefieldinit Buffer10`1<T>
    extends [mscorlib]System.ValueType
{
    .custom instance void System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 0a 00 00 00 00 00
    )
    // Fields
    .field private !T _element0
}

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.InlineArrayAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 08 00 00 00 01 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00
    )
    // Methods
    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 length
        ) cil managed 
    {
        // Method begins at RVA 0x2050
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: ret
    }
}
";

            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void M3(C x)
    {
        x.F[0] = 111;
    }

    static void M4(in C x)
    {
        _ = x.F[0];
    }
}
";
            var comp = CreateCompilationWithIL(src, ilSource, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (11,9): error CS0656: Missing compiler required member 'Buffer10<int>.AsSpan'
                //         x.F[0] = 111;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[0]").WithArguments("Buffer10<int>", "AsSpan").WithLocation(11, 9),
                // (16,13): error CS0656: Missing compiler required member 'Buffer10<int>.AsReadOnlySpan'
                //         _ = x.F[0];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[0]").WithArguments("Buffer10<int>", "AsReadOnlySpan").WithLocation(16, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void MissingHelper_08()
        {
            var src = @"
struct C
{
    public Buffer10<int> F;
}

class Program
{
    static void M3(C x)
    {
        x.F[0] = 1;
        _ = x.F[..3];
    }

    static void M4(in C y)
    {
        _ = y.F[0];
        _ = y.F[..3];
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);

            comp.MakeMemberMissing(WellKnownMember.System_Span_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int);
            comp.MakeMemberMissing(WellKnownMember.System_Span_T__get_Item);
            comp.MakeMemberMissing(WellKnownMember.System_ReadOnlySpan_T__get_Item);

            comp.VerifyEmitDiagnostics(
                // (11,9): error CS0656: Missing compiler required member 'System.Span`1.get_Item'
                //         x.F[0] = 1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[0]").WithArguments("System.Span`1", "get_Item").WithLocation(11, 9),
                // (12,13): error CS0656: Missing compiler required member 'System.Span`1.Slice'
                //         _ = x.F[..3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "x.F[..3]").WithArguments("System.Span`1", "Slice").WithLocation(12, 13),
                // (17,13): error CS0656: Missing compiler required member 'System.ReadOnlySpan`1.get_Item'
                //         _ = y.F[0];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "y.F[0]").WithArguments("System.ReadOnlySpan`1", "get_Item").WithLocation(17, 13),
                // (18,13): error CS0656: Missing compiler required member 'System.ReadOnlySpan`1.Slice'
                //         _ = y.F[..3];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "y.F[..3]").WithArguments("System.ReadOnlySpan`1", "Slice").WithLocation(18, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void NullableAnalysis_01()
        {
            var src = @"
#nullable enable

class C<T>
{
    public Buffer10<T> F = default;
}

class Program
{
    static void M2(string s1, string? s2)
    {
        _ = GetC(s1).F[0].Length;
        _ = GetC(s2).F[0].Length;
        _ = GetC(s1).F[..5][0].Length;
        _ = GetC(s2).F[..5][0].Length;
    }

    static C<T> GetC<T>(T x)
    {
        var c = new C<T>();
        c.F[0] = x;
        return c;
    }
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (14,13): warning CS8602: Dereference of a possibly null reference.
                //         _ = GetC(s2).F[0].Length;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "GetC(s2).F[0]").WithLocation(14, 13),
                // (16,13): warning CS8602: Dereference of a possibly null reference.
                //         _ = GetC(s2).F[..5][0].Length;
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "GetC(s2).F[..5][0]").WithLocation(16, 13)
                );
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void CompoundAssignment_01()
        {
            var src = @"
class C
{
    public Buffer10<int> F;
}

class Program
{
    static void Main()
    {
        var x = new C();
        x.F[0] = -1;
        System.Console.Write(M1(x));
        M2(x);
        System.Console.Write(' ');
        System.Console.Write(M1(x));
    }

    static int M1(C x) => x.F[0];
    static void M2(C x) => x.F[0] += 111;
}
";
            var comp = CreateCompilationWithIL(src, Buffer10IL, targetFramework: TargetFramework.NetCoreApp, appendDefaultHeader: false, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "-1 110").VerifyDiagnostics();

            verifier.VerifyIL("Program.M2",
@"
{
  // Code size       27 (0x1b)
  .maxstack  3
  .locals init (System.Span<int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Buffer10<int> C.F""
  IL_0006:  call       ""System.Span<int> Buffer10<int>.AsSpan()""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.0
  IL_000f:  call       ""ref int System.Span<int>.this[int].get""
  IL_0014:  dup
  IL_0015:  ldind.i4
  IL_0016:  ldc.i4.s   111
  IL_0018:  add
  IL_0019:  stind.i4
  IL_001a:  ret
}
");
        }
    }
}
