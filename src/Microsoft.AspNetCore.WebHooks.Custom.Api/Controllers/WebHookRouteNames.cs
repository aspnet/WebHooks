using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides a set of common route names used by the custom WebHooks Web API controllers.
    /// </summary>
    internal static class WebHookRouteNames
    {
        /// <summary>
        /// Provides the name of the <see cref="Controllers.WebHookFiltersController"/> GET action.
        /// </summary>
        public const string FiltersGetAction = "FiltersGetAction";

        /// <summary>
        /// Provides the name of the <see cref="Controllers.WebHookRegistrationsController"/> lookup action.
        /// </summary>
        public const string RegistrationLookupAction = "RegistrationLookupAction";
    }
}
