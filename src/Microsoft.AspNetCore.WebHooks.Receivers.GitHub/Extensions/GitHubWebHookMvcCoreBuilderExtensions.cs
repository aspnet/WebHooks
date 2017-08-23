// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel;

namespace Microsoft.Extensions.DependencyInjection
{
    // ??? Should we remove provide do-nothing wrappers? I can see including them where the receiver requires specific
    // ??? configuration but am less comfortable with providing the wrappers by default.
    /// <summary>
    /// Extension methods for <see cref="HttpConfiguration"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class GitHubWebHookMvcCoreBuilderExtensions
    {
    }
}