using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using QueryBot.Auth;
using QueryBot.Configuration;
using QueryBot.Data;
using QueryBot.Security;

var builder = WebApplication.CreateBuilder(args);

// ---- Database ----
var connectionString = builder.Configuration.GetConnectionString("MySql")
    ?? throw new InvalidOperationException("ConnectionStrings:MySql is not configured.");

builder.Services.AddDbContext<QueryBotDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ---- Auth ----
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<QueryBotAuthService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
    });

// ---- QueryBot settings ----
builder.Services.Configure<QueryBotSettings>(builder.Configuration.GetSection("QueryBot"));

// ---- HTTP clients ----
builder.Services.AddHttpClient();

// Named client used by DashboardModel to create QBModelDocUpload jobs
builder.Services.AddHttpClient("QuexPlatform", (sp, http) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["QuexPlatform:BaseUrl"]
        ?? throw new InvalidOperationException("QuexPlatform:BaseUrl is not configured.");
    var apiKey = config["QuexPlatform:ApiKey"]
        ?? throw new InvalidOperationException("QuexPlatform:ApiKey is not configured.");
    http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
});

// ---- Razor Pages with default authorization ----
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/Logout");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

// PathBase must be applied BEFORE StaticFiles and Routing.
// In production behind Caddy at /querybot, set PathBase=/querybot via environment variable.
var pathBase = builder.Configuration["PathBase"];
if (!string.IsNullOrWhiteSpace(pathBase))
{
    app.UsePathBase(pathBase);
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", context =>
{
    context.Response.Redirect(context.Request.PathBase + "/index.html");
    return Task.CompletedTask;
});

app.MapRazorPages();

// Serves the hCaptcha site key (and dev mode flag) to the static signup page
app.MapGet("/querybot-config.js", (IConfiguration config, IHostEnvironment env) =>
{
    var siteKey = config["Captcha:SiteKey"] ?? string.Empty;
    var devMode = env.IsDevelopment() ? "true" : "false";
    return Results.Content(
        $"window.QueryBotConfig = {{ captchaSiteKey: \"{siteKey}\", devMode: {devMode} }};",
        "application/javascript");
});

// Proxies signup submissions to QuexPlatform to avoid cross-origin issues in dev
app.MapPost("/signup", async (HttpRequest req, IHttpClientFactory httpClientFactory, IConfiguration config, CancellationToken ct) =>
{
    var baseUrl = config["QuexPlatform:BaseUrl"]
        ?? throw new InvalidOperationException("QuexPlatform:BaseUrl is not configured.");

    using var reader = new StreamReader(req.Body);
    var json = await reader.ReadToEndAsync(ct);

    var client = httpClientFactory.CreateClient();
    using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    var upstream = await client.PostAsync($"{baseUrl}/querybot/signup", content, ct);

    return upstream.IsSuccessStatusCode ? Results.Ok() : Results.StatusCode((int)upstream.StatusCode);
});

app.Run();
