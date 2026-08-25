var builder = WebApplication.CreateBuilder(args);

// Load an untracked local overrides file last, so it wins over appsettings.json.
// This is where local secrets such as SMTP and AI API keys live; it is git-ignored and never committed.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DataDbContext>(x => x.UseSqlServer(connectionString));

// Repository registrations
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddSingleton<IFileHelper, FileHelper>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();

// Email: bind SMTP settings and register the transactional email sender.
// With no SMTP host configured, EmailService logs codes instead of sending (dev fallback).
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();

// AI: keep the provider secret in appsettings.Local.json or user-secrets, never in source control.
builder.Services.Configure<AISettings>(builder.Configuration.GetSection("AISettings"));
builder.Services.AddHttpClient<IAIService, OpenAIService>();

// Background service for expired reservations cleanup
builder.Services.AddScoped<IExpiredReservationsService, ExpiredReservationsService>();
builder.Services.AddHostedService<ExpiredReservationsService>();

// Session configuration with 30-minute timeout
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Development vs Production error handling
if (!app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// Request/Response logging middleware
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request Path: {context.Request.Path}");
    await next();
    Console.WriteLine($"Response Status: {context.Response.StatusCode}");
});

// Default route points to FrontEnd controller
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=FrontEnd}/{action=Index}/{id?}");

app.Run();