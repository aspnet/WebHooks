﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Events;
using Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNet.WebHooks
{
    public class CodeCheckedInEventTests
    {
        [Fact]
        public void CodeCheckedInEvent_Roundtrips()
        {
            // Arrange
            JObject data = EmbeddedResource.ReadAsJObject("Microsoft.AspNet.WebHooks.Messages.tfvc.checkin.json");
            var expected = new CodeCheckedInEvent
            {
                SubscriptionId = "00000000-0000-0000-0000-000000000000",
                NotificationId = 2,
                Id = "f9b4c23e-88dd-4516-b04d-849787304e32",
                EventType = "tfvc.checkin",
                PublisherId = "tfs",
                Message = new TfsEventMessage
                {
                    Text = "Normal Paulk checked in changeset 18: Dropping in new Java sample",
                    Html = "Normal Paulk checked in changeset <a href=\"https://fabrikam-fiber-inc.visualstudio.com/web/cs.aspx?pcguid=d81542e4-cdfa-4333-b082-1ae2d6c3ad16&amp;cs=18\">18</a>: Dropping in new Java sample",
                    Markdown = "Normal Paulk checked in changeset [18](https://fabrikam-fiber-inc.visualstudio.com/web/cs.aspx?pcguid=d81542e4-cdfa-4333-b082-1ae2d6c3ad16&cs=18): Dropping in new Java sample"
                },
                DetailedMessage = new TfsEventMessage
                {
                    Text = "Normal Paulk checked in changeset 18: Dropping in new Java sample",
                    Html = "Normal Paulk checked in changeset <a href=\"https://fabrikam-fiber-inc.visualstudio.com/web/cs.aspx?pcguid=d81542e4-cdfa-4333-b082-1ae2d6c3ad16&amp;cs=18\">18</a>: Dropping in new Java sample",
                    Markdown = "Normal Paulk checked in changeset [18](https://fabrikam-fiber-inc.visualstudio.com/web/cs.aspx?pcguid=d81542e4-cdfa-4333-b082-1ae2d6c3ad16&cs=18): Dropping in new Java sample"
                },
                Resource = new CodeCheckedInResource
                {
                    ChangesetId = 18,
                    Url = "https://fabrikam-fiber-inc.visualstudio.com/DefaultCollection/_apis/tfvc/changesets/18",
                    Author = new ResourceUser
                    {
                        Id = "d6245f20-2af8-44f4-9451-8107cb2767db",
                        DisplayName = "Normal Paulk",
                        UniqueName = "fabrikamfiber16@hotmail.com"
                    },
                    CheckedInBy = new ResourceUser
                    {
                        Id = "d6245f20-2af8-44f4-9451-8107cb2767db",
                        DisplayName = "Normal Paulk",
                        UniqueName = "fabrikamfiber16@hotmail.com"
                    },
                    CreatedDate = "2014-05-12T22:41:16Z".ToDateTime(),
                    Comment = "Dropping in new Java sample"
                },
                ResourceVersion = "1.0",
                ResourceContainers = new TfsEventResourceContainer
                {
                    Collection = new TfsEventContainerProperty { Id = "c12d0eb8-e382-443b-9f9c-c52cba5014c2" },
                    Account = new TfsEventContainerProperty { Id = "f844ec47-a9db-4511-8281-8b63f4eaf94e" },
                    Project = new TfsEventContainerProperty { Id = "be9b3917-87e6-42a4-a549-2bc06a7a878f" }
                },
                CreatedDate = "2016-05-02T19:01:11.7056821Z".ToDateTime()
            };

            // Act
            var actual = data.ToObject<CodeCheckedInEvent>();

            // Assert
            string expectedJson = JsonConvert.SerializeObject(expected);
            string actualJson = JsonConvert.SerializeObject(actual);
            Assert.Equal(expectedJson, actualJson);
        }
    }
}
