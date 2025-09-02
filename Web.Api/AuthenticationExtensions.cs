using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Web.Api.Extensions
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddJWTAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var key = jwtSettings["Key"];

            if (string.IsNullOrEmpty(key) || key.Length < 32)
            {
                throw new InvalidOperationException("JWT Key must be at least 32 characters long");
            }

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ClockSkew = TimeSpan.FromMinutes(30),
                    RequireExpirationTime = true,
                    RequireSignedTokens = true
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";

                        var result = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            success = false,
                            message = "Authentication required",
                            statusCode = 401
                        });

                        return context.Response.WriteAsync(result);
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";

                        var result = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            success = false,
                            message = "Access forbidden",
                            statusCode = 403
                        });

                        return context.Response.WriteAsync(result);
                    }
                };
            });

            services.AddAuthorization(options =>
            {
                // E-commerce specific policies
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("Admin"));

                options.AddPolicy("CustomerOnly", policy =>
                    policy.RequireRole("Customer"));

                options.AddPolicy("ManagerOrAdmin", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(ClaimTypes.Role, "Manager") ||
                        context.User.HasClaim(ClaimTypes.Role, "Admin")));

                options.AddPolicy("ProductManagement", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(ClaimTypes.Role, "Admin") ||
                        context.User.HasClaim(ClaimTypes.Role, "ProductManager")));

                options.AddPolicy("OrderManagement", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(ClaimTypes.Role, "Admin") ||
                        context.User.HasClaim(ClaimTypes.Role, "OrderManager")));

                // Resource-based authorization (user can only access their own orders)
                options.AddPolicy("OwnOrdersOnly", policy =>
                    policy.Requirements.Add(new OwnerAuthorizationRequirement()));

                // Your existing policies
                options.AddPolicy("ResourceOwner", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(claim =>
                            claim.Type == ClaimTypes.Role &&
                            claim.Value.Equals("resourceowner", StringComparison.OrdinalIgnoreCase))));

                options.AddPolicy("Client", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(claim =>
                            claim.Type == ClaimTypes.Role &&
                            claim.Value.Equals("client", StringComparison.OrdinalIgnoreCase))));
            });

            // Register authorization handlers
            services.AddScoped<IAuthorizationHandler, OwnerAuthorizationHandler>();

            return services;
        }
    }

   
    public class OwnerAuthorizationRequirement : IAuthorizationRequirement { }

    
    public class OwnerAuthorizationHandler : AuthorizationHandler<OwnerAuthorizationRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OwnerAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OwnerAuthorizationRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                context.Fail();
                return Task.CompletedTask;
            }

       
            var userEmail = context.User.FindFirst(ClaimTypes.Email)?.Value;

           
            var customerEmail = httpContext.Request.Query["customerEmail"].FirstOrDefault() ??
                               httpContext.Request.RouteValues["customerEmail"]?.ToString();

       
            if (context.User.HasClaim(ClaimTypes.Role, "Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

           
            if (!string.IsNullOrEmpty(userEmail) &&
                !string.IsNullOrEmpty(customerEmail) &&
                userEmail.Equals(customerEmail, StringComparison.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}

// Updated appsettings.json configuration:
/*
{
  "JwtSettings": {
    "Key": "your-super-secret-key-that-is-at-least-32-characters-long-for-production-security",
    "Issuer": "StackBuildECommerceAPI",
    "Audience": "StackBuildClients",
    "ExpiryMinutes": 60
  }
}
*/

// Usage in Program.cs:
/*
using Web.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddHttpContextAccessor(); // Required for authorization handler
builder.Services.AddJWTAuthentication(builder.Configuration);

var app = builder.Build();

// Configure middleware pipeline
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
*/