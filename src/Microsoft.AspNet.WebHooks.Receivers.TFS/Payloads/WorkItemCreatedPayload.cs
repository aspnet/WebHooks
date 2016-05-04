// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Payloads
{
    public class WorkItemCreatedPayload : BasePayload<WorkItemCreatedResource>
    {        
    }

    public class WorkItemCreatedResource : BaseWorkItemResource<WorkItemFields>
    {
    }
}
