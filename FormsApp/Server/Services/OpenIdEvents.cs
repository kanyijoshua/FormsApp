using System;
using System.Linq;
using System.Threading.Tasks;
using FormsApp.Server.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace FormsApp.Server.Services
{
    public class OpenIdEvents
    {
        private static UserManager<ApplicationUser>? _userManager;
        private static SignInManager<ApplicationUser>? _signInManager;
        private static SurveyContext? _context;
        private static IServiceProvider _serviceProvider;

        public OpenIdEvents(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
            IServiceProvider serviceProvider, SurveyContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public static async Task OnTicketReceived(TicketReceivedContext context)
        {
            //using var _userManager = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            //var _signInManager = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
            //using var surveyContext = _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<SurveyContext>();
            //var UserManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var applicationUsers = _userManager?.Users.ToList();
            var claims = context.Principal?.Claims.ToList();
            var email = claims?.FirstOrDefault(c => c.Type == "Email")?.Value;
            if (!string.IsNullOrEmpty(email))
            {
                
                var emailUser = applicationUsers?.FirstOrDefault(c => c?.Email == email);
                if (emailUser is null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email
                    };

                    var result = await _userManager.CreateAsync(user);

                    if (result.Succeeded)
                    {
                        claims?.ForEach(async cliam =>
                        {
                            await _userManager.AddClaimAsync(user, cliam);
                        });
                        // var signInResult = _signInManager.SignInAsync(user,false);
                        //
                        // if (signInResult.IsCompletedSuccessfully)
                        // {
                        // If they exist, add claims to the user for:
                        //    Given (first) name
                        //    Locale
                        //    Picture
                        // if (info.Principal.HasClaim(c => c.Type == ClaimTypes.GivenName))
                        // {
                        //     await _userManager.AddClaimAsync(user,
                        //         info.Principal.FindFirst(ClaimTypes.GivenName));
                        // }
                        // Include the access token in the properties
                        var props = new AuthenticationProperties();
                        props.StoreTokens(context.Properties?.GetTokens() ?? Array.Empty<AuthenticationToken>());
                        props.IsPersistent = true;

                        await _signInManager.SignInAsync(user, props);
                    }
                }
                else
                {
                    claims?.ForEach(async cliam =>
                    {
                        await _userManager.AddClaimAsync(emailUser, cliam);
                    });
                    // Include the access token in the properties
                    var props = new AuthenticationProperties();
                    props.StoreTokens(context.Properties?.GetTokens() ?? Array.Empty<AuthenticationToken>());
                    props.IsPersistent = true;

                    await _signInManager.SignInAsync(emailUser, props);
                }
            }
            return;
        }
    }
}
