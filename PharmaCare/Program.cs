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

var app = builder.Build();

// Development-only catalog setup.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DataDbContext>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

    await DemoCatalogSeeder.SeedAsync(db, loggerFactory.CreateLogger("DemoCatalogSeeder"));
    await RealMedicineImageSeeder.SeedAsync(db, loggerFactory.CreateLogger("RealMedicineImageSeeder"));
}

// Correct environment-specific error handling: detailed errors only in development.
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

// Baseline browser hardening without blocking the existing external CDN/image integrations.
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
    OnPrepareResponse = ctx =>
    {
        // Cache versioned/static assets in the browser while Razor/JSON responses stay dynamic.
        ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=604800";
    }
});

app.UseRouting();
app.UseSession();
app.UseAuthorization();

// Lightweight request tracing in development only.
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
    utc = DateTime.UtcNow
}));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=FrontEnd}/{action=Index}/{id?}");

app.Run();