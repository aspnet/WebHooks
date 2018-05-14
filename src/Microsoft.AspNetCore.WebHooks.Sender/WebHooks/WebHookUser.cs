// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides a default implementation of <see cref="IWebHookUser"/> for getting information about a user.
    /// </summary>
    public class WebHookUser : IWebHookUser
    {
        private const string DefaultClaimsType = ClaimTypes.Name;

        private static string _claimsType = DefaultClaimsType;

        /// <summary>
        /// Gets or sets the claims type which is used to get the user ID from the <see cref="IPrincipal"/>.
        /// The default value is <see cref="ClaimTypes.Name"/> but can be set to any non-null string
        /// representing the user ID.
        /// </summary>
        public static string IdClaimsType
        {
            get
            {
                return _claimsType;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _claimsType = value;
            }
        }

        /// <inheritdoc />
        public Task<string> GetUserIdAsync(IPrincipal user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            string id = null;
            if (user is ClaimsPrincipal principal)
            {
                id = GetClaim(principal, _claimsType);
                if (id == null)
                {
                    id = GetClaim(principal, ClaimTypes.NameIdentifier);
                }
            }

            // Fall back to name property
            if (id == null && user.Identity != null)
            {
                id = user.Identity.Name;
            }

            if (id == null)
            {
                var message = "Could not determine the user ID from the given principal.";
                throw new InvalidOperationException(message);
            }

            return Task.FromResult(id);
        }

        /// <summary>
        /// Looks up a <paramref name="claimsType"/> in the provided <paramref name="principal"/> and returns the value if found or <c>null</c> otherwise.
        /// </summary>
        /// <returns>The value of the claim or <c>null</c> if not found.</returns>
        internal static string GetClaim(ClaimsPrincipal principal, string claimsType)
        {
            var claim = principal?.FindFirst(claimsType);
            return claim?.Value;
        }

        /// <summary>
        /// For testing purposes
        /// </summary>
        public static void Reset()
        {
            _claimsType = DefaultClaimsType;
        }
    }
}
