using Microsoft.EntityFrameworkCore;
using PCH.Api.Services;
using PCH.Connectors;
using PCH.Core.Interfaces;
using PCH.Data;
using PCH.Notifications;

var builder = WebApplication.CreateBuilder(args);

// Don't leak the Kestrel version in the Server response header.
builder.WebHost.ConfigureKestrel(opts => opts.AddServerHeader = false);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS: only the local Blazor app may call this API.
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? ["https://localhost:7083"];
builder.Services.AddCors(options =>
    options.AddPolicy("BlazorApp", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()));

// EF Core + SQLite. Connection string lives in appsettings.json (never in code).
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=pch.db";
builder.Services.AddDbContext<PchDbContext>(options => options.UseSqlite(connectionString));

// Local LLM (GPT4All OpenAI-compatible API). BaseUrl from appsettings; model from appsettings.
var llmBaseUrl = builder.Configuration["Llm:BaseUrl"] ?? "http://localhost:4891";
builder.Services.AddHttpClient<LlmClassifier>(c => c.BaseAddress = new Uri(llmBaseUrl));

// Email connector — scoped so it shares the per-request PchDbContext.
builder.Services.AddScoped<EmailConnector>();

// RSS connector — transient via typed HttpClient (DI resolves PchDbContext from request scope).
builder.Services.AddHttpClient<RssConnector>();

// Desktop toast notifications — singleton (one AUMID registration for the process lifetime).
builder.Services.AddSingleton<INotificationService, NotificationService>();

// Background service: deadline checks + morning news summary every 5 minutes.
builder.Services.AddHostedService<NotificationCheckService>();

var app = builder.Build();

// Apply any pending migrations on startup so the local SQLite file is ready.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PchDbContext>();
    db.Database.Migrate();
    await DemoDataSeeder.SeedAsync(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("BlazorApp");
app.UseHttpsRedirection();

// Security headers on every response.
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"]        = "DENY";
    ctx.Response.Headers["Referrer-Policy"]        = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Permissions-Policy"]     = "geolocation=(), camera=(), microphone=()";
    await next(ctx);
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("Health")
    .WithOpenApi();

app.MapControllers();

app.Run();
