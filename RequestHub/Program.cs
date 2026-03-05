using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RequestHub.Application.Services;
using RequestHub.Infrastructure.Auth;
using RequestHub.Infrastructure.Persistence;

static void TrySet<T>(object target, string propName, T value)
{
    var p = target.GetType().GetProperty(propName);
    if (p == null || !p.CanWrite) return;

    var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
    object v = value;

    if (value != null && t != typeof(T))
    {
        try { v = Convert.ChangeType(value, t); }
        catch { return; }
    }

    p.SetValue(target, v);
}

static void TrySetEnum(object target, string propName, string enumName)
{
    var p = target.GetType().GetProperty(propName);
    if (p == null || !p.CanWrite) return;

    var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
    if (!t.IsEnum) return;

    try
    {
        var v = Enum.Parse(t, enumName, true);
        p.SetValue(target, v);
    }
    catch { }
}

static string HashPasswordWithFallback(object user, string password)
{
    var bcryptType =
        Type.GetType("BCrypt.Net.BCrypt, BCrypt.Net-Next") ??
        Type.GetType("BCrypt.Net.BCrypt, BCrypt.Net");

    if (bcryptType != null)
    {
        var m = bcryptType.GetMethod("HashPassword", new[] { typeof(string) });
        if (m != null)
        {
            var r = m.Invoke(null, new object[] { password });
            if (r is string s && !string.IsNullOrWhiteSpace(s)) return s;
        }
    }

    var hasherType = typeof(PasswordHasher<>).MakeGenericType(user.GetType());
    var hasher = Activator.CreateInstance(hasherType);
    var hashMethod = hasherType.GetMethod("HashPassword", new[] { user.GetType(), typeof(string) });
    var hash = hashMethod?.Invoke(hasher, new object[] { user, password }) as string;
    return hash ?? "";
}

static async Task EnsureAdminAsync(AppDbContext db)
{
    var usersProp = db.GetType().GetProperty("Users");
    if (usersProp == null) return;

    var setObj = usersProp.GetValue(db);
    if (setObj == null) return;

    var setType = setObj.GetType();
    var entityType = setType.GenericTypeArguments.Length == 1 ? setType.GenericTypeArguments[0] : null;
    if (entityType == null) return;

    var usernameProp =
        entityType.GetProperty("Username") ??
        entityType.GetProperty("UserName") ??
        entityType.GetProperty("Email");

    if (usernameProp == null) return;

    var username = "admin";

    var queryable = (IQueryable)setObj;
    var param = Expression.Parameter(entityType, "x");
    var member = Expression.Property(param, usernameProp);
    var constant = Expression.Constant(username);
    Expression body = member.Type == typeof(string) ? Expression.Equal(member, constant) : Expression.Constant(false);

    var funcType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));
    var lambda = Expression.Lambda(funcType, body, param);

    var anyMethod = typeof(Queryable).GetMethods()
        .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
        .MakeGenericMethod(entityType);

    var exists = (bool)anyMethod.Invoke(null, new object[] { queryable, lambda })!;
    if (exists) return;

    var user = Activator.CreateInstance(entityType);
    if (user == null) return;

    TrySet(user, "Username", username);
    TrySet(user, "UserName", username);
    TrySet(user, "Email", "admin@requesthub.local");
    TrySet(user, "FullName", "Administrador");
    TrySet(user, "Name", "Administrador");
    TrySet(user, "Activo", true);
    TrySet(user, "IsActive", true);

    TrySetEnum(user, "Role", "Admin");
    TrySetEnum(user, "Rol", "Admin");
    TrySet(user, "RoleId", 3);
    TrySet(user, "IdRol", 3);

    var pass = "Admin1234";
    var hash = HashPasswordWithFallback(user, pass);

    TrySet(user, "PasswordHash", hash);
    TrySet(user, "ContrasenaHash", hash);
    TrySet(user, "HashPassword", hash);
    TrySet(user, "Password", pass);
    TrySet(user, "Contrasena", pass);

    db.Add(user);
    await db.SaveChangesAsync();
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", p => p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await EnsureAdminAsync(db);
}

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();