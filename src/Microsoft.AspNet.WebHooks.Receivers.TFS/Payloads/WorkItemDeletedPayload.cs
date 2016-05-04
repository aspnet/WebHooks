﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Payloads
{
    /// <summary>
    /// Describes the entire payload of event '<c>workitem.deleted</c>'.
    /// </summary>
    public class WorkItemDeletedPayload : BasePayload<WorkItemDeletedResource>
    {
    }

    /// <summary>
    /// Describes the resource that associated with <see cref="WorkItemDeletedPayload"/>
    /// </summary>
    public class WorkItemDeletedResource : BaseWorkItemResource<WorkItemFields>
    {
    }
}
