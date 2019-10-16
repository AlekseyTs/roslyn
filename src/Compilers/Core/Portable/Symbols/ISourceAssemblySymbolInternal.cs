// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis
{
    internal interface ISourceAssemblySymbolInternal : IAssemblySymbolInternal
    {
        AssemblyFlags AssemblyFlags { get; }

        /// <summary>
        /// The contents of the AssemblySignatureKeyAttribute
        /// </summary>
        string SignatureKey { get; }

        AssemblyHashAlgorithm HashAlgorithm { get; }

        bool InternalsAreVisible { get; }
    }
}
