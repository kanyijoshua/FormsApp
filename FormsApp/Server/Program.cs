using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using FormsApp.Server;
using FormsApp.Server.Models;
using FormsApp.Server.Services;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.

//var migrationsAssembly = Assembly.GetEntryAssembly()?.GetName().FullName;

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<SurveyContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("workloadcontext")));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<SurveyContext>()
                .AddDefaultTokenProviders();
builder.Services.AddTransient<UserManager<ApplicationUser>>();
builder.Services.AddTransient<RoleManager<IdentityRole>>();
//builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddIdentityServer()
                .AddAspNetIdentity<ApplicationUser>()
                .AddInMemoryClients(Configuration.GetClients())
                .AddInMemoryIdentityResources(Configuration.GetIdentityResources())
                .AddInMemoryApiResources(Configuration.GetApis())
                .AddInMemoryApiScopes(new List<ApiScope>())
                // .AddConfigurationStore(options =>
                // {
                //     options.ConfigureDbContext = build => build.UseNpgsql(builder.Configuration.GetConnectionString("workloadcontext")
                //         ,opt => opt.MigrationsAssembly("FormsApp.Server"));
                // })
                // .AddOperationalStore(options =>
                // {
                //     options.ConfigureDbContext = build => build.UseNpgsql(builder.Configuration.GetConnectionString("workloadcontext")
                //         ,opt => opt.MigrationsAssembly("FormsApp.Server"));
                // })
                .AddDeveloperSigningCredential();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    //options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddCookie(setup => setup.ExpireTimeSpan = TimeSpan.FromHours(10))
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme,config =>
{
    config.Authority = builder.Environment.IsDevelopment() ? builder.Configuration["openIdAuthority:live"] : builder.Configuration["openIdAuthority:live"];
    config.ClientId = builder.Configuration["Client:ClientId"];
    config.ClientSecret = builder.Configuration["Client:ClientSecret"];
    config.SaveTokens = true;
    //config.CorrelationCookie.SameSite = SameSiteMode.Lax;
    //config.NonceCookie.SameSite = SameSiteMode.Lax;
    config.RequireHttpsMetadata = false;
    config.ResponseType = "code";
    config.SignedOutRedirectUri = "/Home";
    config.RemoteSignOutPath = "/Home/Index";
    config.SignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
    config.SignedOutCallbackPath = "/signout-callback-oid";
    config.GetClaimsFromUserInfoEndpoint = true;
    config.Scope.Add("openid");
    config.Scope.Add("profile");
    //config.Scope.Add("offline_access");
    config.ClaimActions.MapJsonKey("Role", "Role");
    config.UsePkce = true;
    config.Events = new OpenIdConnectEvents
    {
        /*OpenIdEvents.OnTicketReceived(scope)*/
        OnTicketReceived = async (context) =>
        {
            var scope = builder.Services.BuildServiceProvider();
            var _userManager = scope?.GetRequiredService<UserManager<ApplicationUser>>();
            var _signInManager = scope?.GetService<SignInManager<ApplicationUser>>();
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

                    var result = await _userManager?.CreateAsync(user)!;

                    if (result.Succeeded)
                    {
                        foreach(var claim in claims!)
                        {
                            await _userManager.AddClaimAsync(user, claim);
                        };
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

                        await _signInManager?.SignInAsync(user, props)!;
                    }
                }
                else
                {
                    foreach (var claim in claims!)
                    {
                        await _userManager?.AddClaimAsync(emailUser, claim)!;
                    };
                    // Include the access token in the properties
                    var props = new AuthenticationProperties();
                    props.StoreTokens(context.Properties?.GetTokens() ?? Array.Empty<AuthenticationToken>());
                    props.IsPersistent = true;

                    await _signInManager?.SignInAsync(emailUser, props)!;
                }
            }
            return;
        }

    };
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme,o =>
{
    o.Authority = builder.Configuration["localJwtAuthority:local"];
    o.RequireHttpsMetadata = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = false,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});
//builder.Services.AddLocalApiAuthentication();
builder.Services.Configure<KestrelServerOptions>(opt =>
{
    opt.Limits.MaxRequestBodySize = 1074790400;
    opt.Limits.MaxRequestHeaderCount = 7000;
    opt.Limits.MaxRequestHeadersTotalSize = 1074790400;
    opt.Limits.MaxRequestBufferSize = 1074790400;
    opt.Limits.MaxRequestLineSize = 1074790400;
    //opt.ConfigureEndpointDefaults(endpoint =>
    //{
    //    endpoint.
    //});
} );
//builder.Configuration.GetSection("Kestrel")
var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseAuthentication();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();
app.UseIdentityServer();
app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");
//app.MapDefaultControllerRoute();

app.Run();
