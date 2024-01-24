﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen;

public class CodeGenSpanBasedStringConcatTests : CSharpTestBase
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatTwo_ReadOnlySpan1()
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                }

                static string M1(string s, char c) => s + c;
                static string M2(string s, char c) => c + s;
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "sccs" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M1", """
            {
              // Code size       21 (0x15)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000f:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0014:  ret
            }
            """);
        comp.VerifyIL("Test.M2", """
            {
              // Code size       21 (0x15)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0014:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatTwo_ReadOnlySpan2()
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'C';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                }

                static string M1(string s, char c) => s + char.ToLowerInvariant(c);
                static string M2(string s, char c) => char.ToLowerInvariant(c) + s;
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "sccs" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M1", """
            {
              // Code size       26 (0x1a)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  call       "char char.ToLowerInvariant(char)"
              IL_000c:  stloc.0
              IL_000d:  ldloca.s   V_0
              IL_000f:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0014:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0019:  ret
            }
            """);
        comp.VerifyIL("Test.M2", """
            {
              // Code size       26 (0x1a)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  ldarg.1
              IL_0001:  call       "char char.ToLowerInvariant(char)"
              IL_0006:  stloc.0
              IL_0007:  ldloca.s   V_0
              IL_0009:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000e:  ldarg.0
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0019:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatTwo_ReadOnlySpan_SideEffect()
    {
        var source = """
            using System;

            public class Test
            {
                private static int stringCounter;
                private static int charCounterPlusOne = 1;

                static void Main()
                {
                    Console.WriteLine(M1());
                    Console.WriteLine(M2());
                }

                static string M1() => GetStringWithSideEffect() + GetCharWithSideEffect();
                static string M2() => GetCharWithSideEffect() + GetStringWithSideEffect();

                private static string GetStringWithSideEffect()
                {
                    Console.Write(stringCounter++);
                    return "s";
                }

                private static char GetCharWithSideEffect()
                {
                    Console.Write(charCounterPlusOne++);
                    return 'c';
                }
            }
            """;

        var expectedOutput = """
            01sc
            21cs
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? expectedOutput : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M1", """
            {
              // Code size       29 (0x1d)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  call       "string Test.GetStringWithSideEffect()"
              IL_0005:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000a:  call       "char Test.GetCharWithSideEffect()"
              IL_000f:  stloc.0
              IL_0010:  ldloca.s   V_0
              IL_0012:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0017:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001c:  ret
            }
            """);
        comp.VerifyIL("Test.M2", """
            {
              // Code size       29 (0x1d)
              .maxstack  2
              .locals init (char V_0)
              IL_0000:  call       "char Test.GetCharWithSideEffect()"
              IL_0005:  stloc.0
              IL_0006:  ldloca.s   V_0
              IL_0008:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000d:  call       "string Test.GetStringWithSideEffect()"
              IL_0012:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0017:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001c:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatTwo_ReadOnlySpan_ReferenceToSameLocation()
    {
        var source = """
            using System;

            var c = new C();
            c.M();

            class C
            {
                public char c = 'a';

                public ref char GetC()
                {
                    c = 'b';
                    return ref c;
                }

                public void M()
                {
                    Console.Write(c.ToString() + GetC());
                }
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "ab" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("C.M", """
            {
              // Code size       40 (0x28)
              .maxstack  2
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.0
              IL_0001:  ldfld      "char C.c"
              IL_0006:  stloc.0
              IL_0007:  ldloca.s   V_0
              IL_0009:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000e:  ldarg.0
              IL_000f:  call       "ref char C.GetC()"
              IL_0014:  ldind.u2
              IL_0015:  stloc.1
              IL_0016:  ldloca.s   V_1
              IL_0018:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_001d:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0022:  call       "void System.Console.Write(string)"
              IL_0027:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatTwo_ReadOnlySpan_MutateLocal()
    {
        var source = """
            using System;

            var c = new C();
            Console.WriteLine(c.M());

            class C
            {
                public string M()
                {
                    var c = 'a';
                    return c + SneakyLocalChange(ref c);
                }

                private string SneakyLocalChange(ref char local)
                {
                    local = 'b';
                    return "b";
                }
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "ab" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("C.M", """
            {
              // Code size       31 (0x1f)
              .maxstack  3
              .locals init (char V_0, //c
                            char V_1)
              IL_0000:  ldc.i4.s   97
              IL_0002:  stloc.0
              IL_0003:  ldloc.0
              IL_0004:  stloc.1
              IL_0005:  ldloca.s   V_1
              IL_0007:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000c:  ldarg.0
              IL_000d:  ldloca.s   V_0
              IL_000f:  call       "string C.SneakyLocalChange(ref char)"
              IL_0014:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0019:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001e:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    public void ConcatTwoCharToStrings(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var c1 = 'a';
                    var c2 = 'b';
                    Console.Write(M(c1, c2));
                }

                static string M(char c1, char c2) => c1.ToString() + c2.ToString();
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "ab" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M", """
            {
              // Code size       24 (0x18)
              .maxstack  2
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.0
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0009:  ldarg.1
              IL_000a:  stloc.1
              IL_000b:  ldloca.s   V_1
              IL_000d:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0012:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0017:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    [InlineData((int)WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
    public void ConcatTwo_ReadOnlySpan_MissingMemberForOptimization(int member)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                }

                static string M1(string s, char c) => s + c;
                static string M2(string s, char c) => c + s;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing((WellKnownMember)member);

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "sccs" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       14 (0xe)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  ldarga.s   V_1
              IL_0003:  call       "string char.ToString()"
              IL_0008:  call       "string string.Concat(string, string)"
              IL_000d:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       14 (0xe)
              .maxstack  2
              IL_0000:  ldarga.s   V_1
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  call       "string string.Concat(string, string)"
              IL_000d:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatTwo_MissingObjectToString()
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                }

                static string M1(string s, char c) => s + c;
                static string M2(string s, char c) => c + s;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing(SpecialMember.System_Object__ToString);

        // Although we don't use object.ToString() or char.ToString() in the final codegen we still need object.ToString() during lowering.
        // Moreover, we previously reported these errors anyway, so this is not a behavioral change
        comp.VerifyEmitDiagnostics(
            // (13,47): error CS0656: Missing compiler required member 'System.Object.ToString'
            //     static string M1(string s, char c) => s + c;
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(13, 47),
            // (14,43): error CS0656: Missing compiler required member 'System.Object.ToString'
            //     static string M2(string s, char c) => c + s;
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(14, 43));
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    public void ConcatThree_ReadOnlySpan1(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                }

                static string M1(string s, char c) => c + s + s;
                static string M2(string s, char c) => s + c + s;
                static string M3(string s, char c) => s + s + c;
                static string M4(string s, char c) => c + s + c;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "cssscsssccsc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       27 (0x1b)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       27 (0x1b)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       27 (0x1b)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.1
              IL_000d:  stloc.0
              IL_000e:  ldloca.s   V_0
              IL_0010:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       30 (0x1e)
              .maxstack  3
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.1
              IL_0010:  stloc.1
              IL_0011:  ldloca.s   V_1
              IL_0013:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0018:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001d:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    public void ConcatThree_ReadOnlySpan2(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'C';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                }

                static string M1(string s, char c) => char.ToLowerInvariant(c) + s + s;
                static string M2(string s, char c) => s + char.ToLowerInvariant(c) + s;
                static string M3(string s, char c) => s + s + char.ToLowerInvariant(c);
                static string M4(string s, char c) => char.ToLowerInvariant(c) + s + char.ToLowerInvariant(c);
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "cssscsssccsc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       32 (0x20)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.1
              IL_0001:  call       "char char.ToLowerInvariant(char)"
              IL_0006:  stloc.0
              IL_0007:  ldloca.s   V_0
              IL_0009:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000e:  ldarg.0
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  ldarg.0
              IL_0015:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001a:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001f:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       32 (0x20)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  call       "char char.ToLowerInvariant(char)"
              IL_000c:  stloc.0
              IL_000d:  ldloca.s   V_0
              IL_000f:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0014:  ldarg.0
              IL_0015:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001a:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001f:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       32 (0x20)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.1
              IL_000d:  call       "char char.ToLowerInvariant(char)"
              IL_0012:  stloc.0
              IL_0013:  ldloca.s   V_0
              IL_0015:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_001a:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001f:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       40 (0x28)
              .maxstack  3
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.1
              IL_0001:  call       "char char.ToLowerInvariant(char)"
              IL_0006:  stloc.0
              IL_0007:  ldloca.s   V_0
              IL_0009:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000e:  ldarg.0
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  ldarg.1
              IL_0015:  call       "char char.ToLowerInvariant(char)"
              IL_001a:  stloc.1
              IL_001b:  ldloca.s   V_1
              IL_001d:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0022:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0027:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData("(s + c) + s")]
    [InlineData("s + (c + s)")]
    [InlineData("string.Concat(s, c.ToString()) + s")]
    [InlineData("s + string.Concat(c.ToString(), s)")]
    public void ConcatThree_ReadOnlySpan_OperandGroupingAndUserInputOfStringBasedConcats(string expression)
    {
        var source = $$"""
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M(s, c));
                }

                static string M(string s, char c) => {{expression}};
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "scs" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M", """
            {
              // Code size       27 (0x1b)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    public void ConcatThree_ReadOnlySpan_SideEffect(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                private static int stringCounter;
                private static int charCounterPlusOne = 1;

                static void Main()
                {
                    Console.WriteLine(M1());
                    Console.WriteLine(M2());
                    Console.WriteLine(M3());
                    Console.WriteLine(M4());
                }

                static string M1() => GetCharWithSideEffect() + GetStringWithSideEffect() + GetStringWithSideEffect();
                static string M2() => GetStringWithSideEffect() + GetCharWithSideEffect() + GetStringWithSideEffect();
                static string M3() => GetStringWithSideEffect() + GetStringWithSideEffect() + GetCharWithSideEffect();
                static string M4() => GetCharWithSideEffect() + GetStringWithSideEffect() + GetCharWithSideEffect();

                private static string GetStringWithSideEffect()
                {
                    Console.Write(stringCounter++);
                    return "s";
                }

                private static char GetCharWithSideEffect()
                {
                    Console.Write(charCounterPlusOne++);
                    return 'c';
                }
            }
            """;

        var expectedOutput = """
            101css
            223scs
            453ssc
            465csc
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? expectedOutput : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       39 (0x27)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  call       "char Test.GetCharWithSideEffect()"
              IL_0005:  stloc.0
              IL_0006:  ldloca.s   V_0
              IL_0008:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000d:  call       "string Test.GetStringWithSideEffect()"
              IL_0012:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0017:  call       "string Test.GetStringWithSideEffect()"
              IL_001c:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0021:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0026:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       39 (0x27)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  call       "string Test.GetStringWithSideEffect()"
              IL_0005:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000a:  call       "char Test.GetCharWithSideEffect()"
              IL_000f:  stloc.0
              IL_0010:  ldloca.s   V_0
              IL_0012:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0017:  call       "string Test.GetStringWithSideEffect()"
              IL_001c:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0021:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0026:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       39 (0x27)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  call       "string Test.GetStringWithSideEffect()"
              IL_0005:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000a:  call       "string Test.GetStringWithSideEffect()"
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  call       "char Test.GetCharWithSideEffect()"
              IL_0019:  stloc.0
              IL_001a:  ldloca.s   V_0
              IL_001c:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0021:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0026:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       42 (0x2a)
              .maxstack  3
              .locals init (char V_0,
                            char V_1)
              IL_0000:  call       "char Test.GetCharWithSideEffect()"
              IL_0005:  stloc.0
              IL_0006:  ldloca.s   V_0
              IL_0008:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000d:  call       "string Test.GetStringWithSideEffect()"
              IL_0012:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0017:  call       "char Test.GetCharWithSideEffect()"
              IL_001c:  stloc.1
              IL_001d:  ldloca.s   V_1
              IL_001f:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0024:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0029:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    public void ConcatThree_ReadOnlySpan_ReferenceToSameLocation(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            var c = new C();
            c.M();

            class C
            {
                public char c = 'a';

                public ref char GetC()
                {
                    c = 'b';
                    return ref c;
                }

                public void M()
                {
                    Console.Write("a" + c + GetC());
                }
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "aab" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       50 (0x32)
              .maxstack  3
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldstr      "a"
              IL_0005:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000a:  ldarg.0
              IL_000b:  ldfld      "char C.c"
              IL_0010:  stloc.0
              IL_0011:  ldloca.s   V_0
              IL_0013:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0018:  ldarg.0
              IL_0019:  call       "ref char C.GetC()"
              IL_001e:  ldind.u2
              IL_001f:  stloc.1
              IL_0020:  ldloca.s   V_1
              IL_0022:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0027:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_002c:  call       "void System.Console.Write(string)"
              IL_0031:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    public void ConcatThree_ReadOnlySpan_MutateLocal(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            var c = new C();
            Console.WriteLine(c.M());

            class C
            {
                public string M()
                {
                    var c = 'a';
                    return "a" + c + SneakyLocalChange(ref c);
                }

                private char SneakyLocalChange(ref char local)
                {
                    local = 'b';
                    return 'b';
                }
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "aab" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       44 (0x2c)
              .maxstack  4
              .locals init (char V_0, //c
                            char V_1,
                            char V_2)
              IL_0000:  ldc.i4.s   97
              IL_0002:  stloc.0
              IL_0003:  ldstr      "a"
              IL_0008:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000d:  ldloc.0
              IL_000e:  stloc.1
              IL_000f:  ldloca.s   V_1
              IL_0011:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0016:  ldarg.0
              IL_0017:  ldloca.s   V_0
              IL_0019:  call       "char C.SneakyLocalChange(ref char)"
              IL_001e:  stloc.2
              IL_001f:  ldloca.s   V_2
              IL_0021:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0026:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_002b:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    public void ConcatThreeCharToStrings(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var c1 = 'a';
                    var c2 = 'b';
                    var c3 = 'c';
                    Console.Write(M(c1, c2, c3));
                }

                static string M(char c1, char c2, char c3) => c1.ToString() + c2.ToString() + c3.ToString();
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M", """
            {
              // Code size       33 (0x21)
              .maxstack  3
              .locals init (char V_0,
                            char V_1,
                            char V_2)
              IL_0000:  ldarg.0
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0009:  ldarg.1
              IL_000a:  stloc.1
              IL_000b:  ldloca.s   V_1
              IL_000d:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0012:  ldarg.2
              IL_0013:  stloc.2
              IL_0014:  ldloca.s   V_2
              IL_0016:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan)]
    [InlineData((int)WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
    public void ConcatThree_ReadOnlySpan_MissingMemberForOptimization(int member)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                }

                static string M1(string s, char c) => c + s + s;
                static string M2(string s, char c) => s + c + s;
                static string M3(string s, char c) => s + s + c;
                static string M4(string s, char c) => c + s + c;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing((WellKnownMember)member);

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "cssscsssccsc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       15 (0xf)
              .maxstack  3
              IL_0000:  ldarga.s   V_1
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  ldarg.0
              IL_0009:  call       "string string.Concat(string, string, string)"
              IL_000e:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       15 (0xf)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarga.s   V_1
              IL_0003:  call       "string char.ToString()"
              IL_0008:  ldarg.0
              IL_0009:  call       "string string.Concat(string, string, string)"
              IL_000e:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       15 (0xf)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarg.0
              IL_0002:  ldarga.s   V_1
              IL_0004:  call       "string char.ToString()"
              IL_0009:  call       "string string.Concat(string, string, string)"
              IL_000e:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       21 (0x15)
              .maxstack  3
              IL_0000:  ldarga.s   V_1
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  ldarga.s   V_1
              IL_000a:  call       "string char.ToString()"
              IL_000f:  call       "string string.Concat(string, string, string)"
              IL_0014:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
    public void ConcatThree_UserInputOfSpanBasedConcat_ConcatWithString(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    Console.Write(M1(s1.AsSpan(), s2, s3));
                    Console.Write(M2(s1.AsSpan(), s2, s3));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, string s3) => string.Concat(s1, s2.AsSpan()) + s3;
                static string M2(ReadOnlySpan<char> s1, string s2, string s3) => s3 + string.Concat(s1, s2.AsSpan());
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abccab" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       19 (0x13)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  ldarg.2
              IL_0008:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000d:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0012:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       19 (0x13)
              .maxstack  3
              IL_0000:  ldarg.2
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  ldarg.1
              IL_0008:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000d:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0012:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    public void ConcatThree_UserInputOfSpanBasedConcat_ConcatWithChar(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var c = 'c';
                    Console.Write(M1(s1.AsSpan(), s2, c));
                    Console.Write(M2(s1.AsSpan(), s2, c));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, char c) => string.Concat(s1, s2.AsSpan()) + c;
                static string M2(ReadOnlySpan<char> s1, string s2, char c) => c + string.Concat(s1, s2.AsSpan());
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abccab" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       22 (0x16)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  ldarg.2
              IL_0008:  stloc.0
              IL_0009:  ldloca.s   V_0
              IL_000b:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0010:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0015:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       22 (0x16)
              .maxstack  3
              .locals init (char V_0)
              IL_0000:  ldarg.2
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0009:  ldarg.0
              IL_000a:  ldarg.1
              IL_000b:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0010:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0015:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatThree_UserInputOfSpanBasedConcat_ConcatWithString_MissingMemberToMergeIntoSingleConcat(int member)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    Console.Write(M1(s1.AsSpan(), s2, s3));
                    Console.Write(M2(s1.AsSpan(), s2, s3));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, string s3) => string.Concat(s1, s2.AsSpan()) + s3;
                static string M2(ReadOnlySpan<char> s1, string s2, string s3) => s3 + string.Concat(s1, s2.AsSpan());
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing((WellKnownMember)member);

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abccab" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       19 (0x13)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000c:  ldarg.2
              IL_000d:  call       "string string.Concat(string, string)"
              IL_0012:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       19 (0x13)
              .maxstack  3
              IL_0000:  ldarg.2
              IL_0001:  ldarg.0
              IL_0002:  ldarg.1
              IL_0003:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0008:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000d:  call       "string string.Concat(string, string)"
              IL_0012:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatThree_UserInputOfSpanBasedConcat_ConcatWithChar_MissingMemberToMergeIntoSingleConcat(int member)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var c = 'c';
                    Console.Write(M1(s1.AsSpan(), s2, c));
                    Console.Write(M2(s1.AsSpan(), s2, c));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, char c) => string.Concat(s1, s2.AsSpan()) + c;
                static string M2(ReadOnlySpan<char> s1, string s2, char c) => c + string.Concat(s1, s2.AsSpan());
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing((WellKnownMember)member);

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abccab" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       25 (0x19)
              .maxstack  2
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000c:  ldarga.s   V_2
              IL_000e:  call       "string char.ToString()"
              IL_0013:  call       "string string.Concat(string, string)"
              IL_0018:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       25 (0x19)
              .maxstack  3
              IL_0000:  ldarga.s   V_2
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  ldarg.1
              IL_0009:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0013:  call       "string string.Concat(string, string)"
              IL_0018:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatThree_MissingObjectToString()
    {
        var source = """
            using System;
            
            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                }
            
                static string M1(string s, char c) => c + s + s;
                static string M2(string s, char c) => s + c + s;
                static string M3(string s, char c) => s + s + c;
                static string M4(string s, char c) => c + s + c;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing(SpecialMember.System_Object__ToString);

        // Although we don't use object.ToString() or char.ToString() in the final codegen we still need object.ToString() during lowering.
        // Moreover, we previously reported these errors anyway, so this is not a behavioral change
        comp.VerifyEmitDiagnostics(
            // (15,43): error CS0656: Missing compiler required member 'System.Object.ToString'
            //     static string M1(string s, char c) => c + s + s;
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(15, 43),
            // (16,47): error CS0656: Missing compiler required member 'System.Object.ToString'
            //     static string M2(string s, char c) => s + c + s;
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(16, 47),
            // (17,51): error CS0656: Missing compiler required member 'System.Object.ToString'
            //     static string M3(string s, char c) => s + s + c;
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(17, 51),
            // (18,43): error CS0656: Missing compiler required member 'System.Object.ToString'
            //     static string M4(string s, char c) => c + s + c;
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(18, 43),
            // (18,51): error CS0656: Missing compiler required member 'System.Object.ToString'
            //     static string M4(string s, char c) => c + s + c;
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(18, 51));
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatFour_ReadOnlySpan1(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                    Console.Write(M5(s, c));
                    Console.Write(M6(s, c));
                    Console.Write(M7(s, c));
                }

                static string M1(string s, char c) => c + s + s + s;
                static string M2(string s, char c) => s + c + s + s;
                static string M3(string s, char c) => s + s + c + s;
                static string M4(string s, char c) => s + s + s + c;
                static string M5(string s, char c) => c + s + c + s;
                static string M6(string s, char c) => s + c + s + c;
                static string M7(string s, char c) => c + s + s + c;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "csssscsssscssssccscsscsccssc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.0
              IL_0016:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.0
              IL_0016:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.1
              IL_000d:  stloc.0
              IL_000e:  ldloca.s   V_0
              IL_0010:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0015:  ldarg.0
              IL_0016:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.0
              IL_000d:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0012:  ldarg.1
              IL_0013:  stloc.0
              IL_0014:  ldloca.s   V_0
              IL_0016:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
        verifier.VerifyIL("Test.M5", """
            {
              // Code size       36 (0x24)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.1
              IL_0010:  stloc.1
              IL_0011:  ldloca.s   V_1
              IL_0013:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0018:  ldarg.0
              IL_0019:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0023:  ret
            }
            """);
        verifier.VerifyIL("Test.M6", """
            {
              // Code size       36 (0x24)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.1
              IL_0016:  stloc.1
              IL_0017:  ldloca.s   V_1
              IL_0019:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_001e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0023:  ret
            }
            """);
        verifier.VerifyIL("Test.M7", """
            {
              // Code size       36 (0x24)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.1
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.1
              IL_0016:  stloc.1
              IL_0017:  ldloca.s   V_1
              IL_0019:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_001e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0023:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatFour_ReadOnlySpan2(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'C';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                    Console.Write(M5(s, c));
                    Console.Write(M6(s, c));
                    Console.Write(M7(s, c));
                }

                static string M1(string s, char c) => char.ToLowerInvariant(c) + s + s + s;
                static string M2(string s, char c) => s + char.ToLowerInvariant(c) + s + s;
                static string M3(string s, char c) => s + s + char.ToLowerInvariant(c) + s;
                static string M4(string s, char c) => s + s + s + char.ToLowerInvariant(c);
                static string M5(string s, char c) => char.ToLowerInvariant(c) + s + char.ToLowerInvariant(c) + s;
                static string M6(string s, char c) => s + char.ToLowerInvariant(c) + s + char.ToLowerInvariant(c);
                static string M7(string s, char c) => char.ToLowerInvariant(c) + s + s + char.ToLowerInvariant(c);
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "csssscsssscssssccscsscsccssc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       38 (0x26)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.1
              IL_0001:  call       "char char.ToLowerInvariant(char)"
              IL_0006:  stloc.0
              IL_0007:  ldloca.s   V_0
              IL_0009:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000e:  ldarg.0
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  ldarg.0
              IL_0015:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001a:  ldarg.0
              IL_001b:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0020:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0025:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       38 (0x26)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  call       "char char.ToLowerInvariant(char)"
              IL_000c:  stloc.0
              IL_000d:  ldloca.s   V_0
              IL_000f:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0014:  ldarg.0
              IL_0015:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001a:  ldarg.0
              IL_001b:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0020:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0025:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       38 (0x26)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.1
              IL_000d:  call       "char char.ToLowerInvariant(char)"
              IL_0012:  stloc.0
              IL_0013:  ldloca.s   V_0
              IL_0015:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_001a:  ldarg.0
              IL_001b:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0020:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0025:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       38 (0x26)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.0
              IL_000d:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0012:  ldarg.1
              IL_0013:  call       "char char.ToLowerInvariant(char)"
              IL_0018:  stloc.0
              IL_0019:  ldloca.s   V_0
              IL_001b:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0020:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0025:  ret
            }
            """);
        verifier.VerifyIL("Test.M5", """
            {
              // Code size       46 (0x2e)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.1
              IL_0001:  call       "char char.ToLowerInvariant(char)"
              IL_0006:  stloc.0
              IL_0007:  ldloca.s   V_0
              IL_0009:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000e:  ldarg.0
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  ldarg.1
              IL_0015:  call       "char char.ToLowerInvariant(char)"
              IL_001a:  stloc.1
              IL_001b:  ldloca.s   V_1
              IL_001d:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0022:  ldarg.0
              IL_0023:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0028:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_002d:  ret
            }
            """);
        verifier.VerifyIL("Test.M6", """
            {
              // Code size       46 (0x2e)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  call       "char char.ToLowerInvariant(char)"
              IL_000c:  stloc.0
              IL_000d:  ldloca.s   V_0
              IL_000f:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0014:  ldarg.0
              IL_0015:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001a:  ldarg.1
              IL_001b:  call       "char char.ToLowerInvariant(char)"
              IL_0020:  stloc.1
              IL_0021:  ldloca.s   V_1
              IL_0023:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0028:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_002d:  ret
            }
            """);
        verifier.VerifyIL("Test.M7", """
            {
              // Code size       46 (0x2e)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.1
              IL_0001:  call       "char char.ToLowerInvariant(char)"
              IL_0006:  stloc.0
              IL_0007:  ldloca.s   V_0
              IL_0009:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000e:  ldarg.0
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  ldarg.0
              IL_0015:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001a:  ldarg.1
              IL_001b:  call       "char char.ToLowerInvariant(char)"
              IL_0020:  stloc.1
              IL_0021:  ldloca.s   V_1
              IL_0023:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0028:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_002d:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData("(s + c) + s + s")]
    [InlineData("s + (c + s) + s")]
    [InlineData("s + c + (s + s)")]
    [InlineData("(s + c + s) + s")]
    [InlineData("s + (c + s + s)")]
    [InlineData("(s + c) + (s + s)")]
    [InlineData("string.Concat(s, c.ToString()) + s + s")]
    [InlineData("s + string.Concat(c.ToString(), s) + s")]
    [InlineData("s + c + string.Concat(s, s)")]
    [InlineData("string.Concat(s, c.ToString(), s) + s")]
    [InlineData("s + string.Concat(c.ToString(), s, s)")]
    [InlineData("string.Concat(s, c.ToString()) + string.Concat(s, s)")]
    public void ConcatFour_ReadOnlySpan_OperandGroupingAndUserInputOfStringBasedConcats(string expression)
    {
        var source = $$"""
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M(s, c));
                }

                static string M(string s, char c) => {{expression}};
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "scss" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M", """
            {
              // Code size       33 (0x21)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.1
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0015:  ldarg.0
              IL_0016:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0020:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatFour_ReadOnlySpan_SideEffect(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                private static int stringCounter;
                private static int charCounterPlusOne = 1;

                static void Main()
                {
                    Console.WriteLine(M1());
                    Console.WriteLine(M2());
                    Console.WriteLine(M3());
                    Console.WriteLine(M4());
                    Console.WriteLine(M5());
                    Console.WriteLine(M6());
                    Console.WriteLine(M7());
                }

                static string M1() => GetCharWithSideEffect() + GetStringWithSideEffect() + GetStringWithSideEffect() + GetStringWithSideEffect();
                static string M2() => GetStringWithSideEffect() + GetCharWithSideEffect() + GetStringWithSideEffect() + GetStringWithSideEffect();
                static string M3() => GetStringWithSideEffect() + GetStringWithSideEffect() + GetCharWithSideEffect() + GetStringWithSideEffect();
                static string M4() => GetStringWithSideEffect() + GetStringWithSideEffect() + GetStringWithSideEffect() + GetCharWithSideEffect();
                static string M5() => GetCharWithSideEffect() + GetStringWithSideEffect() + GetCharWithSideEffect() + GetStringWithSideEffect();
                static string M6() => GetStringWithSideEffect() + GetCharWithSideEffect() + GetStringWithSideEffect() + GetCharWithSideEffect();
                static string M7() => GetCharWithSideEffect() + GetStringWithSideEffect() + GetStringWithSideEffect() + GetCharWithSideEffect();

                private static string GetStringWithSideEffect()
                {
                    Console.Write(stringCounter++);
                    return "s";
                }

                private static char GetCharWithSideEffect()
                {
                    Console.Write(charCounterPlusOne++);
                    return 'c';
                }
            }
            """;

        var expectedOutput = """
            1012csss
            3245scss
            6738sscs
            910114sssc
            512613cscs
            147158scsc
            9161710cssc
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? expectedOutput : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       49 (0x31)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  call       "char Test.GetCharWithSideEffect()"
              IL_0005:  stloc.0
              IL_0006:  ldloca.s   V_0
              IL_0008:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000d:  call       "string Test.GetStringWithSideEffect()"
              IL_0012:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0017:  call       "string Test.GetStringWithSideEffect()"
              IL_001c:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0021:  call       "string Test.GetStringWithSideEffect()"
              IL_0026:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_002b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0030:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       49 (0x31)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  call       "string Test.GetStringWithSideEffect()"
              IL_0005:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000a:  call       "char Test.GetCharWithSideEffect()"
              IL_000f:  stloc.0
              IL_0010:  ldloca.s   V_0
              IL_0012:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0017:  call       "string Test.GetStringWithSideEffect()"
              IL_001c:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0021:  call       "string Test.GetStringWithSideEffect()"
              IL_0026:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_002b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0030:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       49 (0x31)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  call       "string Test.GetStringWithSideEffect()"
              IL_0005:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000a:  call       "string Test.GetStringWithSideEffect()"
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  call       "char Test.GetCharWithSideEffect()"
              IL_0019:  stloc.0
              IL_001a:  ldloca.s   V_0
              IL_001c:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0021:  call       "string Test.GetStringWithSideEffect()"
              IL_0026:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_002b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0030:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       49 (0x31)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  call       "string Test.GetStringWithSideEffect()"
              IL_0005:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000a:  call       "string Test.GetStringWithSideEffect()"
              IL_000f:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0014:  call       "string Test.GetStringWithSideEffect()"
              IL_0019:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001e:  call       "char Test.GetCharWithSideEffect()"
              IL_0023:  stloc.0
              IL_0024:  ldloca.s   V_0
              IL_0026:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_002b:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0030:  ret
            }
            """);
        verifier.VerifyIL("Test.M5", """
            {
              // Code size       52 (0x34)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  call       "char Test.GetCharWithSideEffect()"
              IL_0005:  stloc.0
              IL_0006:  ldloca.s   V_0
              IL_0008:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000d:  call       "string Test.GetStringWithSideEffect()"
              IL_0012:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0017:  call       "char Test.GetCharWithSideEffect()"
              IL_001c:  stloc.1
              IL_001d:  ldloca.s   V_1
              IL_001f:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0024:  call       "string Test.GetStringWithSideEffect()"
              IL_0029:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_002e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0033:  ret
            }
            """);
        verifier.VerifyIL("Test.M6", """
            {
              // Code size       52 (0x34)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  call       "string Test.GetStringWithSideEffect()"
              IL_0005:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000a:  call       "char Test.GetCharWithSideEffect()"
              IL_000f:  stloc.0
              IL_0010:  ldloca.s   V_0
              IL_0012:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0017:  call       "string Test.GetStringWithSideEffect()"
              IL_001c:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0021:  call       "char Test.GetCharWithSideEffect()"
              IL_0026:  stloc.1
              IL_0027:  ldloca.s   V_1
              IL_0029:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_002e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0033:  ret
            }
            """);
        verifier.VerifyIL("Test.M7", """
            {
              // Code size       52 (0x34)
              .maxstack  4
              .locals init (char V_0,
                          char V_1)
              IL_0000:  call       "char Test.GetCharWithSideEffect()"
              IL_0005:  stloc.0
              IL_0006:  ldloca.s   V_0
              IL_0008:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000d:  call       "string Test.GetStringWithSideEffect()"
              IL_0012:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0017:  call       "string Test.GetStringWithSideEffect()"
              IL_001c:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0021:  call       "char Test.GetCharWithSideEffect()"
              IL_0026:  stloc.1
              IL_0027:  ldloca.s   V_1
              IL_0029:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_002e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0033:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatFour_ReadOnlySpan_ReferenceToSameLocation(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            var c = new C();
            c.M();

            class C
            {
                public char c = 'a';

                public ref char GetC() => ref c;

                public ref char GetC2()
                {
                    c = 'b';
                    return ref c;
                }

                public void M()
                {
                    Console.Write("a" + c + GetC() + GetC2());
                }
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "aaab" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       65 (0x41)
              .maxstack  4
              .locals init (char V_0,
                            char V_1,
                            char V_2)
              IL_0000:  ldstr      "a"
              IL_0005:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000a:  ldarg.0
              IL_000b:  ldfld      "char C.c"
              IL_0010:  stloc.0
              IL_0011:  ldloca.s   V_0
              IL_0013:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0018:  ldarg.0
              IL_0019:  call       "ref char C.GetC()"
              IL_001e:  ldind.u2
              IL_001f:  stloc.1
              IL_0020:  ldloca.s   V_1
              IL_0022:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0027:  ldarg.0
              IL_0028:  call       "ref char C.GetC2()"
              IL_002d:  ldind.u2
              IL_002e:  stloc.2
              IL_002f:  ldloca.s   V_2
              IL_0031:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0036:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_003b:  call       "void System.Console.Write(string)"
              IL_0040:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatFour_ReadOnlySpan_MutateLocal(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            var c = new C();
            Console.WriteLine(c.M());

            class C
            {
                public string M()
                {
                    var c1 = 'a';
                    var c2 = 'a';
                    return c1 + SneakyLocalChange(ref c1) + c2 + SneakyLocalChange(ref c2);
                }

                private string SneakyLocalChange(ref char local)
                {
                    local = 'b';
                    return "b";
                }
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abab" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("C.M", """
            {
              // Code size       56 (0x38)
              .maxstack  5
              .locals init (char V_0, //c1
                            char V_1, //c2
                            char V_2,
                            char V_3)
              IL_0000:  ldc.i4.s   97
              IL_0002:  stloc.0
              IL_0003:  ldc.i4.s   97
              IL_0005:  stloc.1
              IL_0006:  ldloc.0
              IL_0007:  stloc.2
              IL_0008:  ldloca.s   V_2
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000f:  ldarg.0
              IL_0010:  ldloca.s   V_0
              IL_0012:  call       "string C.SneakyLocalChange(ref char)"
              IL_0017:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_001c:  ldloc.1
              IL_001d:  stloc.3
              IL_001e:  ldloca.s   V_3
              IL_0020:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0025:  ldarg.0
              IL_0026:  ldloca.s   V_1
              IL_0028:  call       "string C.SneakyLocalChange(ref char)"
              IL_002d:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0032:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0037:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatFourCharToStrings(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var c1 = 'a';
                    var c2 = 'b';
                    var c3 = 'c';
                    var c4 = 'd';
                    Console.Write(M(c1, c2, c3, c4));
                }

                static string M(char c1, char c2, char c3, char c4) => c1.ToString() + c2.ToString() + c3.ToString() + c4.ToString();
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcd" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M", """
            {
              // Code size       42 (0x2a)
              .maxstack  4
              .locals init (char V_0,
                            char V_1,
                            char V_2,
                            char V_3)
              IL_0000:  ldarg.0
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0009:  ldarg.1
              IL_000a:  stloc.1
              IL_000b:  ldloca.s   V_1
              IL_000d:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0012:  ldarg.2
              IL_0013:  stloc.2
              IL_0014:  ldloca.s   V_2
              IL_0016:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_001b:  ldarg.3
              IL_001c:  stloc.3
              IL_001d:  ldloca.s   V_3
              IL_001f:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0024:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0029:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpanReadOnlySpan)]
    [InlineData((int)WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
    public void ConcatFour_ReadOnlySpan_MissingMemberForOptimization(int member)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                    Console.Write(M5(s, c));
                    Console.Write(M6(s, c));
                    Console.Write(M7(s, c));
                }

                static string M1(string s, char c) => c + s + s + s;
                static string M2(string s, char c) => s + c + s + s;
                static string M3(string s, char c) => s + s + c + s;
                static string M4(string s, char c) => s + s + s + c;
                static string M5(string s, char c) => c + s + c + s;
                static string M6(string s, char c) => s + c + s + c;
                static string M7(string s, char c) => c + s + s + c;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing((WellKnownMember)member);

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "csssscsssscssssccscsscsccssc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       16 (0x10)
              .maxstack  4
              IL_0000:  ldarga.s   V_1
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  ldarg.0
              IL_0009:  ldarg.0
              IL_000a:  call       "string string.Concat(string, string, string, string)"
              IL_000f:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       16 (0x10)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  ldarga.s   V_1
              IL_0003:  call       "string char.ToString()"
              IL_0008:  ldarg.0
              IL_0009:  ldarg.0
              IL_000a:  call       "string string.Concat(string, string, string, string)"
              IL_000f:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       16 (0x10)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  ldarg.0
              IL_0002:  ldarga.s   V_1
              IL_0004:  call       "string char.ToString()"
              IL_0009:  ldarg.0
              IL_000a:  call       "string string.Concat(string, string, string, string)"
              IL_000f:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       16 (0x10)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  ldarg.0
              IL_0002:  ldarg.0
              IL_0003:  ldarga.s   V_1
              IL_0005:  call       "string char.ToString()"
              IL_000a:  call       "string string.Concat(string, string, string, string)"
              IL_000f:  ret
            }
            """);
        verifier.VerifyIL("Test.M5", """
            {
              // Code size       22 (0x16)
              .maxstack  4
              IL_0000:  ldarga.s   V_1
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  ldarga.s   V_1
              IL_000a:  call       "string char.ToString()"
              IL_000f:  ldarg.0
              IL_0010:  call       "string string.Concat(string, string, string, string)"
              IL_0015:  ret
            }
            """);
        verifier.VerifyIL("Test.M6", """
            {
              // Code size       22 (0x16)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  ldarga.s   V_1
              IL_0003:  call       "string char.ToString()"
              IL_0008:  ldarg.0
              IL_0009:  ldarga.s   V_1
              IL_000b:  call       "string char.ToString()"
              IL_0010:  call       "string string.Concat(string, string, string, string)"
              IL_0015:  ret
            }
            """);
        verifier.VerifyIL("Test.M7", """
            {
              // Code size       22 (0x16)
              .maxstack  4
              IL_0000:  ldarga.s   V_1
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  ldarg.0
              IL_0009:  ldarga.s   V_1
              IL_000b:  call       "string char.ToString()"
              IL_0010:  call       "string string.Concat(string, string, string, string)"
              IL_0015:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatFour_UserInputOfSpanBasedConcatOf2_ConcatWithString(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    Console.Write(M1(s1.AsSpan(), s2, s3));
                    Console.Write(M2(s1.AsSpan(), s2, s3));
                    Console.Write(M3(s1.AsSpan(), s2, s3));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, string s3) => string.Concat(s1, s2.AsSpan()) + s3 + s3;
                static string M2(ReadOnlySpan<char> s1, string s2, string s3) => s3 + s3 + string.Concat(s1, s2.AsSpan());
                static string M3(ReadOnlySpan<char> s1, string s2, string s3) => s3 + string.Concat(s1, s2.AsSpan()) + s3;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abccccabcabc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       25 (0x19)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  ldarg.2
              IL_0008:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000d:  ldarg.2
              IL_000e:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0013:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0018:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       25 (0x19)
              .maxstack  4
              IL_0000:  ldarg.2
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.2
              IL_0007:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000c:  ldarg.0
              IL_000d:  ldarg.1
              IL_000e:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0013:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0018:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       25 (0x19)
              .maxstack  4
              IL_0000:  ldarg.2
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  ldarg.1
              IL_0008:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000d:  ldarg.2
              IL_000e:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0013:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0018:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatFour_UserInputOfSpanBasedConcatOf2_ConcatWithChar(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var c = 'c';
                    Console.Write(M1(s1.AsSpan(), s2, c));
                    Console.Write(M2(s1.AsSpan(), s2, c));
                    Console.Write(M3(s1.AsSpan(), s2, c));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, char c) => string.Concat(s1, s2.AsSpan()) + c + c;
                static string M2(ReadOnlySpan<char> s1, string s2, char c) => c.ToString() + c + string.Concat(s1, s2.AsSpan());
                static string M3(ReadOnlySpan<char> s1, string s2, char c) => c + string.Concat(s1, s2.AsSpan()) + c;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abccccabcabc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       31 (0x1f)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  ldarg.2
              IL_0008:  stloc.0
              IL_0009:  ldloca.s   V_0
              IL_000b:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0010:  ldarg.2
              IL_0011:  stloc.1
              IL_0012:  ldloca.s   V_1
              IL_0014:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0019:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001e:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       31 (0x1f)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.2
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0009:  ldarg.2
              IL_000a:  stloc.1
              IL_000b:  ldloca.s   V_1
              IL_000d:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0012:  ldarg.0
              IL_0013:  ldarg.1
              IL_0014:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0019:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001e:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       31 (0x1f)
              .maxstack  4
              .locals init (char V_0,
                            char V_1)
              IL_0000:  ldarg.2
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0009:  ldarg.0
              IL_000a:  ldarg.1
              IL_000b:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0010:  ldarg.2
              IL_0011:  stloc.1
              IL_0012:  ldloca.s   V_1
              IL_0014:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0019:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001e:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatFour_UserInputOfSpanBasedConcatOf2_ConcatWithStringAndChar(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    var c = 'd';
                    Console.Write(M1(s1.AsSpan(), s2, s3, c));
                    Console.Write(M2(s1.AsSpan(), s2, s3, c));
                    Console.Write(M3(s1.AsSpan(), s2, s3, c));
                    Console.Write(M4(s1.AsSpan(), s2, s3, c));
                    Console.Write(M5(s1.AsSpan(), s2, s3, c));
                    Console.Write(M6(s1.AsSpan(), s2, s3, c));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, string s3, char c) => string.Concat(s1, s2.AsSpan()) + s3 + c;
                static string M2(ReadOnlySpan<char> s1, string s2, string s3, char c) => string.Concat(s1, s2.AsSpan()) + c + s3;
                static string M3(ReadOnlySpan<char> s1, string s2, string s3, char c) => s3 + c + string.Concat(s1, s2.AsSpan());
                static string M4(ReadOnlySpan<char> s1, string s2, string s3, char c) => c + s3 + string.Concat(s1, s2.AsSpan());
                static string M5(ReadOnlySpan<char> s1, string s2, string s3, char c) => s3 + string.Concat(s1, s2.AsSpan()) + c;
                static string M6(ReadOnlySpan<char> s1, string s2, string s3, char c) => c + string.Concat(s1, s2.AsSpan()) + s3;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcdabdccdabdcabcabddabc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       28 (0x1c)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  ldarg.2
              IL_0008:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000d:  ldarg.3
              IL_000e:  stloc.0
              IL_000f:  ldloca.s   V_0
              IL_0011:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0016:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001b:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       28 (0x1c)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  ldarg.3
              IL_0008:  stloc.0
              IL_0009:  ldloca.s   V_0
              IL_000b:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0010:  ldarg.2
              IL_0011:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0016:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001b:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       28 (0x1c)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.2
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.3
              IL_0007:  stloc.0
              IL_0008:  ldloca.s   V_0
              IL_000a:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_000f:  ldarg.0
              IL_0010:  ldarg.1
              IL_0011:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0016:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001b:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       28 (0x1c)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.3
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0009:  ldarg.2
              IL_000a:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000f:  ldarg.0
              IL_0010:  ldarg.1
              IL_0011:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0016:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001b:  ret
            }
            """);
        verifier.VerifyIL("Test.M5", """
            {
              // Code size       28 (0x1c)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.2
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  ldarg.1
              IL_0008:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000d:  ldarg.3
              IL_000e:  stloc.0
              IL_000f:  ldloca.s   V_0
              IL_0011:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0016:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001b:  ret
            }
            """);
        verifier.VerifyIL("Test.M6", """
            {
              // Code size       28 (0x1c)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.3
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0009:  ldarg.0
              IL_000a:  ldarg.1
              IL_000b:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0010:  ldarg.2
              IL_0011:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0016:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001b:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData(WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatFour_TwoUserInputsOfSpanBasedConcatOf2(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    var s4 = "d";
                    Console.Write(M(s1.AsSpan(), s2.AsSpan(), s3.AsSpan(), s4.AsSpan()));
                }

                static string M(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, ReadOnlySpan<char> s3, ReadOnlySpan<char> s4) => string.Concat(s1, s2) + string.Concat(s3, s4);
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcd" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M", """
            {
              // Code size       10 (0xa)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  ldarg.2
              IL_0003:  ldarg.3
              IL_0004:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0009:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
    public void ConcatFour_UserInputOfSpanBasedConcatOf3_ConcatWithString(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    var s4 = "d";
                    Console.Write(M1(s1.AsSpan(), s2, s3.AsSpan(), s4));
                    Console.Write(M2(s1.AsSpan(), s2, s3.AsSpan(), s4));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, ReadOnlySpan<char> s3, string s4) => string.Concat(s1, s2.AsSpan(), s3) + s4;
                static string M2(ReadOnlySpan<char> s1, string s2, ReadOnlySpan<char> s3, string s4) => s4 + string.Concat(s1, s2.AsSpan(), s3);
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcddabc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       20 (0x14)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  ldarg.2
              IL_0008:  ldarg.3
              IL_0009:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_000e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0013:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       20 (0x14)
              .maxstack  4
              IL_0000:  ldarg.3
              IL_0001:  call       "System.ReadOnlySpan<char> string.op_Implicit(string)"
              IL_0006:  ldarg.0
              IL_0007:  ldarg.1
              IL_0008:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000d:  ldarg.2
              IL_000e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0013:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    public void ConcatFour_UserInputOfSpanBasedConcatOf3_ConcatWithChar(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    var c = 'd';
                    Console.Write(M1(s1.AsSpan(), s2, s3.AsSpan(), c));
                    Console.Write(M2(s1.AsSpan(), s2, s3.AsSpan(), c));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, ReadOnlySpan<char> s3, char c) => string.Concat(s1, s2.AsSpan(), s3) + c;
                static string M2(ReadOnlySpan<char> s1, string s2, ReadOnlySpan<char> s3, char c) => c + string.Concat(s1, s2.AsSpan(), s3);
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcddabc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       23 (0x17)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  ldarg.2
              IL_0008:  ldarg.3
              IL_0009:  stloc.0
              IL_000a:  ldloca.s   V_0
              IL_000c:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0011:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0016:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       23 (0x17)
              .maxstack  4
              .locals init (char V_0)
              IL_0000:  ldarg.3
              IL_0001:  stloc.0
              IL_0002:  ldloca.s   V_0
              IL_0004:  newobj     "System.ReadOnlySpan<char>..ctor(in char)"
              IL_0009:  ldarg.0
              IL_000a:  ldarg.1
              IL_000b:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0010:  ldarg.2
              IL_0011:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0016:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan, (int)WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatFour_UserInputOfSpanBasedConcatOf2_ConcatWithString_MissingMembersToMergeIntoSingleConcat(params int[] members)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    Console.Write(M1(s1.AsSpan(), s2, s3));
                    Console.Write(M2(s1.AsSpan(), s2, s3));
                    Console.Write(M3(s1.AsSpan(), s2, s3));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, string s3) => string.Concat(s1, s2.AsSpan()) + s3 + s3;
                static string M2(ReadOnlySpan<char> s1, string s2, string s3) => s3 + s3 + string.Concat(s1, s2.AsSpan());
                static string M3(ReadOnlySpan<char> s1, string s2, string s3) => s3 + string.Concat(s1, s2.AsSpan()) + s3;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        foreach (var member in members)
        {
            comp.MakeMemberMissing((WellKnownMember)member);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abccccabcabc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       20 (0x14)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000c:  ldarg.2
              IL_000d:  ldarg.2
              IL_000e:  call       "string string.Concat(string, string, string)"
              IL_0013:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       20 (0x14)
              .maxstack  4
              IL_0000:  ldarg.2
              IL_0001:  ldarg.2
              IL_0002:  ldarg.0
              IL_0003:  ldarg.1
              IL_0004:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0009:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000e:  call       "string string.Concat(string, string, string)"
              IL_0013:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       20 (0x14)
              .maxstack  3
              IL_0000:  ldarg.2
              IL_0001:  ldarg.0
              IL_0002:  ldarg.1
              IL_0003:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0008:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000d:  ldarg.2
              IL_000e:  call       "string string.Concat(string, string, string)"
              IL_0013:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan, (int)WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatFour_UserInputOfSpanBasedConcatOf2_ConcatWithChar_MissingMembersToMergeIntoSingleConcat(params int[] members)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var c = 'c';
                    Console.Write(M1(s1.AsSpan(), s2, c));
                    Console.Write(M2(s1.AsSpan(), s2, c));
                    Console.Write(M3(s1.AsSpan(), s2, c));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, char c) => string.Concat(s1, s2.AsSpan()) + c + c;
                static string M2(ReadOnlySpan<char> s1, string s2, char c) => c.ToString() + c + string.Concat(s1, s2.AsSpan());
                static string M3(ReadOnlySpan<char> s1, string s2, char c) => c + string.Concat(s1, s2.AsSpan()) + c;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        foreach (var member in members)
        {
            comp.MakeMemberMissing((WellKnownMember)member);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abccccabcabc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       32 (0x20)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000c:  ldarga.s   V_2
              IL_000e:  call       "string char.ToString()"
              IL_0013:  ldarga.s   V_2
              IL_0015:  call       "string char.ToString()"
              IL_001a:  call       "string string.Concat(string, string, string)"
              IL_001f:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       32 (0x20)
              .maxstack  4
              IL_0000:  ldarga.s   V_2
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarga.s   V_2
              IL_0009:  call       "string char.ToString()"
              IL_000e:  ldarg.0
              IL_000f:  ldarg.1
              IL_0010:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  call       "string string.Concat(string, string, string)"
              IL_001f:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       32 (0x20)
              .maxstack  3
              IL_0000:  ldarga.s   V_2
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  ldarg.1
              IL_0009:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0013:  ldarga.s   V_2
              IL_0015:  call       "string char.ToString()"
              IL_001a:  call       "string string.Concat(string, string, string)"
              IL_001f:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)WellKnownMember.System_ReadOnlySpan_T__ctor_Reference, (int)WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan, (int)WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan, (int)WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan, (int)WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatFour_UserInputOfSpanBasedConcatOf2_ConcatWithStringAndChar_MissingMembersToMergeIntoSingleConcat(params int[] members)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    var c = 'd';
                    Console.Write(M1(s1.AsSpan(), s2, s3, c));
                    Console.Write(M2(s1.AsSpan(), s2, s3, c));
                    Console.Write(M3(s1.AsSpan(), s2, s3, c));
                    Console.Write(M4(s1.AsSpan(), s2, s3, c));
                    Console.Write(M5(s1.AsSpan(), s2, s3, c));
                    Console.Write(M6(s1.AsSpan(), s2, s3, c));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, string s3, char c) => string.Concat(s1, s2.AsSpan()) + s3 + c;
                static string M2(ReadOnlySpan<char> s1, string s2, string s3, char c) => string.Concat(s1, s2.AsSpan()) + c + s3;
                static string M3(ReadOnlySpan<char> s1, string s2, string s3, char c) => s3 + c + string.Concat(s1, s2.AsSpan());
                static string M4(ReadOnlySpan<char> s1, string s2, string s3, char c) => c + s3 + string.Concat(s1, s2.AsSpan());
                static string M5(ReadOnlySpan<char> s1, string s2, string s3, char c) => s3 + string.Concat(s1, s2.AsSpan()) + c;
                static string M6(ReadOnlySpan<char> s1, string s2, string s3, char c) => c + string.Concat(s1, s2.AsSpan()) + s3;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        foreach (var member in members)
        {
            comp.MakeMemberMissing((WellKnownMember)member);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcdabdccdabdcabcabddabc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       26 (0x1a)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000c:  ldarg.2
              IL_000d:  ldarga.s   V_3
              IL_000f:  call       "string char.ToString()"
              IL_0014:  call       "string string.Concat(string, string, string)"
              IL_0019:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       26 (0x1a)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000c:  ldarga.s   V_3
              IL_000e:  call       "string char.ToString()"
              IL_0013:  ldarg.2
              IL_0014:  call       "string string.Concat(string, string, string)"
              IL_0019:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       26 (0x1a)
              .maxstack  4
              IL_0000:  ldarg.2
              IL_0001:  ldarga.s   V_3
              IL_0003:  call       "string char.ToString()"
              IL_0008:  ldarg.0
              IL_0009:  ldarg.1
              IL_000a:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000f:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0014:  call       "string string.Concat(string, string, string)"
              IL_0019:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       26 (0x1a)
              .maxstack  4
              IL_0000:  ldarga.s   V_3
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.2
              IL_0008:  ldarg.0
              IL_0009:  ldarg.1
              IL_000a:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000f:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0014:  call       "string string.Concat(string, string, string)"
              IL_0019:  ret
            }
            """);
        verifier.VerifyIL("Test.M5", """
            {
              // Code size       26 (0x1a)
              .maxstack  3
              IL_0000:  ldarg.2
              IL_0001:  ldarg.0
              IL_0002:  ldarg.1
              IL_0003:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0008:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000d:  ldarga.s   V_3
              IL_000f:  call       "string char.ToString()"
              IL_0014:  call       "string string.Concat(string, string, string)"
              IL_0019:  ret
            }
            """);
        verifier.VerifyIL("Test.M6", """
            {
              // Code size       26 (0x1a)
              .maxstack  3
              IL_0000:  ldarga.s   V_3
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  ldarg.1
              IL_0009:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000e:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0013:  ldarg.2
              IL_0014:  call       "string string.Concat(string, string, string)"
              IL_0019:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatFour_TwoUserInputsOfSpanBasedConcatOf2_MissingMemberToMergeIntoSingleConcat(int member)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    var s4 = "d";
                    Console.Write(M(s1.AsSpan(), s2.AsSpan(), s3.AsSpan(), s4.AsSpan()));
                }

                static string M(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, ReadOnlySpan<char> s3, ReadOnlySpan<char> s4) => string.Concat(s1, s2) + string.Concat(s3, s4);
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing((WellKnownMember)member);

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcd" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M", """
            {
              // Code size       20 (0x14)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0007:  ldarg.2
              IL_0008:  ldarg.3
              IL_0009:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000e:  call       "string string.Concat(string, string)"
              IL_0013:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData((int)WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatFour_UserInputOfSpanBasedConcatOf3_ConcatWithString_MissingMemberToMergeIntoSingleConcat(int member)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    var s4 = "d";
                    Console.Write(M1(s1.AsSpan(), s2, s3.AsSpan(), s4));
                    Console.Write(M2(s1.AsSpan(), s2, s3.AsSpan(), s4));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, ReadOnlySpan<char> s3, string s4) => string.Concat(s1, s2.AsSpan(), s3) + s4;
                static string M2(ReadOnlySpan<char> s1, string s2, ReadOnlySpan<char> s3, string s4) => s4 + string.Concat(s1, s2.AsSpan(), s3);
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing((WellKnownMember)member);

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcddabc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       20 (0x14)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  ldarg.2
              IL_0008:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000d:  ldarg.3
              IL_000e:  call       "string string.Concat(string, string)"
              IL_0013:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       20 (0x14)
              .maxstack  4
              IL_0000:  ldarg.3
              IL_0001:  ldarg.0
              IL_0002:  ldarg.1
              IL_0003:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0008:  ldarg.2
              IL_0009:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000e:  call       "string string.Concat(string, string)"
              IL_0013:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData((int)WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData((int)WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatFour_UserInputOfSpanBasedConcatOf3_ConcatWithChar_MissingMemberToMergeIntoSingleConcat(int member)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s1 = "a";
                    var s2 = "b";
                    var s3 = "c";
                    var c = 'd';
                    Console.Write(M1(s1.AsSpan(), s2, s3.AsSpan(), c));
                    Console.Write(M2(s1.AsSpan(), s2, s3.AsSpan(), c));
                }

                static string M1(ReadOnlySpan<char> s1, string s2, ReadOnlySpan<char> s3, char c) => string.Concat(s1, s2.AsSpan(), s3) + c;
                static string M2(ReadOnlySpan<char> s1, string s2, ReadOnlySpan<char> s3, char c) => c + string.Concat(s1, s2.AsSpan(), s3);
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing((WellKnownMember)member);

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcddabc" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       26 (0x1a)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  ldarg.1
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  ldarg.2
              IL_0008:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_000d:  ldarga.s   V_3
              IL_000f:  call       "string char.ToString()"
              IL_0014:  call       "string string.Concat(string, string)"
              IL_0019:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       26 (0x1a)
              .maxstack  4
              IL_0000:  ldarga.s   V_3
              IL_0002:  call       "string char.ToString()"
              IL_0007:  ldarg.0
              IL_0008:  ldarg.1
              IL_0009:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000e:  ldarg.2
              IL_000f:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0014:  call       "string string.Concat(string, string)"
              IL_0019:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatFour_MissingObjectToString()
    {
        var source = """
            using System;
            
            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                    Console.Write(M5(s, c));
                    Console.Write(M6(s, c));
                    Console.Write(M7(s, c));
                }
            
                static string M1(string s, char c) => c + s + s + s;
                static string M2(string s, char c) => s + c + s + s;
                static string M3(string s, char c) => s + s + c + s;
                static string M4(string s, char c) => s + s + s + c;
                static string M5(string s, char c) => c + s + c + s;
                static string M6(string s, char c) => s + c + s + c;
                static string M7(string s, char c) => c + s + s + c;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing(SpecialMember.System_Object__ToString);

        // Although we don't use object.ToString() or char.ToString() in the final codegen we still need object.ToString() during lowering.
        // Moreover, we previously reported these errors anyway, so this is not a behavioral change
        comp.VerifyEmitDiagnostics(
            // (18,43): error CS0656: Missing compiler required member 'System.Object.ToString'
            //     static string M1(string s, char c) => c + s + s + s;
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(18, 43),
            // (19,47): error CS0656: Missing compiler required member 'System.Object.ToString'
            //     static string M2(string s, char c) => s + c + s + s;
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(19, 47),
            // (20,51): error CS0656: Missing compiler required member 'System.Object.ToString'
            //     static string M3(string s, char c) => s + s + c + s;
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(20, 51),
            // (21,55): error CS0656: Missing compiler required member 'System.Object.ToString'
            //     static string M4(string s, char c) => s + s + s + c;
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(21, 55),
            // (22,43): error CS0656: Missing compiler required member 'System.Object.ToString'
            //     static string M5(string s, char c) => c + s + c + s;
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(22, 43),
            // (22,51): error CS0656: Missing compiler required member 'System.Object.ToString'
            //     static string M5(string s, char c) => c + s + c + s;
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(22, 51),
            // (23,47): error CS0656: Missing compiler required member 'System.Object.ToString'
            //     static string M6(string s, char c) => s + c + s + c;
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(23, 47),
            // (23,55): error CS0656: Missing compiler required member 'System.Object.ToString'
            //     static string M6(string s, char c) => s + c + s + c;
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(23, 55),
            // (24,43): error CS0656: Missing compiler required member 'System.Object.ToString'
            //     static string M7(string s, char c) => c + s + s + c;
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(24, 43),
            // (24,55): error CS0656: Missing compiler required member 'System.Object.ToString'
            //     static string M7(string s, char c) => c + s + s + c;
            Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "c").WithArguments("System.Object", "ToString").WithLocation(24, 55));
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData(WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatFive_Char(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(CharInFirstFourArgs(s, c));
                    Console.Write(CharAfterFirstFourArgs(s, c));
                }

                static string CharInFirstFourArgs(string s, char c) => s + c + s + s + s;
                static string CharAfterFirstFourArgs(string s, char c) => s + s + s + s + c;
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "scsssssssc" : null, verify: ExecutionConditionUtil.IsCoreClr ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();

        // When lengths of inputs are low it is actually more optimal to first concat 4 operands with span-based concat and then concat that result with the remaining operand.
        // However when inputs become large enough cost of allocating a params array becomes lower than cost of creating an intermediate string.
        // + code size for using params array is less than code size from intermediate concat approach, especially when the amount of operands is high.
        // So in the end we always prefer overload with params array when there are 5+ operands
        verifier.VerifyIL("Test.CharInFirstFourArgs", """
            {
              // Code size       38 (0x26)
              .maxstack  4
              IL_0000:  ldc.i4.5
              IL_0001:  newarr     "string"
              IL_0006:  dup
              IL_0007:  ldc.i4.0
              IL_0008:  ldarg.0
              IL_0009:  stelem.ref
              IL_000a:  dup
              IL_000b:  ldc.i4.1
              IL_000c:  ldarga.s   V_1
              IL_000e:  call       "string char.ToString()"
              IL_0013:  stelem.ref
              IL_0014:  dup
              IL_0015:  ldc.i4.2
              IL_0016:  ldarg.0
              IL_0017:  stelem.ref
              IL_0018:  dup
              IL_0019:  ldc.i4.3
              IL_001a:  ldarg.0
              IL_001b:  stelem.ref
              IL_001c:  dup
              IL_001d:  ldc.i4.4
              IL_001e:  ldarg.0
              IL_001f:  stelem.ref
              IL_0020:  call       "string string.Concat(params string[])"
              IL_0025:  ret
            }
            """);
        verifier.VerifyIL("Test.CharAfterFirstFourArgs", """
            {
              // Code size       38 (0x26)
              .maxstack  4
              IL_0000:  ldc.i4.5
              IL_0001:  newarr     "string"
              IL_0006:  dup
              IL_0007:  ldc.i4.0
              IL_0008:  ldarg.0
              IL_0009:  stelem.ref
              IL_000a:  dup
              IL_000b:  ldc.i4.1
              IL_000c:  ldarg.0
              IL_000d:  stelem.ref
              IL_000e:  dup
              IL_000f:  ldc.i4.2
              IL_0010:  ldarg.0
              IL_0011:  stelem.ref
              IL_0012:  dup
              IL_0013:  ldc.i4.3
              IL_0014:  ldarg.0
              IL_0015:  stelem.ref
              IL_0016:  dup
              IL_0017:  ldc.i4.4
              IL_0018:  ldarga.s   V_1
              IL_001a:  call       "string char.ToString()"
              IL_001f:  stelem.ref
              IL_0020:  call       "string string.Concat(params string[])"
              IL_0025:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData("(s + s) + c + s + s")]
    [InlineData("s + (s + c) + s + s")]
    [InlineData("s + s + (c + s) + s")]
    [InlineData("s + s + c + (s + s)")]
    [InlineData("(s + s + c) + s + s")]
    [InlineData("s + (s + c + s) + s")]
    [InlineData("s + s + (c + s + s)")]
    [InlineData("(s + s + c + s) + s")]
    [InlineData("s + (s + c + s + s)")]
    [InlineData("(s + s) + (c + s) + s")]
    [InlineData("(s + s) + c + (s + s)")]
    [InlineData("s + (s + c) + (s + s)")]
    [InlineData("(s + s + c) + (s + s)")]
    [InlineData("(s + s) + (c + s + s)")]
    [InlineData("string.Concat(s, s) + c + s + s")]
    [InlineData("s + string.Concat(s, c.ToString()) + s + s")]
    [InlineData("s + s + string.Concat(c.ToString(), s) + s")]
    [InlineData("s + s + c + string.Concat(s, s)")]
    [InlineData("string.Concat(s, s, c.ToString()) + s + s")]
    [InlineData("s + string.Concat(s, c.ToString(), s) + s")]
    [InlineData("s + s + string.Concat(c.ToString(), s, s)")]
    [InlineData("string.Concat(s, s, c.ToString(), s) + s")]
    [InlineData("s + string.Concat(s, c.ToString(), s, s)")]
    [InlineData("string.Concat(s, s) + string.Concat(c.ToString(), s) + s")]
    [InlineData("string.Concat(s, s) + c + string.Concat(s, s)")]
    [InlineData("s + string.Concat(s, c.ToString()) + string.Concat(s, s)")]
    [InlineData("string.Concat(s, s, c.ToString()) + string.Concat(s, s)")]
    [InlineData("string.Concat(s, s) + string.Concat(c.ToString(), s, s)")]
    public void ConcatFive_Char_OperandGroupingAndUserInputOfStringBasedConcats(string expression)
    {
        var source = $$"""
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M(s, c));
                }

                static string M(string s, char c) => {{expression}};
            }
            """;

        var comp = CompileAndVerify(source, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "sscss" : null, targetFramework: TargetFramework.Net80, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        comp.VerifyDiagnostics();
        comp.VerifyIL("Test.M", """
            {
              // Code size       38 (0x26)
              .maxstack  4
              IL_0000:  ldc.i4.5
              IL_0001:  newarr     "string"
              IL_0006:  dup
              IL_0007:  ldc.i4.0
              IL_0008:  ldarg.0
              IL_0009:  stelem.ref
              IL_000a:  dup
              IL_000b:  ldc.i4.1
              IL_000c:  ldarg.0
              IL_000d:  stelem.ref
              IL_000e:  dup
              IL_000f:  ldc.i4.2
              IL_0010:  ldarga.s   V_1
              IL_0012:  call       "string char.ToString()"
              IL_0017:  stelem.ref
              IL_0018:  dup
              IL_0019:  ldc.i4.3
              IL_001a:  ldarg.0
              IL_001b:  stelem.ref
              IL_001c:  dup
              IL_001d:  ldc.i4.4
              IL_001e:  ldarg.0
              IL_001f:  stelem.ref
              IL_0020:  call       "string string.Concat(params string[])"
              IL_0025:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData(WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatFiveCharToStrings(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var c1 = 'a';
                    var c2 = 'b';
                    var c3 = 'c';
                    var c4 = 'd';
                    var c5 = 'e';
                    Console.Write(M(c1, c2, c3, c4, c5));
                }

                static string M(char c1, char c2, char c3, char c4, char c5) => c1.ToString() + c2.ToString() + c3.ToString() + c4.ToString() + c5.ToString();
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "abcde" : null, verify: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();
        verifier.VerifyIL("Test.M", """
            {
              // Code size       62 (0x3e)
              .maxstack  4
              IL_0000:  ldc.i4.5
              IL_0001:  newarr     "string"
              IL_0006:  dup
              IL_0007:  ldc.i4.0
              IL_0008:  ldarga.s   V_0
              IL_000a:  call       "string char.ToString()"
              IL_000f:  stelem.ref
              IL_0010:  dup
              IL_0011:  ldc.i4.1
              IL_0012:  ldarga.s   V_1
              IL_0014:  call       "string char.ToString()"
              IL_0019:  stelem.ref
              IL_001a:  dup
              IL_001b:  ldc.i4.2
              IL_001c:  ldarga.s   V_2
              IL_001e:  call       "string char.ToString()"
              IL_0023:  stelem.ref
              IL_0024:  dup
              IL_0025:  ldc.i4.3
              IL_0026:  ldarga.s   V_3
              IL_0028:  call       "string char.ToString()"
              IL_002d:  stelem.ref
              IL_002e:  dup
              IL_002f:  ldc.i4.4
              IL_0030:  ldarga.s   V_4
              IL_0032:  call       "string char.ToString()"
              IL_0037:  stelem.ref
              IL_0038:  call       "string string.Concat(params string[])"
              IL_003d:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData(WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpan)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatTotalOfFive_UserInputOfSpanBasedConcatOf2(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                    Console.Write(M4(s, c));
                }

                static string M1(string s, char c) => string.Concat(s.AsSpan(), s.AsSpan()) + c + s + s;
                static string M2(string s, char c) => s + string.Concat(s.AsSpan(), s.AsSpan()) + s + c;
                static string M3(string s, char c) => s + s + c + string.Concat(s.AsSpan(), s.AsSpan());
                static string M4(string s, char c) => string.Concat(s.AsSpan(), s.AsSpan()) + c + string.Concat(s.AsSpan(), s.AsSpan());
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "sscsssssscsscsssscss" : null, verify: ExecutionConditionUtil.IsCoreClr ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();

        // Since combined length of arguments if we unwrap all user-provided span-based concats is >4,
        // we just lower outer concat as is without unwrapping any user-provided ones.
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       32 (0x20)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000c:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0011:  ldarga.s   V_1
              IL_0013:  call       "string char.ToString()"
              IL_0018:  ldarg.0
              IL_0019:  ldarg.0
              IL_001a:  call       "string string.Concat(string, string, string, string)"
              IL_001f:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       32 (0x20)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  ldarg.0
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  ldarg.0
              IL_0008:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000d:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0012:  ldarg.0
              IL_0013:  ldarga.s   V_1
              IL_0015:  call       "string char.ToString()"
              IL_001a:  call       "string string.Concat(string, string, string, string)"
              IL_001f:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       32 (0x20)
              .maxstack  5
              IL_0000:  ldarg.0
              IL_0001:  ldarg.0
              IL_0002:  ldarga.s   V_1
              IL_0004:  call       "string char.ToString()"
              IL_0009:  ldarg.0
              IL_000a:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000f:  ldarg.0
              IL_0010:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0015:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001a:  call       "string string.Concat(string, string, string, string)"
              IL_001f:  ret
            }
            """);
        verifier.VerifyIL("Test.M4", """
            {
              // Code size       47 (0x2f)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000c:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0011:  ldarga.s   V_1
              IL_0013:  call       "string char.ToString()"
              IL_0018:  ldarg.0
              IL_0019:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_001e:  ldarg.0
              IL_001f:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0024:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0029:  call       "string string.Concat(string, string, string)"
              IL_002e:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData(WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpan)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatTotalOfFive_UserInputOfSpanBasedConcatOf3(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    var c = 'c';
                    Console.Write(M1(s, c));
                    Console.Write(M2(s, c));
                    Console.Write(M3(s, c));
                }

                static string M1(string s, char c) => string.Concat(s.AsSpan(), s.AsSpan(), s.AsSpan()) + c + s;
                static string M2(string s, char c) => s + string.Concat(s.AsSpan(), s.AsSpan(), s.AsSpan()) + c;
                static string M3(string s, char c) => s + c + string.Concat(s.AsSpan(), s.AsSpan(), s.AsSpan());
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "ssscssssscscsss" : null, verify: ExecutionConditionUtil.IsCoreClr ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();

        // Since combined length of arguments if we unwrap all user-provided span-based concats is >4,
        // we just lower outer concat as is without unwrapping any user-provided ones.
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       37 (0x25)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000c:  ldarg.0
              IL_000d:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0012:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0017:  ldarga.s   V_1
              IL_0019:  call       "string char.ToString()"
              IL_001e:  ldarg.0
              IL_001f:  call       "string string.Concat(string, string, string)"
              IL_0024:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       37 (0x25)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  ldarg.0
              IL_0002:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0007:  ldarg.0
              IL_0008:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000d:  ldarg.0
              IL_000e:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0013:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0018:  ldarga.s   V_1
              IL_001a:  call       "string char.ToString()"
              IL_001f:  call       "string string.Concat(string, string, string)"
              IL_0024:  ret
            }
            """);
        verifier.VerifyIL("Test.M3", """
            {
              // Code size       37 (0x25)
              .maxstack  5
              IL_0000:  ldarg.0
              IL_0001:  ldarga.s   V_1
              IL_0003:  call       "string char.ToString()"
              IL_0008:  ldarg.0
              IL_0009:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000e:  ldarg.0
              IL_000f:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0014:  ldarg.0
              IL_0015:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_001a:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001f:  call       "string string.Concat(string, string, string)"
              IL_0024:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData(WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
    [InlineData(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpanReadOnlySpan)]
    public void ConcatTotalOfFive_UserInputOfMixedSpanBasedConcatsOf2And3(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    Console.Write(M1(s));
                    Console.Write(M2(s));
                }

                static string M1(string s) => string.Concat(s.AsSpan(), s.AsSpan()) + string.Concat(s.AsSpan(), s.AsSpan(), s.AsSpan());
                static string M2(string s) => string.Concat(s.AsSpan(), s.AsSpan(), s.AsSpan()) + string.Concat(s.AsSpan(), s.AsSpan());
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "ssssssssss" : null, verify: ExecutionConditionUtil.IsCoreClr ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();

        // Since combined length of arguments if we unwrap all user-provided span-based concats is >4,
        // we just lower outer concat as is without unwrapping any user-provided ones.
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       46 (0x2e)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000c:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0011:  ldarg.0
              IL_0012:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0017:  ldarg.0
              IL_0018:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_001d:  ldarg.0
              IL_001e:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0023:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0028:  call       "string string.Concat(string, string)"
              IL_002d:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       46 (0x2e)
              .maxstack  3
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000c:  ldarg.0
              IL_000d:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0012:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0017:  ldarg.0
              IL_0018:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_001d:  ldarg.0
              IL_001e:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0023:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0028:  call       "string string.Concat(string, string)"
              IL_002d:  ret
            }
            """);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    [InlineData(null)]
    [InlineData(WellKnownMember.System_String__op_Implicit_ToReadOnlySpanOfChar)]
    [InlineData(WellKnownMember.System_ReadOnlySpan_T__ctor_Reference)]
    public void ConcatTotalOfMoreThanFive_UserInputOf2SpanBasedConcatsOf2InARow(int? missingUnimportantWellKnownMember)
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    Console.Write(M1(s));
                    Console.Write(M2(s));
                }

                static string M1(string s) => string.Concat(s.AsSpan(), s.AsSpan()) + string.Concat(s.AsSpan(), s.AsSpan()) + string.Concat(s.AsSpan(), s.AsSpan(), s.AsSpan());
                static string M2(string s) => string.Concat(s.AsSpan(), s.AsSpan(), s.AsSpan()) + string.Concat(s.AsSpan(), s.AsSpan()) + string.Concat(s.AsSpan(), s.AsSpan());
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);

        if (missingUnimportantWellKnownMember.HasValue)
        {
            comp.MakeMemberMissing((WellKnownMember)missingUnimportantWellKnownMember.Value);
        }

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "ssssssssssssss" : null, verify: ExecutionConditionUtil.IsCoreClr ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();

        // This is an edge case where due to order of processing arguments during rewriting we manage to merge 2 concats of 2 into a single concat of 4 in the first case (which changes what we get in the end),
        // but in the second case we leave things untouched. Both lowerings produce correct output in the end, so this isn't much to worry about
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       58 (0x3a)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000c:  ldarg.0
              IL_000d:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0012:  ldarg.0
              IL_0013:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0018:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_001d:  ldarg.0
              IL_001e:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0023:  ldarg.0
              IL_0024:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0029:  ldarg.0
              IL_002a:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_002f:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0034:  call       "string string.Concat(string, string)"
              IL_0039:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       63 (0x3f)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000c:  ldarg.0
              IL_000d:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0012:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0017:  ldarg.0
              IL_0018:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_001d:  ldarg.0
              IL_001e:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0023:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0028:  ldarg.0
              IL_0029:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_002e:  ldarg.0
              IL_002f:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0034:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0039:  call       "string string.Concat(string, string, string)"
              IL_003e:  ret
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66827")]
    public void ConcatTotalOfMoreThanFive_UserInputOf2SpanBasedConcatsOf2InARow_MissingSpanBasedConcatOf4()
    {
        var source = """
            using System;

            public class Test
            {
                static void Main()
                {
                    var s = "s";
                    Console.Write(M1(s));
                    Console.Write(M2(s));
                }

                static string M1(string s) => string.Concat(s.AsSpan(), s.AsSpan()) + string.Concat(s.AsSpan(), s.AsSpan()) + string.Concat(s.AsSpan(), s.AsSpan(), s.AsSpan());
                static string M2(string s) => string.Concat(s.AsSpan(), s.AsSpan(), s.AsSpan()) + string.Concat(s.AsSpan(), s.AsSpan()) + string.Concat(s.AsSpan(), s.AsSpan());
            }
            """;

        var comp = CreateCompilation(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net80);
        comp.MakeMemberMissing(WellKnownMember.System_String__Concat_ReadOnlySpanReadOnlySpanReadOnlySpanReadOnlySpan);

        var verifier = CompileAndVerify(compilation: comp, expectedOutput: RuntimeUtilities.IsCoreClr8OrHigherRuntime ? "ssssssssssssss" : null, verify: ExecutionConditionUtil.IsCoreClr ? default : Verification.Skipped);

        verifier.VerifyDiagnostics();

        // If span-based concat of 4 is missing we produce Il equivalent to what user has written without any changes
        verifier.VerifyIL("Test.M1", """
            {
              // Code size       63 (0x3f)
              .maxstack  5
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000c:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0011:  ldarg.0
              IL_0012:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0017:  ldarg.0
              IL_0018:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_001d:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0022:  ldarg.0
              IL_0023:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0028:  ldarg.0
              IL_0029:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_002e:  ldarg.0
              IL_002f:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0034:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0039:  call       "string string.Concat(string, string, string)"
              IL_003e:  ret
            }
            """);
        verifier.VerifyIL("Test.M2", """
            {
              // Code size       63 (0x3f)
              .maxstack  4
              IL_0000:  ldarg.0
              IL_0001:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0006:  ldarg.0
              IL_0007:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_000c:  ldarg.0
              IL_000d:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0012:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0017:  ldarg.0
              IL_0018:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_001d:  ldarg.0
              IL_001e:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0023:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0028:  ldarg.0
              IL_0029:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_002e:  ldarg.0
              IL_002f:  call       "System.ReadOnlySpan<char> System.MemoryExtensions.AsSpan(string)"
              IL_0034:  call       "string string.Concat(System.ReadOnlySpan<char>, System.ReadOnlySpan<char>)"
              IL_0039:  call       "string string.Concat(string, string, string)"
              IL_003e:  ret
            }
            """);
    }
}
