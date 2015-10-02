// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.WebHooks.Payloads
{
    /// <summary>
    /// Payload sent when a build has finished.
    /// </summary>
    public class BuildFinishedPayload
    {
        /// <summary>
        /// Name of the build source.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Branch.
        /// </summary>
        public string Branch { get; set; }
        
        /// <summary>
        /// Build result. Will contain "failed" or "success".
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// Feed.
        /// </summary>
        public string FeedIdentifier { get; set; }

        /// <summary>
        /// Feed URL.
        /// </summary>
        public Uri FeedUrl { get; set; }

        /// <summary>
        /// URL to the build log.
        /// </summary>
        public Uri BuildLogUrl { get; set; }

        /// <summary>
        /// Packages that have been created.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "JSON.NET should be able to set the data.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "JSON.NET should be able to set the data.")]
        public List<Package> Packages { get; set; }
    }
}