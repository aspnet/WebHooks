// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web.Http.Controllers;
using Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Events;

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks
{
    class TfsWebHookReceiver : WebHookReceiver
    {
        internal const string RecName = "tfs";

        static readonly Dictionary<string, Type> _mapping = new Dictionary<string, Type>()
        {
            { "workitem.updated", typeof(WorkItemUpdatedEvent) },
            { "workitem.restored", typeof(WorkItemRestoredEvent) },
            { "workitem.deleted", typeof(WorkItemDeletedEvent) },
            { "workitem.created", typeof(WorkItemCreatedEvent) },
            { "workitem.commented", typeof(WorkItemCommentedOnEvent) },
            { "message.posted", typeof(TeamRoomMessagePostedEvent) },
            { "tfvc.checkin", typeof(CodeCheckedInEvent) },
            { "build.complete", typeof(BuildCompletedEvent) }
        };

        /// <summary>
        /// Gets the receiver name for this receiver.
        /// </summary>
        public static string ReceiverName
        {
            get { return RecName; }
        }

        /// <inheritdoc />
        public override string Name
        {
            get { return RecName; }
        }

        public override Task<HttpResponseMessage> ReceiveAsync(string id, HttpRequestContext context, HttpRequestMessage request)
        {
            throw new NotImplementedException();
        }
    }
}
