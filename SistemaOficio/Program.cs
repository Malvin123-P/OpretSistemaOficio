using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OfiGest.Context;
using OfiGest.Helpers;
using OfiGest.Managers;
using OfiGest.Manegers;
using OfiGest.Utilities;
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
    .AddDataAnnotationsLocalization(); // Validaciones en español

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("CadenaSql"));
});

// Configuración de seguridad institucional
builder.Services.Configure<SeguridadCorreoOptions>(
    builder.Configuration.GetSection("SeguridadCorreo"));

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


// Autenticación con cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login/AccesoDenegado";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.None;
        options.Cookie.HttpOnly = true;

        // Configuración para mejor reconocimiento
        options.Events = new CookieAuthenticationEvents()
        {
            OnSignedIn = context =>
            {
                // La cookie se ha establecido correctamente
                return Task.CompletedTask;
            },
            OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
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
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

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

// Middleware para manejar errores 400
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == 400 && !context.Response.HasStarted)
    {
        // Redirigir al login en caso de error 400
        context.Response.Redirect("/Login");
    }
});

// Ruta por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();