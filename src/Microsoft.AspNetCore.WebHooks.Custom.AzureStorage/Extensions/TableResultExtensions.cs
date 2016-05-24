using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    public static class TableResultExtensions
    {

        public static bool IsSuccess(this TableResult result)
        {
            return result.HttpStatusCode >= 200 && result.HttpStatusCode < 300;
        }
    }
}
