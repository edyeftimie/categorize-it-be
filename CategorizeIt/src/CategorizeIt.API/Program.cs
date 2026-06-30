using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using CategorizeIt.Application.Interfaces;
using CategorizeIt.Application.Services;
using CategorizeIt.Application.Settings;
using CategorizeIt.Infrastructure.Data;
using CategorizeIt.Infrastructure.Repositories;
using CategorizeIt.Infrastructure.ExternalClients;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<GoogleAuthenticationSettings>(builder.Configuration.GetSection("GoogleAuth"));
builder.Services.Configure<EnableBankingSettings>(builder.Configuration.GetSection("EnableBanking"));
builder.Services.AddHttpClient<IEnableBankingClient, EnableBankingClient>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGoogleAuthenticationProvider, GoogleAuthenticationProvider>();
builder.Services.AddScoped<IBankConnectionRepository, BankConnectionRepository>();
builder.Services.AddScoped<IBankAccountRepository, BankAccountRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IBudgetRepository, BudgetRepository>();
builder.Services.AddScoped<IRecommendationRepository, RecommendationRepository>();

builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ITransactionSyncService, TransactionSyncService>();
builder.Services.AddScoped<IMccCategoriser, MccCategoriser>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBankConnectionService, BankConnectionService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CategorizeIt.API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Apply pending EF Core migrations on startup — creates the schema on a fresh
// (e.g. cloud) database automatically, so no manual migration step is needed.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Swagger enabled in all environments so the deployed API can be tested in the browser.
// (If you'd rather keep it local-only, wrap these in: if (app.Environment.IsDevelopment()) { ... })
app.UseSwagger(options =>
{
    options.SerializeAsV2 = false;
});
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CategorizeIt.API v1");
});

// No UseHttpsRedirection: Render (and most container hosts) terminate TLS at their
// proxy and forward plain HTTP to the container. Forcing an HTTPS redirect here would
// break those proxied requests. The public Render URL is still HTTPS.

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();