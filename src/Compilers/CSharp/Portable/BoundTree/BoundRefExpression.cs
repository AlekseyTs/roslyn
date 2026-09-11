// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundRefExpression
    {
        private partial void Validate()
        {
            Debug.Assert(RefKind is RefKind.None or RefKind.Ref or RefKind.In or RefKind.Out);
            Debug.Assert(Expression is not BoundRefExpression);
        }
    }
}
