using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OfiGest.Context;
using OfiGest.Helpers;
using OfiGest.Managers;
using OfiGest.Manegers;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Configuración de cultura institucional
var supportedCultures = new[] { new CultureInfo("es-DO") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("es-DO");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.SuppressXFrameOptionsHeader = false;
});

// Servicios MVC y EF Core con localización de validaciones
builder.Services.AddControllersWithViews()
    .AddDataAnnotationsLocalization();

// Configuración de Base de Datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer($@"Server={Environment.GetEnvironmentVariable("server")};Database={Environment.GetEnvironmentVariable("database")};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;");
});

// Registro de servicios del sistema
builder.Services.AddScoped<DepartamentosManenger>();
builder.Services.AddScoped<OficioManager>();
builder.Services.AddScoped<RolManeger>();
builder.Services.AddScoped<TipoOficioManenger>();
builder.Services.AddScoped<UsuarioManager>();
builder.Services.AddScoped<CorreoManager>();
builder.Services.AddScoped<LoginUserManeger>();
builder.Services.AddScoped<DivisionesManager>();
builder.Services.AddScoped<EstadisticaManager>();
builder.Services.AddScoped<LogOficioHelper>();
builder.Services.AddScoped<NotificacionManager>();
builder.Services.AddScoped<PdfOficioManager>();

// Configuración de Data Protection para cookies únicas por sesión
builder.Services.AddDataProtection();

// ⭐⭐ CONFIGURACIÓN CORREGIDA DE AUTENTICACIÓN
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login/AccesoDenegado";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(15); // 1 minuto de inactividad
        options.SlidingExpiration = true; // Renovación con cada actividad

        // ⭐⭐ CORRECCIÓN: Configuración de cookie mejorada
        options.Cookie = new CookieBuilder
        {
            Name = "OfiGest.Auth",
            SameSite = SameSiteMode.Lax,
            SecurePolicy = CookieSecurePolicy.None, // Cambiar a Always en producción
            HttpOnly = true,
            IsEssential = true,
            MaxAge = TimeSpan.FromMinutes(15) // ⭐ Coincide con ExpireTimeSpan
        };

        // Configuración para mejor manejo de sesiones múltiples
        options.SessionStore = new MemoryCacheTicketStore();

        options.Events = new CookieAuthenticationEvents()
        {
            OnSignedIn = context =>
            {
                return Task.CompletedTask;
            },
            OnRedirectToLogin = context =>
            {
                // ⭐⭐ CORRECCIÓN: Redirección directa al login
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = 401;
                }
                else
                {
                    context.Response.Redirect("/Login?timeout=true");
                }
                return Task.CompletedTask;
            },
            OnValidatePrincipal = async context =>
            {
                // Validación adicional si es necesaria
                await Task.CompletedTask;
            }
        };
    });

// Autorización basada en claims
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Administrador", policy => policy.RequireClaim("Rol", "Administrador"));
});

// Activar sesión para trazabilidad visual 
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "OfiGest.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(15); // Coincide con auth
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Agregar MemoryCache para el almacenamiento de tickets
builder.Services.AddMemoryCache();

var app = builder.Build();

// Manejo de errores y seguridad en producción
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Oficio/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Aplicar localización
var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ⭐⭐ MIDDLEWARE MEJORADO PARA DETECTAR EXPIRACIÓN
app.Use(async (context, next) =>
{
    // Solo verificar para rutas que requieren autenticación
    var path = context.Request.Path;
    var isPublicPath = path.StartsWithSegments("/Login") ||
                       path.StartsWithSegments("/Error") ||
                       path.StartsWithSegments("/css") ||
                       path.StartsWithSegments("/js") ||
                       path.StartsWithSegments("/images") ||
                       path.StartsWithSegments("/lib");

    if (!isPublicPath && context.User.Identity.IsAuthenticated)
    {
        // Intentar autenticar para verificar si la cookie sigue siendo válida
        var result = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded)
        {
            // Limpiar la sesión y redirigir al login
            context.Response.Redirect("/Login?timeout=true");
            return;
        }
    }

    await next();
});

// Middleware para manejar errores 400
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == 400 && !context.Response.HasStarted)
    {
        context.Response.Redirect("/Login");
    }
});

// Ruta por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();

// Implementación de MemoryCacheTicketStore
public class MemoryCacheTicketStore : ITicketStore
{
    private const string KeyPrefix = "AuthSessionStore-";
    private readonly IMemoryCache _cache;

    public MemoryCacheTicketStore()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var options = new MemoryCacheEntryOptions();
        var expiresUtc = ticket.Properties.ExpiresUtc;

        if (expiresUtc.HasValue)
        {
            options.SetAbsoluteExpiration(expiresUtc.Value);
        }

        options.SetSlidingExpiration(TimeSpan.FromMinutes(15));

        _cache.Set(key, ticket, options);
        return Task.CompletedTask;
    }

    public Task<AuthenticationTicket> RetrieveAsync(string key)
    {
        _cache.TryGetValue(key, out AuthenticationTicket ticket);
        return Task.FromResult(ticket);
    }

    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = KeyPrefix + Guid.NewGuid().ToString();
        var options = new MemoryCacheEntryOptions();
        var expiresUtc = ticket.Properties.ExpiresUtc;

        if (expiresUtc.HasValue)
        {
            options.SetAbsoluteExpiration(expiresUtc.Value);
        }

        options.SetSlidingExpiration(TimeSpan.FromMinutes(15));

        _cache.Set(key, ticket, options);
        return Task.FromResult(key);
    }
}