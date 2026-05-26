using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using BuildWise.Models;
using BuildWise.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys")));

builder.Services.AddDbContext<BuildWiseDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BuildWise"), 
    sqlServerOptionsAction: sqlOptions => 
    {
        sqlOptions.EnableRetryOnFailure();
    }));

builder.Services.AddScoped<BuildWise.DataLayer.PropertyDAL>();
builder.Services.AddScoped<BuildWise.BusinessLayer.PropertyBLL>();
builder.Services.AddScoped<BuildWise.DataLayer.MaterialDAL>();
builder.Services.AddScoped<BuildWise.BusinessLayer.MaterialBLL>();
builder.Services.AddScoped<WorkerProjectSchemaService>();
builder.Services.AddScoped<PropertyPhaseSchemaService>();
builder.Services.AddHostedService<DatabaseWarmupService>();

// Configure Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";
        options.AccessDeniedPath = "/Home/Index";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Firebase Admin is optional. Configure Firebase:ServiceAccountPath, or keep firebase-admin-sdk.json
// in the project root for local development.
string? firebaseKeyPath = builder.Configuration["Firebase:ServiceAccountPath"];
if (string.IsNullOrWhiteSpace(firebaseKeyPath))
{
    string localFirebaseKeyPath = System.IO.Path.Combine(builder.Environment.ContentRootPath, "firebase-admin-sdk.json");
    if (System.IO.File.Exists(localFirebaseKeyPath))
    {
        firebaseKeyPath = localFirebaseKeyPath;
    }
}

if (!string.IsNullOrWhiteSpace(firebaseKeyPath) && FirebaseApp.DefaultInstance == null)
{
    try
    {
        if (!System.IO.Path.IsPathRooted(firebaseKeyPath))
        {
            firebaseKeyPath = System.IO.Path.Combine(builder.Environment.ContentRootPath, firebaseKeyPath);
        }

        if (System.IO.File.Exists(firebaseKeyPath))
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = CredentialFactory
                    .FromFile<ServiceAccountCredential>(firebaseKeyPath)
                    .ToGoogleCredential()
            });
        }
        else
        {
            app.Logger.LogWarning("Firebase service account file was not found at {FirebaseKeyPath}. Firebase login is disabled.", firebaseKeyPath);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Firebase Admin could not be initialized. Firebase login is disabled.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
