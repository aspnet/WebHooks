// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.WebHooks.WebHooks
{
    public class WildcardWebHookFilterProviderTests
    {
        private readonly WildcardWebHookFilterProvider _provider;

        public WildcardWebHookFilterProviderTests()
        {
            _provider = new WildcardWebHookFilterProvider();
        }

        [Fact]
        public async Task GetFiltersAsync_ReturnsWildcardFilter()
        {
            // Act
            Collection<WebHookFilter> actual = await _provider.GetFiltersAsync();

            // Assert
            Assert.Single(actual);
            Assert.Equal(WildcardWebHookFilterProvider.Name, actual.Single().Name);
        }
    }
}
