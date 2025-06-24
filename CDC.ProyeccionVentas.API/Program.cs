using CDC.ProyeccionVentas.Dominio.Interfaces;
using CDC.ProyeccionVentas.Infraestructura.Repositorios;
using CDC.ProyeccionVentas.AuthService.Servicios;
using CDC.ProyeccionVentas.Dominio.Servicios;
using CDC.ProyeccionVentas.Infraestructura.Servicios;



var builder = WebApplication.CreateBuilder(args);

// ---------------------- INYECCIÓN DE DEPENDENCIAS ----------------------

// Autenticación (base CDC)
builder.Services.AddScoped<IUsuarioRepository>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("CDC");
    return new UsuarioRepository(connectionString);
});
builder.Services.AddScoped<IAuthService, AuthService>();

// ReportesLS: ProyeccionVentas y Stores
builder.Services.AddScoped<IProyeccionVentasRepository>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("ReportesLS");
    return new ProyeccionVentasRepository(connectionString);
});
builder.Services.AddScoped<IProyeccionVentasService, ProyeccionVentasService>();

builder.Services.AddScoped<IStoreRepository>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("ReportesLS");
    return new StoreRepository(connectionString);
});
builder.Services.AddScoped<StoreService>();


//ReportesLS: para validar proyecciones que no se repitan en el mismo mes
builder.Services.AddScoped<IValidarFechasRepository>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("ReportesLS");
    return new ValidarFechasRepository(connectionString);
});

builder.Services.AddScoped<IValidarFechasService, ValidarFechasService>();


builder.Services.AddScoped<IProyeccionVentasConsultaService, ProyeccionVentasConsultaService>();


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
