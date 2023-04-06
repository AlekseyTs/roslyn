// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundFixedSizeBufferElementAccess
    {
#if DEBUG
        private partial void Validate()
        {
            Debug.Assert(!IsValue || GetItemOrSliceHelper == WellKnownMember.System_ReadOnlySpan_T__get_Item);
            Debug.Assert(GetSpanOrReadOnlySpanHelper is object || Type == (object)ErrorTypeSymbol.UnknownResultType);
#pragma warning disable format
            Debug.Assert(Argument.Type is
                             { SpecialType: SpecialType.System_Int32 } or
                             NamedTypeSymbol
                                 {
                                     ContainingSymbol: NamespaceSymbol
                                                       {
                                                           Name: "System",
                                                           ContainingSymbol: NamespaceSymbol { IsGlobalNamespace: true }
                                                       },
                                     Name: "Index" or "Range",
                                     IsGenericType: false
                                 }
                             );
#pragma warning restore format

            if (Argument.Type.Name == "Range")
            {
                Debug.Assert(GetItemOrSliceHelper is
                                WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int or
                                WellKnownMember.System_Span_T__Slice_Int_Int);
            }
            else
            {
                Debug.Assert(GetItemOrSliceHelper is
                                WellKnownMember.System_ReadOnlySpan_T__get_Item or
                                WellKnownMember.System_Span_T__get_Item);
            }
        }
#endif
    }
}

