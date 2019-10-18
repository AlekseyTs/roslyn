// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class SymbolInfoFactory
    {
        internal static SymbolInfo Create(ImmutableArray<Symbol> symbols, LookupResultKind resultKind, bool isDynamic)
        {
            if (isDynamic)
            {
                if (symbols.Length == 1)
                {
                    return new SymbolInfo(symbols[0].GetPublicSymbol<ISymbol>(), CandidateReason.LateBound);
                }
                else
                {
                    return new SymbolInfo(symbols.SelectAsArray(s => s.GetPublicSymbol<ISymbol>()), CandidateReason.LateBound);
                }
            }
            else if (resultKind == LookupResultKind.Viable)
            {
                if (symbols.Length > 0)
                {
                    Debug.Assert(symbols.Length == 1);
                    return new SymbolInfo(symbols[0].GetPublicSymbol<ISymbol>());
                }
                else
                {
                    return SymbolInfo.None;
                }
            }
            else
            {
                return new SymbolInfo(symbols.SelectAsArray(s => s.GetPublicSymbol<ISymbol>()), (symbols.Length > 0) ? resultKind.ToCandidateReason() : CandidateReason.None);
            }
        }
    }
}
