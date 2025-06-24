using CDC.ProyeccionVentas.HttpClients.Auth;
using CDC.ProyeccionVentas.HttpClients.Interfaces;
using CDC.ProyeccionVentas.HttpClients.Clients;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Obtener configuraciones de URLs desde appsettings.json
var config = builder.Configuration;

var authApiUrl = config["Servicios:AuthAPI"];
var validarFechasUrl = config["Servicios:ProyeccionVentasAPI"];
var consultaVentasUrl = config["Servicios:ProyeccionVentasConsultaAPI"];

// Validaciones básicas
if (string.IsNullOrWhiteSpace(authApiUrl))
    throw new InvalidOperationException("Falta la URL de AuthAPI en appsettings.json (Servicios:AuthAPI).");

if (string.IsNullOrWhiteSpace(validarFechasUrl))
    throw new InvalidOperationException("Falta la URL de ProyeccionVentasAPI en appsettings.json (Servicios:ProyeccionVentasAPI).");

if (string.IsNullOrWhiteSpace(consultaVentasUrl))
    throw new InvalidOperationException("Falta la URL de ProyeccionVentasConsultaAPI en appsettings.json (Servicios:ProyeccionVentasConsultaAPI).");

// Agrega servicios de Razor Pages
builder.Services.AddRazorPages();

// Configuración de autenticación por cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Denied";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

// Inyección de HttpClient para Auth
builder.Services.AddHttpClient<AuthApiClient>(client =>
{
    client.BaseAddress = new Uri(authApiUrl);
});

// Inyección para ValidarFechas
builder.Services.AddHttpClient<IValidarFechasHttpClient, ValidarFechasHttpClient>(client =>
{
    client.BaseAddress = new Uri(validarFechasUrl);
});

// Inyección para consulta de proyecciones
builder.Services.AddHttpClient<IProyeccionVentasConsultaHttpClient, ProyeccionVentasConsultaHttpClient>(client =>
{
    client.BaseAddress = new Uri(consultaVentasUrl);
});

var app = builder.Build();

// Configuración del pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();
