using Microsoft.AspNetCore.WebHooks.Controllers;

namespace Microsoft.AspNetCore.WebHooks.Routes
{
    /// <summary>
    /// Provides a set of common route names used by the custom WebHooks Web API controllers.
    /// </summary>
    internal static class WebHookRouteNames
    {
        /// <summary>
        /// Provides the name of the <see cref="WebHookFiltersController"/> GET action.
        /// </summary>
        public const string FiltersGetAction = "FiltersGetAction";

        /// <summary>
        /// Provides the name of the <see cref="WebHookRegistrationsController"/> lookup action.
        /// </summary>
        public const string RegistrationLookupAction = "RegistrationLookupAction";
    }
}
