using Microsoft.NetCore.TestUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.WebHooks.WebHooks
{
    public class WebHookFilterTests
    {
        private WebHookFilter _filter;

        public WebHookFilterTests()
        {
            _filter = new WebHookFilter();
        }

        [Fact]
        public void Name_Roundtrips()
        {
            PropertyAssert.Roundtrips(_filter, f => f.Name, PropertySetter.NullRoundtrips, roundtripValue: "你好世界");
        }

        [Fact]
        public void Description_Roundtrips()
        {
            PropertyAssert.Roundtrips(_filter, f => f.Description, PropertySetter.NullRoundtrips, roundtripValue: "你好世界");
        }
    }

}
