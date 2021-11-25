using FormsApp.Server.Models;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FormsApp.Server.Controllers
{
    //[ApiController]
    [Route("[controller]")]
    [RequestSizeLimit(1074790400)]
    public class AccountController : Controller
    {
        private readonly IEventService _events;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly ILogger<AccountController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IIdentityServerInteractionService _interactionService;
        public AccountController(IEventService events, IIdentityServerInteractionService interaction,
            IAuthenticationSchemeProvider schemeProvider, IClientStore clientStore, SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager, IIdentityServerInteractionService interactionService, ILogger<AccountController> logger)
        {
            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;
            _signInManager = signInManager;
            _userManager = userManager;
            _interactionService = interactionService;
            _logger = logger;
        }
        [HttpGet("login")]
        //[Route("Account/login")]
        public IActionResult Get(string? returnUrl)
        {
            //var callbackUrl = Url.Action("ExternalLoginCallback");

            var props = new AuthenticationProperties
            {
                RedirectUri = returnUrl/*callbackUrl*/,
                Items =
                {
                    { "scheme", OpenIdConnectDefaults.AuthenticationScheme },
                    { "returnUrl", returnUrl }
                }
            };
            return Challenge(props, OpenIdConnectDefaults.AuthenticationScheme);
            //return Json("sd");
        }
        //[Authorize(AuthenticationSchemes = (OpenIdConnectDefaults.AuthenticationScheme))]
        //[HttpGet("Logout")]
        //public async Task<IActionResult> Signout(string logoutId)
        //{
        //    //var vm = await _account.BuildLoggedOutViewModelAsync(model.LogoutId);

        //    var user = HttpContext.User;
        //    if (user?.Identity.IsAuthenticated == true)
        //    {
        //        // delete local authentication cookie
        //        await HttpContext.SignOutAsync();

        //        // raise the logout event
        //        await _events.RaiseAsync(new UserLogoutSuccessEvent(user.GetSubjectId(), user.GetName()));
        //    }

        //    // check if we need to trigger sign-out at an upstream identity provider
        //    //if (vm.TriggerExternalSignout)
        //    //{
        //    //    // build a return URL so the upstream provider will redirect back
        //    //    // to us after the user has logged out. this allows us to then
        //    //    // complete our single sign-out processing.
        //    //    string url = Url.Action("Logout", new { logoutId = vm.LogoutId });

        //    //    // this triggers a redirect to the external provider for sign-out
        //    //    return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
        //    //}

        //    //return RedirectToAction(nameof(SignIn));
        //    await HttpContext.SignOutAsync(IdentityServerConstants.DefaultCookieAuthenticationScheme);
        //    await HttpContext.SignOutAsync();
        //    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/" });
        //    await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new OpenIdConnectChallengeProperties { RedirectUri = "/" });
        //    var authSignOut = new AuthenticationProperties
        //    {
        //        RedirectUri = "/"
        //    };
        //    //return RedirectToAction(nameof(Index));
        //    return SignOut(authSignOut, CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
        //}
        [HttpGet("Logout")]
        public async Task<IActionResult> Logout(string logoutId)
        {
            // build a model so the logout page knows what to display
            var vm = await BuildLogoutViewModelAsync(logoutId);

            if (vm.ShowLogoutPrompt == false)
            {
                // if the request for logout was properly authenticated from IdentityServer, then
                // we don't need to show the prompt and can just log the user out directly.
                return await Logout(vm);
            }

            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutInputModel model)
        {
            // build a model so the logged out page knows what to display
            var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

            if (User?.Identity?.IsAuthenticated == true)
            {
                // delete local authentication cookie
                await HttpContext.SignOutAsync();

                // raise the logout event
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
                await _signInManager.SignOutAsync();
                await HttpContext.SignOutAsync();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

                var logoutRequest = await _interactionService.GetLogoutContextAsync(model.LogoutId);

                if (string.IsNullOrEmpty(logoutRequest.PostLogoutRedirectUri))
                {
                    return RedirectToAction("Index", "Home");
                }
                return SignOut(new AuthenticationProperties { RedirectUri = logoutRequest.PostLogoutRedirectUri }, OpenIdConnectDefaults.AuthenticationScheme);
                //return Redirect(logoutRequest.PostLogoutRedirectUri);
            }

            // check if we need to trigger sign-out at an upstream identity provider
            if (vm.TriggerExternalSignout)
            {
                // build a return URL so the upstream provider will redirect back
                // to us after the user has logged out. this allows us to then
                // complete our single sign-out processing.
                string url = Url.Action("Logout", new { logoutId = vm.LogoutId })!;

                // this triggers a redirect to the external provider for sign-out
                return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
            }

            return View("LoggedOut", vm);
        }
        //[Authorize(AuthenticationSchemes = (OpenIdConnectDefaults.AuthenticationScheme))]
       
        //public async Task Logout()
        //{
        //    await HttpContext.SignOutAsync();
        //    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        //    await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        //}

        private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null)
            {
                var local = context.IdP == IdentityServerConstants.LocalIdentityProvider;

                // this is meant to short circuit the UI and only trigger the one external IdP
                var vm = new LoginViewModel
                {
                    EnableLocalLogin = local,
                    ReturnUrl = returnUrl,
                    EmailOrPfNo = context.LoginHint,
                };

                if (!local)
                {
                    vm.ExternalProviders = new[] { new ExternalProvider { AuthenticationScheme = context.IdP } };
                }

                return vm;
            }

            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var providers = schemes
                .Where(x => x.DisplayName != null)
                .Select(x => new ExternalProvider
                {
                    DisplayName = x.DisplayName ?? x.Name,
                    AuthenticationScheme = x.Name
                }).ToList();

            var allowLocal = true;
            if (context?.Client.ClientId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
                if (client != null)
                {
                    allowLocal = client.EnableLocalLogin;

                    if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                    {
                        providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                    }
                }
            }

            return new LoginViewModel
            {
                AllowRememberLogin = AccountOptions.AllowRememberLogin,
                EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
                ReturnUrl = returnUrl,
                EmailOrPfNo = context?.LoginHint,
                ExternalProviders = providers.ToArray()
            };
        }

        private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model)
        {
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
            vm.EmailOrPfNo = model.EmailOrPfNo;
            vm.RememberLogin = model.RememberLogin;
            return vm;
        }

        private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
        {
            var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };

            if (User.Identity?.IsAuthenticated != true)
            {
                // if the user is not authenticated, then just show logged out page
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                // it's safe to automatically sign-out
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            return vm;
        }

        private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
        {
            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(logoutId);

            var vm = new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = logoutId
            };

            if (User?.Identity.IsAuthenticated == true)
            {
                var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if (idp != null && idp != IdentityServer4.IdentityServerConstants.LocalIdentityProvider)
                {
                    var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                    if (providerSupportsSignout)
                    {
                        if (vm.LogoutId == null)
                        {
                            // if there's no current logout context, we need to create one
                            // this captures necessary info from the current logged in user
                            // before we signout and redirect away to the external IdP for signout
                            vm.LogoutId = await _interaction.CreateLogoutContextAsync();
                        }

                        vm.ExternalAuthenticationScheme = idp;
                    }
                }
            }

            return vm;
        }
    }

    
}
