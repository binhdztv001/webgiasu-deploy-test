using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Globalization;
using System.Text;
using Webgiasu.Data;
using Webgiasu.Models;
using Webgiasu.Services;
using Microsoft.AspNetCore.Mvc;

// Set UTF-8 encoding
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.OutputEncoding = Encoding.UTF8;



var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtKey = builder.Configuration["Jwt:Key"] ?? "please-change-this-default-jwt-key-2026";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Webgiasu";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "WebgiasuUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("auth_token", out var token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "XSRF-TOKEN";
    options.Cookie.HttpOnly = false;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddScoped<GeminiService>();
builder.Services.AddScoped<SuiService>();

// ✅ Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gia Sư Online API",
        Version = "v1",
        Description = "API cho hệ thống Gia Sư Online - Hỗ trợ quản lý bài tập, gia sư, học sinh và AI",
        Contact = new OpenApiContact
        {
            Name = "Support Team",
            Email = "support@giasuonline.com"
        }
    });

    // ✅ Thêm XML comments (optional - để hiển thị mô tả chi tiết)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Configure Request Localization for Vietnamese
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("vi-VN"),
        new CultureInfo("en-US")
    };

    options.DefaultRequestCulture = new RequestCulture("vi-VN");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register EF-backed services (replacing mocks)
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProblemService, ProblemService>();
builder.Services.AddScoped<ISolutionService, SolutionService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<ICommunityService, CommunityService>();
builder.Services.AddScoped<IPremiumService, PremiumService>();
builder.Services.AddScoped<IProblemGroupService, ProblemGroupService>();
builder.Services.AddScoped<ISchoolClassService, SchoolClassService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITutorApplicationService, TutorApplicationService>();
builder.Services.AddScoped<IStatisticsExportService, StatisticsExportService>();

// Add SignalR
builder.Services.AddSignalR();

// SePay configuration
builder.Services.Configure<SePayOptions>(builder.Configuration.GetSection("SePay"));
builder.Services.AddSingleton<ISePayGateway, SePayGateway>();

builder.Services.AddHttpClient<ISerpApiService, SerpApiService>(client =>
{
    client.BaseAddress = new Uri("https://serpapi.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        // chạy migrations trước (nếu bạn muốn tự động cập nhật db tại startup)
        // db.Database.Migrate();
        SeedData.EnsureSeedData(db);
        Console.WriteLine("✅ Seed data đã được chèn (nếu chưa có)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Lỗi khi seed data: {ex.Message}");
    }
}

// Use Request Localization
app.UseRequestLocalization();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // ✅ Enable Swagger in Development mode
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gia Sư Online API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Gia Sư Online API Documentation";

        options.DefaultModelsExpandDepth(2);
        options.DefaultModelExpandDepth(2);
        options.DisplayRequestDuration();
        options.EnableDeepLinking();
        options.EnableFilter();
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<Webgiasu.Hubs.ChatHub>("/chatHub");
app.MapHub<Webgiasu.Hubs.CommunityHub>("/communityHub");

app.MapGet("/api/documentsearch", async (ISerpApiService serp, string q) =>
{
    if (string.IsNullOrWhiteSpace(q))
        return Results.BadRequest(new { success = false, message = "Query is required" });

    var results = await serp.SearchDocumentsAsync(q, "pdf");
    return Results.Ok(new { success = true, results });
});

app.MapGet("/antiforgery/token", (HttpContext context, IAntiforgery antiforgery) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    return Results.Ok(new { token = tokens.RequestToken });
});

app.Run();