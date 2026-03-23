using CDC.ProyeccionVentas.HttpClients.Auth;
using CDC.ProyeccionVentas.HttpClients.Interfaces;
using CDC.ProyeccionVentas.HttpClients.Clients;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// -------------------  URLs desde appsettings.json -------------------
var config = builder.Configuration;
var authApiUrl = config["Servicios:AuthAPI"];
var validarFechasUrl = config["Servicios:ProyeccionVentasAPI"];
var consultaVentasUrl = config["Servicios:ProyeccionVentasConsultaAPI"];
var ticketStaffUrl = config["Servicios:TicketStaffAPI"] ?? consultaVentasUrl;

// -------------------  Validaciones básicas  -------------------------
if (string.IsNullOrWhiteSpace(authApiUrl))
    throw new InvalidOperationException("Falta la URL de AuthAPI en appsettings.json (Servicios:AuthAPI).");
if (string.IsNullOrWhiteSpace(validarFechasUrl))
    throw new InvalidOperationException("Falta la URL de ProyeccionVentasAPI en appsettings.json (Servicios:ProyeccionVentasAPI).");
if (string.IsNullOrWhiteSpace(consultaVentasUrl))
    throw new InvalidOperationException("Falta la URL de ProyeccionVentasConsultaAPI en appsettings.json (Servicios:ProyeccionVentasConsultaAPI).");
if (string.IsNullOrWhiteSpace(ticketStaffUrl))
    throw new InvalidOperationException("Falta la URL de TicketStaffAPI en appsettings.json (Servicios:TicketStaffAPI).");

// -------------------  Servicios MVC/Razor  --------------------------
builder.Services.AddRazorPages();

// -------------------  Autenticación por cookies  --------------------
const string basePath = "/ProyeccionesVentas";

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Denied";
        options.Cookie.Path = basePath;

        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

// -------------------  HttpClients inyectados  -----------------------
builder.Services.AddHttpClient<AuthApiClient>(c => c.BaseAddress = new Uri(authApiUrl));
builder.Services.AddHttpClient<IValidarFechasHttpClient, ValidarFechasHttpClient>(c => c.BaseAddress = new Uri(validarFechasUrl));
builder.Services.AddHttpClient<IProyeccionVentasConsultaHttpClient, ProyeccionVentasConsultaHttpClient>(c => c.BaseAddress = new Uri(consultaVentasUrl));
builder.Services.AddHttpClient<IStoresHttpClient, StoresHttpClient>(c => c.BaseAddress = new Uri(consultaVentasUrl));
builder.Services.AddHttpClient<ICalendarioHttpClient, CalendarioHttpClient>( c => c.BaseAddress = new Uri(validarFechasUrl));
builder.Services.AddHttpClient<ITicketStaffHttpClient, TicketStaffHttpClient>(c => c.BaseAddress = new Uri(ticketStaffUrl));

var app = builder.Build();

// ---- Soporte para alojar como sub-aplicación en IIS (/ProyeccionesVentas)
app.UsePathBase(basePath);
app.Use((ctx, next) =>
{
    ctx.Request.PathBase = basePath;
    return next();
});
// --------------------------------------------------------------------

// --- Redirigir la raíz /ProyeccionesVentas -> /ProyeccionesVentas/Login
app.MapGet("/", ctx =>
{
    ctx.Response.Redirect($"{basePath}/Login", permanent: false);
    return Task.CompletedTask;
});
// --------------------------------------------------------------------

// -------------------  Pipeline estándar  ----------------------------
if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();









//using CDC.ProyeccionVentas.HttpClients.Auth;
//using CDC.ProyeccionVentas.HttpClients.Interfaces;
//using CDC.ProyeccionVentas.HttpClients.Clients;
//using Microsoft.AspNetCore.Authentication.Cookies;

//var builder = WebApplication.CreateBuilder(args);

//// Obtener configuraciones de URLs desde appsettings.json
//var config = builder.Configuration;

//var authApiUrl = config["Servicios:AuthAPI"];
//var validarFechasUrl = config["Servicios:ProyeccionVentasAPI"];
//var consultaVentasUrl = config["Servicios:ProyeccionVentasConsultaAPI"];

//// Validaciones básicas
//if (string.IsNullOrWhiteSpace(authApiUrl))
//    throw new InvalidOperationException("Falta la URL de AuthAPI en appsettings.json (Servicios:AuthAPI).");

//if (string.IsNullOrWhiteSpace(validarFechasUrl))
//    throw new InvalidOperationException("Falta la URL de ProyeccionVentasAPI en appsettings.json (Servicios:ProyeccionVentasAPI).");

//if (string.IsNullOrWhiteSpace(consultaVentasUrl))
//    throw new InvalidOperationException("Falta la URL de ProyeccionVentasConsultaAPI en appsettings.json (Servicios:ProyeccionVentasConsultaAPI).");

//// Agrega servicios de Razor Pages
//builder.Services.AddRazorPages();

//// Configuración de autenticación por cookies
//builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//    .AddCookie(options =>
//    {
//        options.LoginPath = "/Login";
//        options.AccessDeniedPath = "/Denied";
//        options.Cookie.HttpOnly = true;
//        options.Cookie.IsEssential = true;
//        options.SlidingExpiration = true;
//        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
//    });

//// Inyección de HttpClient para Auth
//builder.Services.AddHttpClient<AuthApiClient>(client =>
//{
//    client.BaseAddress = new Uri(authApiUrl);
//});

//// Inyección para ValidarFechas
//builder.Services.AddHttpClient<IValidarFechasHttpClient, ValidarFechasHttpClient>(client =>
//{
//    client.BaseAddress = new Uri(validarFechasUrl);
//});

//// Inyección para consulta de proyecciones
//builder.Services.AddHttpClient<IProyeccionVentasConsultaHttpClient, ProyeccionVentasConsultaHttpClient>(client =>
//{
//    client.BaseAddress = new Uri(consultaVentasUrl);
//});

//var app = builder.Build();

//// Configuración del pipeline
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//}

//app.UseStaticFiles();
//app.UseRouting();
//app.UseAuthentication();
//app.UseAuthorization();

//app.MapStaticAssets();
//app.MapRazorPages().WithStaticAssets();

//app.Run();


