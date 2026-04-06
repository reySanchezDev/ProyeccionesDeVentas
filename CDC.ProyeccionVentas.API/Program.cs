using CDC.ProyeccionVentas.Dominio.Interfaces;
using CDC.ProyeccionVentas.Infraestructura.Repositorios;
using CDC.ProyeccionVentas.AuthService.Servicios;
using CDC.ProyeccionVentas.Dominio.Servicios;
using CDC.ProyeccionVentas.Infraestructura.Servicios;
using Microsoft.Extensions.Configuration;



var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var cdcConnectionString = builder.Configuration.GetConnectionString("CDC")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'CDC'.");
var reportesLsConnectionString = builder.Configuration.GetConnectionString("ReportesLS")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'ReportesLS'.");

// ---------------------- INYECCIÓN DE DEPENDENCIAS ----------------------

// Autenticación (base CDC)
builder.Services.AddScoped<IUsuarioRepository>(provider =>
{
    return new UsuarioRepository(cdcConnectionString);
});
builder.Services.AddScoped<IAuthService, AuthService>();

// ReportesLS: ProyeccionVentas y Stores
builder.Services.AddScoped<IProyeccionVentasRepository>(provider =>
{
    return new ProyeccionVentasRepository(reportesLsConnectionString);
});
builder.Services.AddScoped<IProyeccionVentasService, ProyeccionVentasService>();

builder.Services.AddScoped<IStoreRepository>(provider =>
{
    return new StoreRepository(reportesLsConnectionString);
});
builder.Services.AddScoped<StoreService>();


//ReportesLS: para validar proyecciones que no se repitan en el mismo mes
builder.Services.AddScoped<IValidarFechasRepository>(provider =>
{
    return new ValidarFechasRepository(reportesLsConnectionString);
});

builder.Services.AddScoped<IValidarFechasService, ValidarFechasService>();


builder.Services.AddScoped<IProyeccionVentasConsultaService, ProyeccionVentasConsultaService>();

builder.Services.AddScoped<ICalendarioPedidosService, CalendarioPedidosService>();

builder.Services.AddScoped<ITicketStaffService>(provider =>
{
    return new TicketStaffService(reportesLsConnectionString);
});

builder.Services.AddScoped<ITicketSucursalService>(provider =>
{
    return new TicketSucursalService(reportesLsConnectionString);
});


// ----------------------------- CORS -------------------------------------

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ---------------------------- CONFIGURACIÓN -----------------------------

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
