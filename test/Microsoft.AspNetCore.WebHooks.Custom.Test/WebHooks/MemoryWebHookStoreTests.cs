using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.WebHooks.WebHooks
{
    [Collection("StoreCollection")]
    public class MemoryWebHookStoryTests : WebHookStoreTest
    {
        public MemoryWebHookStoryTests()
            : base(new MemoryWebHookStore())
        {
        }
    }
}
