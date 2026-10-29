using System.Text;
using Leasing.Host.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;

namespace Leasing.Host.Authorization;

public static class AuthenticationExtensions
{
    public const string LocalDevScheme = "LocalDev";

    /// <summary>
    /// Entra ID bearer tokens (<c>AzureAd</c> section) everywhere; in Local and Docker also tokens signed with
    /// <c>Authentication:LocalDev:SigningKey</c>. Policies are role based.
    /// </summary>
    public static IServiceCollection AddLeasingAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var authentication = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
        authentication.AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));

        var schemes = new List<string> { JwtBearerDefaults.AuthenticationScheme };
        var localKey = configuration["Authentication:LocalDev:SigningKey"];
        if ((environment.IsEnvironment("Local") || environment.IsEnvironment("Docker")) && !string.IsNullOrEmpty(localKey))
        {
            authentication.AddJwtBearer(LocalDevScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = configuration["Authentication:LocalDev:Issuer"],
                    ValidAudience = configuration["Authentication:LocalDev:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(localKey)),
                };
                options.Events = new JwtBearerEvents { OnMessageReceived = ReadHubToken };
            });
            schemes.Add(LocalDevScheme);
        }

        services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Events ??= new JwtBearerEvents();
            var previous = options.Events.OnMessageReceived;
            options.Events.OnMessageReceived = async context =>
            {
                await previous(context);
                await ReadHubToken(context);
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.DefaultRead, p => p.AddAuthenticationSchemes([.. schemes]).RequireRole(Roles.Reader, Roles.Writer, Roles.Administrator));
            options.AddPolicy(Policies.DefaultWrite, p => p.AddAuthenticationSchemes([.. schemes]).RequireRole(Roles.Writer, Roles.Administrator));
            options.AddPolicy(Policies.Admin, p => p.AddAuthenticationSchemes([.. schemes]).RequireRole(Roles.Administrator));
        });

        return services;
    }

    /// <summary>Browsers cannot set headers on WebSocket requests: the SignalR client sends the token in <c>access_token</c>.</summary>
    private static Task ReadHubToken(MessageReceivedContext context)
    {
        var token = context.Request.Query["access_token"];
        if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments(LeasingHub.Route))
        {
            context.Token = token;
        }

        return Task.CompletedTask;
    }
}
