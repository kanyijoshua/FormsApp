using System.Collections.Generic;
using System.Linq;
using FormsApp.Server.Models;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;

namespace FormsApp.Server
{
    public static class Configuration
    {
        public static IEnumerable<IdentityResource> GetIdentityResources() =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResource
                {
                    Name = "rc.scope",
                    UserClaims = ApplicationUserProperties()
                }
            };

        private static ICollection<string> ApplicationUserProperties()
        {
            var userProfileClaims = typeof(ApplicationUser).GetProperties().Select(x => x.Name).ToList();
            var appUserClaims = typeof(ApplicationUserClaim).GetProperties().Select(x => x.Name).ToList();
            return userProfileClaims.Concat(appUserClaims).ToList();
        }

        public static IEnumerable<ApiResource> GetApis() =>
            new List<ApiResource>
            {
                new ApiResource("ApiOne"),
                new ApiResource("ApiTwo", new string[] { "rc.api.garndma" }),
            };

        public static IEnumerable<IdentityServer4.Models.Client> GetClients() =>
            new List<IdentityServer4.Models.Client>
            {
                new IdentityServer4.Models.Client
                {
                    ClientId = "client_id_mvc",
                    RequireClientSecret = false,
                    //ClientSecrets = { new Secret("client_secret_mvc".ToSha256()) },
                    AllowedGrantTypes = GrantTypes.Code,
                    RequirePkce = true,
                    RedirectUris = { "https://localhost:7106/authentication/login-callback", "https://localhost:5001/authentication/login-callback" },
                    PostLogoutRedirectUris = { "https://localhost:7106/", "https://localhost:5001" },
                    AllowedScopes =
                    {
                        "ApiOne",
                        "ApiTwo",
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "rc.scope",
                    },
                    //puts all the claims in the id token
                    AlwaysIncludeUserClaimsInIdToken = true,
                    AllowOfflineAccess = true,
                    RequireConsent = false,
                },
            };
    }
}