using System;
using System.Net.Http;
using Blazored.LocalStorage;
using FormsApp.Client;
using FormsApp.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//builder.Services.AddScoped<CustomAuthenticationMessageHandler>();
builder.Services.AddHttpClient("FormsAppServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    //.AddHttpMessageHandler<CustomAuthenticationMessageHandler>();
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
//builder.Services.AddBaseAddressHttpClient();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
    .CreateClient("FormsAppServerAPI"));
//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
//builder.Services.AddApiAuthorization(opt =>
//{
//    opt.ProviderOptions.ConfigurationEndpoint = "oidc.json";
//    //opt.AuthenticationPaths.LogInPath = $"{builder.HostEnvironment.BaseAddress}Account/login";
//});
builder.Services.AddOidcAuthentication(options =>
{
    options.ProviderOptions.Authority = builder.HostEnvironment.BaseAddress;
    options.ProviderOptions.ClientId = "client_id_mvc";
    options.ProviderOptions.DefaultScopes.Add("openid");
    options.ProviderOptions.DefaultScopes.Add("profile");
    options.ProviderOptions.PostLogoutRedirectUri = builder.HostEnvironment.BaseAddress;
    options.ProviderOptions.ResponseType = "code";
    //builder.Configuration.Bind("Local", options.ProviderOptions);
});
//builder.Services.AddApiAuthorization(opt =>
//{
//    opt.ProviderOptions.ConfigurationEndpoint = $"{builder.HostEnvironment.BaseAddress}/.well-known/openid-configuration";
//});
builder.Services.AddBlazoredLocalStorage();
//builder.Services.AddAuthorizationCore();
//builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
//builder.Services.AddScoped<IAuthService, AuthService>()
await builder.Build().RunAsync();


public class CustomAuthenticationMessageHandler : AuthorizationMessageHandler
{
    public CustomAuthenticationMessageHandler(IAccessTokenProvider provider, NavigationManager navigation) : base(provider, navigation)
    {
        ConfigureHandler(new string[] { navigation.BaseUri });
    }
}