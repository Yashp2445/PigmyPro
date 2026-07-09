using PigmyPro.Data.Context;
using PigmyPro.Data.Interfaces;
using PigmyPro.Data.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
                 .WriteTo.File("diagnostic_log.txt", rollingInterval: RollingInterval.Day));

builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var key = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is missing from config");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey("PigmyPro.Token"))
            {
                context.Token = context.Request.Cookies["PigmyPro.Token"];
            }
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            // Do not redirect for API calls if any exist, but for MVC always redirect to login
            context.HandleResponse();
            context.Response.Redirect("/Auth/Login");
            return Task.CompletedTask;
        }
    };
});


builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});


// Known dev environment behavior: Opening the project in a second browser tab while a
// previous tab is open can cause login issues due to cookie conflicts. This is standard
// browser cookie behavior and is NOT a code bug. Workaround: use a private/incognito
// window for each separate session, or clear cookies between runs.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "PigmyPro.Session";
});


builder.Services.AddSingleton<DapperContext>();


builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBankRepository, BankRepository>();
builder.Services.AddScoped<IBranchRepository, BranchRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAgentRepository, AgentRepository>();
builder.Services.AddScoped<IPigmyStatementRepository, PigmyStatementRepository>();
builder.Services.AddScoped<IMobileImportRepository, MobileImportRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IMapRepository, MapRepository>();
builder.Services.AddScoped<IDropdownService, DropdownService>();
builder.Services.AddScoped<PigmyPro.Web.Services.IReportExportService, PigmyPro.Web.Services.ReportExportService>();

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSerilogRequestLogging();

// Prevent caching for dynamic requests (fixes back-button after logout and weird bfcache layouts)
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "-1";
    await next();
});

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Restore session from JWT claims if session was cleared/lost (e.g. browser restart or PC shutdown)
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        if (!context.Session.GetInt32("BankID").HasValue)
        {
            var bankIdClaim = context.User.FindFirst("BankID")?.Value;
            if (int.TryParse(bankIdClaim, out int bankId))
            {
                context.Session.SetInt32("BankID", bankId);
            }
        }
        if (!context.Session.GetInt32("BranchID").HasValue)
        {
            var branchIdClaim = context.User.FindFirst("BranchID")?.Value;
            if (int.TryParse(branchIdClaim, out int branchId))
            {
                context.Session.SetInt32("BranchID", branchId);
            }
        }
        if (string.IsNullOrEmpty(context.Session.GetString("UserRole")))
        {
            var roleClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(roleClaim))
            {
                context.Session.SetString("UserRole", roleClaim);
            }
        }
    }
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();