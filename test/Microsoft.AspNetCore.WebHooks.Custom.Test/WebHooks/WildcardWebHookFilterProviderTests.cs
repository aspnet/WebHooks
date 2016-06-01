using System;
using System.Collections.Generic;
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
            Assert.Equal(1, actual.Count);
            Assert.Equal(WildcardWebHookFilterProvider.Name, actual.Single().Name);
        }
    }
}
