// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.WebHooks.Payloads
{
    /// <summary>
    /// Package metadata.
    /// </summary>
    public class PackageMetadata
    {
        /// <summary>
        /// Icon URL.
        /// </summary>
        public Uri IconUrl { get; set; }

        /// <summary>
        /// Package size in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Authors.
        /// </summary>
        public string Authors { get; set; }

        /// <summary>
        /// Description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// License URL.
        /// </summary>
        public Uri LicenseUrl { get; set; }

        /// <summary>
        /// License name(s).
        /// </summary>
        public string LicenseNames { get; set; }

        /// <summary>
        /// Project URL.
        /// </summary>
        public Uri ProjectUrl { get; set; }

        /// <summary>
        /// Tags.
        /// </summary>
        public string Tags { get; set; }

        /// <summary>
        /// Minimal client version, if applicable.
        /// </summary>
        public string MinClientVersion { get; set; }

        /// <summary>
        /// Release notes.
        /// </summary>
        public string ReleaseNotes { get; set; }

        /// <summary>
        /// Package dependencies. May contain duplicate id/version if there are different framework dependencies.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "JSON.NET should be able to set the data.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "JSON.NET should be able to set the data.")]
        public List<Package> Dependencies { get; set; }
    }
}