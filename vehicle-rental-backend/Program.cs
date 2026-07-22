using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using VehicleRentalApi.Data;
using VehicleRentalApi.Repositories;
using VehicleRentalApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- Config ----
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;
var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"]!;

// ---- Services ----
builder.Services.AddControllers().AddJsonOptions(opts =>
{
    // camelCase is the ASP.NET Core default for System.Text.Json — kept explicit
    // so this always matches the frontend's mock JSON shapes (vehicleId, pricePerDay, etc.)
    opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(
    builder.Configuration.GetConnectionString("Default")!));

builder.Services.AddScoped<VehicleRepository>();
builder.Services.AddScoped<LocationRepository>();
builder.Services.AddScoped<BookingRepository>();
builder.Services.AddScoped<ReviewRepository>();
builder.Services.AddScoped<NotificationRepository>();
builder.Services.AddScoped<CustomerRepository>();
builder.Services.AddScoped<AiRepository>();
builder.Services.AddScoped<PaymentRepository>();
builder.Services.AddScoped<VehicleLocationRepository>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddHostedService<VehicleRentalApi.Services.VehicleTrackingSimulator>();

builder.Services.AddCors(opts =>
{
    opts.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "NightDrive API", Version = "v1" });

    var jwtScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste the JWT from /api/auth/login here (no 'Bearer ' prefix needed).",
    };
    opts.AddSecurityDefinition("Bearer", jwtScheme);
    opts.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
          Array.Empty<string>() }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // serves at /swagger — this is what was missing before
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await VehicleRentalApi.Seed.DevSeeder.RunAsync(dbFactory, logger);
}

app.Run();
