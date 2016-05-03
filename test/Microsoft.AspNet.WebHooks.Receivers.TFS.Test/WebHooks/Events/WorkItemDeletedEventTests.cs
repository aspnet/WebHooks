﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Events;
using Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNet.WebHooks
{
    public class WorkItemDeletedEventTests
    {
        [Fact]
        public void WorkItemDeletedEvent_Roundtrips()
        {
            // Arrange
            JObject data = EmbeddedResource.ReadAsJObject("Microsoft.AspNet.WebHooks.Messages.workitem.deleted.json");
            var expected = new WorkItemDeletedEvent
            {
                SubscriptionId = "00000000-0000-0000-0000-000000000000",
                NotificationId = 6,
                Id = "72da0ade-0709-40ee-beb7-104287bf7e84",
                EventType = "workitem.deleted",
                PublisherId = "tfs",
                Message = new TfsEventMessage
                {
                    Text = "Bug #5 (Some great new idea!) deleted by Jamal Hartnett.",
                    Html = "Bug #5 (Some great new idea!) deleted by Jamal Hartnett.",
                    Markdown = "[Bug #5] (Some great new idea!) deleted by Jamal Hartnett."
                },
                DetailedMessage = new TfsEventMessage
                {
                    Text = "Bug #5 (Some great new idea!) deleted by Jamal Hartnett.\r\n\r\n- Area: FabrikamCloud\r\n- Iteration: FabrikamCloud\\Release 1\\Sprint 1\r\n- State: New\r\n",
                    Html = "Bug #5 (Some great new idea!) deleted by Jamal Hartnett.<ul>\r\n<li>Area: FabrikamCloud</li>\r\n<li>Iteration: FabrikamCloud\\Release 1\\Sprint 1</li>\r\n<li>State: New</li></ul>",
                    Markdown = "[Bug #5] (Some great new idea!) deleted by Jamal Hartnett.\r\n\r\n* Area: FabrikamCloud\r\n* Iteration: FabrikamCloud\\Release 1\\Sprint 1\r\n* State: New\r\n"
                },
                Resource = new WorkItemDeletedResource
                {
                    Id = 5,
                    RevisionNumber = 1,
                    Fields = new WorkItemFields
                    {
                        SystemAreaPath = "FabrikamCloud",
                        SystemTeamProject = "FabrikamCloud",
                        SystemIterationPath = "FabrikamCloud\\Release 1\\Sprint 1",
                        SystemWorkItemType = "Bug",
                        SystemState = "New",
                        SystemReason = "New defect reported",
                        SystemCreatedDate = "2014-07-15T17:42:44.663Z".ToDateTime(),
                        SystemCreatedBy = "Jamal Hartnett",
                        SystemChangedDate = "2014-07-15T17:42:44.663Z".ToDateTime(),
                        SystemChangedBy = "Jamal Hartnett",
                        SystemTitle = "Some great new idea!",
                        MicrosoftVSTSCommonSeverity = "3 - Medium",
                        KanbanColumn = "New"
                    },
                    Links = new WorkItemLinks
                    {
                        Self = new WorkItemLink { Href = "http://fabrikam-fiber-inc.visualstudio.com/DefaultCollection/_apis/wit/recyclebin/5" },
                        WorkItemType = new WorkItemLink { Href = "http://fabrikam-fiber-inc.visualstudio.com/DefaultCollection/_apis/wit/ea830882-2a3c-4095-a53f-972f9a376f6e/workItemTypes/Bug" },
                        Fields = new WorkItemLink { Href = "http://fabrikam-fiber-inc.visualstudio.com/DefaultCollection/_apis/wit/fields" }
                    },
                    Url = "http://fabrikam-fiber-inc.visualstudio.com/DefaultCollection/_apis/wit/recyclebin/5"
                },
                ResourceVersion = "1.0",
                ResourceContainers = new TfsEventResourceContainer
                {
                    Collection = new TfsEventContainerProperty { Id = "c12d0eb8-e382-443b-9f9c-c52cba5014c2" },
                    Account = new TfsEventContainerProperty { Id = "f844ec47-a9db-4511-8281-8b63f4eaf94e" },
                    Project = new TfsEventContainerProperty { Id = "be9b3917-87e6-42a4-a549-2bc06a7a878f" }
                },
                CreatedDate = "2016-05-02T19:17:28.3644564Z".ToDateTime()
            };

            // Act
            var actual = data.ToObject<WorkItemDeletedEvent>();

            // Assert
            string expectedJson = JsonConvert.SerializeObject(expected);
            string actualJson = JsonConvert.SerializeObject(actual);
            Assert.Equal(expectedJson, actualJson);
        }
    }
}
