using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RequestHub.Application.Services;
using RequestHub.Domain.Entities;
using RequestHub.Domain.Enums;
using RequestHub.Infrastructure.Auth;
using RequestHub.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
          ?? throw new InvalidOperationException("La sección Jwt no está configurada.");

if (string.IsNullOrWhiteSpace(jwt.Key))
    throw new InvalidOperationException("Jwt:Key es requerido.");

if (string.IsNullOrWhiteSpace(jwt.Issuer))
    throw new InvalidOperationException("Jwt:Issuer es requerido.");

if (string.IsNullOrWhiteSpace(jwt.Audience))
    throw new InvalidOperationException("Jwt:Audience es requerido.");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection es requerido.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.IncludeErrorDetails = true;
        options.UseSecurityTokenValidators = true;
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "username",
            RoleClaimType = "role"
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var header = context.Request.Headers.Authorization.ToString();
                Console.WriteLine($"JWT HEADER: {header}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("JWT TOKEN VALIDADO OK");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"JWT AUTH FAILED: {context.Exception.GetType().Name} - {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(new
                {
                    message = "No autorizado",
                    error = context.Error,
                    errorDescription = context.ErrorDescription
                });
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RequestHub API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega solo el token JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".pdf"] = "application/pdf";

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");

app.UseStaticFiles();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    ContentTypeProvider = contentTypeProvider,
    ServeUnknownFileTypes = false,
    OnPrepareResponse = context =>
    {
        var extension = Path.GetExtension(context.File.Name);

        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            context.Context.Response.Headers["Content-Type"] = "application/pdf";
            context.Context.Response.Headers["Content-Disposition"] = $"inline; filename=\"{context.File.Name}\"";
            context.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            context.Context.Response.Headers["Pragma"] = "no-cache";
            context.Context.Response.Headers["Expires"] = "0";
        }
    }
});

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();

    var superAdmin = await db.Users.FirstOrDefaultAsync(x =>
        x.Username.ToLower() == "a.morelp" || x.Email.ToLower() == "a.morelp@aduanas.gob.do");

    if (superAdmin is null)
    {
        superAdmin = new User
        {
            Username = "a.morelp",
            Email = "a.morelp@aduanas.gob.do",
            FullName = "Angel Roberto Morel Peña",
            Role = UserRole.SuperAdmin.ToString(),
            AreaId = null,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Angel1234")
        };

        db.Users.Add(superAdmin);
    }
    else
    {
        superAdmin.Username = "a.morelp";
        superAdmin.Email = "a.morelp@aduanas.gob.do";
        superAdmin.FullName = "Angel Roberto Morel Peña";
        superAdmin.Role = UserRole.SuperAdmin.ToString();
        superAdmin.AreaId = null;
        superAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Angel1234");
    }

    await db.SaveChangesAsync();
}

app.MapControllers();

app.Run();