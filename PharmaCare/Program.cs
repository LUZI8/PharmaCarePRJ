var builder = WebApplication.CreateBuilder(args);

// Load local overrides explicitly from the project content root.
var localConfigUpper = Path.Combine(builder.Environment.ContentRootPath, "appsettings.Local.json");
var localConfigLower = Path.Combine(builder.Environment.ContentRootPath, "appsettings.local.json");

if (File.Exists(localConfigUpper))
    builder.Configuration.AddJsonFile(localConfigUpper, optional: false, reloadOnChange: true);

if (File.Exists(localConfigLower) && !string.Equals(localConfigLower, localConfigUpper, StringComparison.Ordinal))
    builder.Configuration.AddJsonFile(localConfigLower, optional: false, reloadOnChange: true);

var environmentApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrWhiteSpace(builder.Configuration["AISettings:ApiKey"]) && !string.IsNullOrWhiteSpace(environmentApiKey))
    builder.Configuration["AISettings:ApiKey"] = environmentApiKey;

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DataDbContext>(x => x.UseSqlServer(connectionString));

builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddSingleton<IFileHelper, FileHelper>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.Configure<AISettings>(builder.Configuration.GetSection("AISettings"));
builder.Services.AddHttpClient<IAIService, OpenAIService>();

builder.Services.AddScoped<IExpiredReservationsService, ExpiredReservationsService>();
builder.Services.AddHostedService<ExpiredReservationsService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var path = (context.Request.Path.Value ?? string.Empty).ToLowerInvariant();
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (path.StartsWith("/account/login") || path.StartsWith("/account/forgotpassword") ||
            path.StartsWith("/account/verifyemail") || path.StartsWith("/account/resendverificationcode"))
        {
            return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                "auth:" + ip,
                _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = 12,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        }

        if (path.StartsWith("/ai/") || path.StartsWith("/adminai/"))
        {
            return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                "ai:" + ip,
                _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        }

        return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            "site:" + ip,
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 240,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DataDbContext>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

    await DemoCatalogSeeder.SeedAsync(db, loggerFactory.CreateLogger("DemoCatalogSeeder"));
    await RealMedicineImageSeeder.SeedAsync(db, loggerFactory.CreateLogger("RealMedicineImageSeeder"));
    await MarketplaceBootstrapper.EnsureAsync(db, loggerFactory.CreateLogger("MarketplaceBootstrapper"));
    await MarketplaceOrderBootstrapper.EnsureAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "SAMEORIGIN";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin-allow-popups";
        return Task.CompletedTask;
    });
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx => ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=604800"
});

app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        Console.WriteLine($"Request Path: {context.Request.Path}");
        await next();
        Console.WriteLine($"Response Status: {context.Response.StatusCode}");
    });
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "PharmaCare",
    mode = "multi-pharmacy-marketplace",
    utc = DateTime.UtcNow
}));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Marketplace}/{action=Index}/{id?}");

app.Run();