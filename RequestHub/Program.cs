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
{
    throw new InvalidOperationException("Jwt:Key es requerido.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.IncludeErrorDetails = true;
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
                Console.WriteLine("AUTH HEADER => " + context.Request.Headers.Authorization);
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("JWT ERROR => " + context.Exception);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("JWT OK");
                return Task.CompletedTask;
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

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("frontend");

app.UseRouting();

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

    var admin = await db.Users.FirstOrDefaultAsync(x =>
        x.Username == "a.morelp" || x.Email == "a.morelp@aduanas.gob.do");

    if (admin == null)
    {
        admin = new User
        {
            Username = "a.morelp",
            Email = "a.morelp@aduanas.gob.do",
            FullName = "Angel Roberto Morel Peña",
            Role = UserRole.Admin.ToString(),
            AreaId = null,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Angel1234")
        };

        db.Users.Add(admin);
    }
    else
    {
        admin.Username = "a.morelp";
        admin.Email = "a.morelp@aduanas.gob.do";
        admin.FullName = "Angel Roberto Morel Peña";
        admin.Role = UserRole.Admin.ToString();
        admin.AreaId = null;
        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Angel1234");
    }

    await db.SaveChangesAsync();
}

app.MapControllers();

app.Run();