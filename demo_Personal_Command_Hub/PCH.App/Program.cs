using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using PCH.App.Components;
using PCH.App.Services;
using PCH.Core.Security;

// Utility mode: print a password hash for user-secrets, then exit.
//   dotnet run --project PCH.App -- hash-password "your-password"
if (args.Length >= 2 && args[0] == "hash-password")
{
    Console.WriteLine(PasswordHasher.Hash(args[1]));
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Don't leak the Kestrel version in the Server response header.
builder.WebHost.ConfigureKestrel(opts => opts.AddServerHeader = false);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Cookie authentication for the single admin user. Credentials come from
// configuration (Auth:Username / Auth:PasswordHash); set the hash via user-secrets.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "PCH.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Rate-limit /login: max 5 attempts per IP per 2-minute fixed window → 429 on rejection.
builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("login", policy =>
    {
        policy.PermitLimit          = 5;
        policy.Window               = TimeSpan.FromMinutes(2);
        policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        policy.QueueLimit           = 0;
    });
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Typed HTTP clients for the PCH API.
// Base URL comes from config (ApiBaseUrl) so HTTP vs HTTPS is not hardcoded.
// Server-side Blazor calls the API directly on the same machine — no TLS needed for localhost.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5292/";
builder.Services.AddHttpClient<TaskApiClient>(c =>  c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<EmailApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddHttpClient<NewsApiClient>(c =>  c.BaseAddress = new Uri(apiBaseUrl));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Security headers on every response.
// CSP allows Blazor Server's inline scripts/styles and SignalR WebSocket.
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"]  = "nosniff";
    ctx.Response.Headers["X-Frame-Options"]         = "DENY";
    ctx.Response.Headers["Referrer-Policy"]         = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Permissions-Policy"]      = "geolocation=(), camera=(), microphone=()";
    ctx.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: blob:; " +
        "connect-src 'self' ws: wss:; " +
        "font-src 'self'";
    await next(ctx);
});

app.UseStaticFiles();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Verify credentials and issue the auth cookie.
// Route is /auth/login (not /login) to avoid ambiguity with the Blazor component at @page "/login".
app.MapPost("/auth/login", async (HttpContext http, IConfiguration config) =>
{
    var form = await http.Request.ReadFormAsync();
    var username = form["username"].ToString().Trim();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    var expectedUser = config["Auth:Username"] ?? "admin";
    var storedHash  = config["Auth:PasswordHash"];

    if (!string.Equals(username, expectedUser, StringComparison.OrdinalIgnoreCase)
        || !PasswordHasher.Verify(password, storedHash))
    {
        return Results.Redirect($"/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    var claims   = new[] { new Claim(ClaimTypes.Name, username) };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity));

    var dest = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
    return Results.Redirect(dest);
}).DisableAntiforgery()
  .RequireRateLimiting("login");

// Sign the admin out and return to the login page.
app.MapPost("/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
