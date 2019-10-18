// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal abstract class NamespaceOrTypeSymbol : Symbol, INamespaceOrTypeSymbol
    {
        internal abstract Symbols.NamespaceOrTypeSymbol UnderlyingNamespaceOrTypeSymbol { get; }

        ImmutableArray<ISymbol> INamespaceOrTypeSymbol.GetMembers()
        {
            return UnderlyingNamespaceOrTypeSymbol.GetMembers().SelectAsArray(m => m.GetPublicSymbol<ISymbol>());
        }

        ImmutableArray<ISymbol> INamespaceOrTypeSymbol.GetMembers(string name)
        {
            return UnderlyingNamespaceOrTypeSymbol.GetMembers(name).SelectAsArray(m => m.GetPublicSymbol<ISymbol>());
        }

        ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers()
        {
            return UnderlyingNamespaceOrTypeSymbol.GetTypeMembers().SelectAsArray(m => m.GetPublicSymbol<INamedTypeSymbol>());
        }

        ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers(string name)
        {
            return UnderlyingNamespaceOrTypeSymbol.GetTypeMembers(name).SelectAsArray(m => m.GetPublicSymbol<INamedTypeSymbol>());
        }

        ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers(string name, int arity)
        {
            return UnderlyingNamespaceOrTypeSymbol.GetTypeMembers(name, arity).SelectAsArray(m => m.GetPublicSymbol<INamedTypeSymbol>());
        }

        bool INamespaceOrTypeSymbol.IsNamespace => UnderlyingSymbol.Kind == SymbolKind.Namespace;

        bool INamespaceOrTypeSymbol.IsType => UnderlyingSymbol.Kind != SymbolKind.Namespace;
    }
}
