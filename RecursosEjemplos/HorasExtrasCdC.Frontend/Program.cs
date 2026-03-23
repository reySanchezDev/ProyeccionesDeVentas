using HorasExtrasCdC.Frontend.Services;
using HorasExtrasCdC.Frontend.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
var appStartedAtUtc = DateTimeOffset.UtcNow;

builder.Services.AddRazorPages();
builder.Services.Configure<AuthRolesOptions>(
    builder.Configuration.GetSection(AuthRolesOptions.SectionName));
builder.Services.Configure<ApiSettingsOptions>(
    builder.Configuration.GetSection(ApiSettingsOptions.SectionName));

var dataProtectionSection = builder.Configuration.GetSection("DataProtection");
var dataProtectionApplicationName = dataProtectionSection["ApplicationName"];
if (string.IsNullOrWhiteSpace(dataProtectionApplicationName))
{
    dataProtectionApplicationName = "HorasExtrasCdC.Frontend";
}

var useEphemeralInDevelopment = dataProtectionSection
    .GetValue("UseEphemeralInDevelopment", true);
var requireWritableKeyRing = dataProtectionSection
    .GetValue("RequireWritableKeyRing", true);
var configuredDataProtectionKeysPath = dataProtectionSection["KeysPath"];
var dataProtectionKeysPath = string.IsNullOrWhiteSpace(configuredDataProtectionKeysPath)
    ? Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys")
    : configuredDataProtectionKeysPath.Trim();

var canUseEphemeral = builder.Environment.IsDevelopment() && useEphemeralInDevelopment;

if (canUseEphemeral && string.IsNullOrWhiteSpace(configuredDataProtectionKeysPath))
{
    builder.Services.AddSingleton<IDataProtectionProvider>(_ => new EphemeralDataProtectionProvider());
    Console.WriteLine("[DataProtection] Using ephemeral keys in Development.");
}
else if (TryPrepareWritableDirectory(dataProtectionKeysPath, out var dataProtectionError))
{
    var dataProtectionBuilder = builder.Services.AddDataProtection()
        .SetApplicationName(dataProtectionApplicationName)
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

    if (OperatingSystem.IsWindows())
    {
        dataProtectionBuilder.ProtectKeysWithDpapi(protectToLocalMachine: true);
    }

    Console.WriteLine($"[DataProtection] Key ring path: {dataProtectionKeysPath}");
}
else if (canUseEphemeral && !requireWritableKeyRing)
{
    builder.Services.AddSingleton<IDataProtectionProvider>(_ => new EphemeralDataProtectionProvider());
    Console.WriteLine(
        $"[DataProtection] Using ephemeral keys because '{dataProtectionKeysPath}' is not writable. " +
        $"Reason: {dataProtectionError}");
}
else
{
    throw new InvalidOperationException(
        $"No se puede usar DataProtection key-ring en '{dataProtectionKeysPath}'. " +
        $"Detalle: {dataProtectionError}. " +
        "Conceda permisos de escritura al App Pool de IIS o configure DataProtection:KeysPath.");
}


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccesoDenegado";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var issuedUtc = context.Properties?.IssuedUtc;
                if (!issuedUtc.HasValue || issuedUtc.Value < appStartedAtUtc)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("GhOnly", policy => policy.RequireClaim("rolPrincipal", "GH"));
    options.AddPolicy("SupervisorOnly", policy => policy.RequireClaim("rolPrincipal", "SUPERVISOR"));
    options.AddPolicy(
        "SupervisorPanelAccess",
        policy => policy.RequireClaim("rolPrincipal", "SUPERVISOR", "EMPLEADO"));
});

var apiSettings = builder.Configuration
    .GetSection(ApiSettingsOptions.SectionName)
    .Get<ApiSettingsOptions>() ?? new ApiSettingsOptions();
if (string.IsNullOrWhiteSpace(apiSettings.BaseUrl))
{
    throw new InvalidOperationException("ApiSettings:BaseUrl no esta configurado en appsettings.");
}

builder.Services.AddHttpClient<IHorasExtrasApiClient, HorasExtrasApiClient>(client =>
{
    client.BaseAddress = new Uri(apiSettings.BaseUrl.TrimEnd('/'));
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();
var headerLogoBytes = TryLoadHeaderLogoBytes(builder.Environment, out var headerLogoSource, out var headerLogoError);
var criticalStaticAssets = LoadCriticalStaticAssets(builder.Environment);
if (headerLogoBytes is { Length: > 0 })
{
    Console.WriteLine($"[Branding] Header logo cargado desde: {headerLogoSource}");
}
else
{
    Console.WriteLine($"[Branding] No se pudo cargar el header logo. Detalle: {headerLogoError}");
}
Console.WriteLine($"[StaticAssets] Assets criticos cargados en memoria: {criticalStaticAssets.Count}");

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = feature?.Error;

        if (TryResolveConnectivityError(exception, out var tipo, out var mensaje))
        {
            var requestPath = string.IsNullOrWhiteSpace(feature?.Path)
                ? context.Request.Path.Value
                : feature?.Path;
            var returnUrl = $"{context.Request.Path}{context.Request.QueryString}";
            var endpoint = ResolveConnectivityEndpoint(exception);
            var detalleTecnico = BuildConnectivityTechnicalDetail(exception);
            var query = BuildConnectivityQuery(
                tipo,
                mensaje,
                requestPath ?? "/",
                returnUrl,
                endpoint,
                detalleTecnico,
                context.TraceIdentifier,
                "backend-exception",
                StatusCodes.Status503ServiceUnavailable);
            var redirectUrl = "/ErrorConexion" + query;

            if (RequestWantsJson(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "application/json; charset=utf-8";
                var payload = JsonSerializer.Serialize(new
                {
                    codigo = 503,
                    tipo,
                    mensaje,
                    detalleTecnico,
                    endpoint,
                    origen = requestPath,
                    traceId = context.TraceIdentifier,
                    redirectUrl
                });
                await context.Response.WriteAsync(payload);
                return;
            }

            context.Response.Redirect(redirectUrl);
            return;
        }

        context.Response.Redirect("/Error");
    });
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    var requestPath = context.Request.Path.Value;
    if (!string.IsNullOrWhiteSpace(requestPath) &&
        criticalStaticAssets.TryGetValue(requestPath, out var asset))
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = asset.ContentType;
        context.Response.Headers.CacheControl = "public,max-age=86400";
        await context.Response.Body.WriteAsync(asset.Content, context.RequestAborted);
        return;
    }

    await next();
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/_asset/header-logo", () =>
{
    if (headerLogoBytes is { Length: > 0 })
    {
        return Results.File(headerLogoBytes, "image/png");
    }

    return Results.NotFound(new
    {
        mensaje = "Logo no disponible.",
        detalle = headerLogoError ?? "No se pudo cargar el recurso de logo."
    });
});

app.MapRazorPages();

app.Run();

static bool TryResolveConnectivityError(Exception? exception, out string tipo, out string mensaje)
{
    switch (exception)
    {
        case ApiConnectivityException connectivity:
            tipo = connectivity.IsTimeout ? "timeout" : "conexion";
            mensaje = connectivity.UserMessage;
            return true;

        case TaskCanceledException:
            tipo = "timeout";
            mensaje = "La solicitud al servicio excedio el tiempo de espera. Intente nuevamente.";
            return true;

        case HttpRequestException:
            tipo = "conexion";
            mensaje = "No se pudo conectar con el servicio. Revise su red e intente nuevamente.";
            return true;

        default:
            tipo = string.Empty;
            mensaje = string.Empty;
            return false;
    }
}

static string? ResolveConnectivityEndpoint(Exception? exception)
{
    return exception switch
    {
        ApiConnectivityException connectivity => connectivity.Endpoint,
        _ => null
    };
}

static string BuildConnectivityTechnicalDetail(Exception? exception)
{
    return exception switch
    {
        ApiConnectivityException connectivity => BuildConnectivityExceptionDetail(connectivity),
        TaskCanceledException timeout => Truncate(timeout.Message, 900),
        HttpRequestException httpRequest => Truncate(httpRequest.Message, 900),
        _ => "Error de conectividad no clasificado."
    };
}

static string BuildConnectivityExceptionDetail(ApiConnectivityException exception)
{
    var endpoint = string.IsNullOrWhiteSpace(exception.Endpoint)
        ? "N/D"
        : exception.Endpoint.Trim();
    var inner = exception.InnerException?.Message;
    var baseDetail = $"Endpoint: {endpoint}.";

    if (string.IsNullOrWhiteSpace(inner))
    {
        return baseDetail;
    }

    return Truncate($"{baseDetail} Causa: {inner.Trim()}", 900);
}

static QueryString BuildConnectivityQuery(
    string tipo,
    string mensaje,
    string origen,
    string returnUrl,
    string? endpoint,
    string? detalleTecnico,
    string? traceId,
    string? fuente,
    int codigo)
{
    var query = QueryString.Empty;
    query = query.Add("tipo", Truncate(tipo, 40));
    query = query.Add("mensaje", Truncate(mensaje, 350));
    query = query.Add("origen", Truncate(origen, 250));
    query = query.Add("returnUrl", Truncate(returnUrl, 450));
    query = query.Add("codigo", codigo.ToString());

    if (!string.IsNullOrWhiteSpace(endpoint))
    {
        query = query.Add("endpoint", Truncate(endpoint, 280));
    }

    if (!string.IsNullOrWhiteSpace(detalleTecnico))
    {
        query = query.Add("detalle", Truncate(detalleTecnico, 900));
    }

    if (!string.IsNullOrWhiteSpace(traceId))
    {
        query = query.Add("traceId", Truncate(traceId, 140));
    }

    if (!string.IsNullOrWhiteSpace(fuente))
    {
        query = query.Add("fuente", Truncate(fuente, 120));
    }

    return query;
}

static string Truncate(string? value, int maxLength)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return string.Empty;
    }

    var trimmed = value.Trim();
    if (trimmed.Length <= maxLength)
    {
        return trimmed;
    }

    return trimmed[..maxLength];
}

static bool RequestWantsJson(HttpRequest request)
{
    var accept = request.Headers.Accept.ToString();
    if (accept.Contains("application/json", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    var requestedWith = request.Headers["X-Requested-With"].ToString();
    if (string.Equals(requestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    var contentType = request.ContentType ?? string.Empty;
    return contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
}

static bool TryPrepareWritableDirectory(string directoryPath, out string? error)
{
    try
    {
        Directory.CreateDirectory(directoryPath);
        var probePath = Path.Combine(directoryPath, $".probe-{Guid.NewGuid():N}.tmp");
        File.WriteAllText(probePath, "ok");
        File.Delete(probePath);
        error = null;
        return true;
    }
    catch (Exception ex)
    {
        error = ex.Message;
        return false;
    }
}

static byte[]? TryLoadHeaderLogoBytes(
    IWebHostEnvironment environment,
    out string source,
    out string? error)
{
    const string embeddedLogoName = "HorasExtrasCdC.Frontend.BrandAssets.HeaderLogoDark";
    error = null;

    try
    {
        using var embeddedStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(embeddedLogoName);
        if (embeddedStream is not null)
        {
            using var memoryStream = new MemoryStream();
            embeddedStream.CopyTo(memoryStream);
            source = $"embedded:{embeddedLogoName}";
            return memoryStream.ToArray();
        }
    }
    catch (Exception ex)
    {
        error = $"Error leyendo recurso embebido: {ex.Message}";
    }

    if (string.IsNullOrWhiteSpace(environment.WebRootPath))
    {
        source = "none";
        error ??= "WebRootPath no configurado y recurso embebido no encontrado.";
        return null;
    }

    var logoPath = Path.Combine(environment.WebRootPath, "resources", "logo-dark.png");
    source = logoPath;

    if (!File.Exists(logoPath))
    {
        error ??= $"Archivo no encontrado: {logoPath}";
        return null;
    }

    try
    {
        return File.ReadAllBytes(logoPath);
    }
    catch (Exception ex)
    {
        error = $"Error leyendo archivo de logo: {ex.Message}";
        return null;
    }
}

static Dictionary<string, EmbeddedStaticAsset> LoadCriticalStaticAssets(IWebHostEnvironment environment)
{
    var assets = new Dictionary<string, EmbeddedStaticAsset>(StringComparer.OrdinalIgnoreCase);
    var assembly = Assembly.GetExecutingAssembly();
    var webRoot = environment.WebRootPath ?? string.Empty;

    TryAddEmbeddedOrDiskAsset(
        assets,
        requestPath: "/js/site.js",
        embeddedResourceName: "HorasExtrasCdC.Frontend.StaticAssets.Js.Site",
        fallbackPath: Path.Combine(webRoot, "js", "site.js"),
        contentType: "text/javascript; charset=utf-8",
        assembly);

    TryAddEmbeddedOrDiskAsset(
        assets,
        requestPath: "/js/listado-gh.js",
        embeddedResourceName: "HorasExtrasCdC.Frontend.StaticAssets.Js.ListadoGh",
        fallbackPath: Path.Combine(webRoot, "js", "listado-gh.js"),
        contentType: "text/javascript; charset=utf-8",
        assembly);

    TryAddEmbeddedOrDiskAsset(
        assets,
        requestPath: "/vendor/daterangepicker/jquery-3.1.0.min.js",
        embeddedResourceName: "HorasExtrasCdC.Frontend.StaticAssets.Vendor.DateRangePicker.JQuery",
        fallbackPath: Path.Combine(webRoot, "vendor", "daterangepicker", "jquery-3.1.0.min.js"),
        contentType: "text/javascript; charset=utf-8",
        assembly);

    TryAddEmbeddedOrDiskAsset(
        assets,
        requestPath: "/vendor/daterangepicker/moment.min.js",
        embeddedResourceName: "HorasExtrasCdC.Frontend.StaticAssets.Vendor.DateRangePicker.Moment",
        fallbackPath: Path.Combine(webRoot, "vendor", "daterangepicker", "moment.min.js"),
        contentType: "text/javascript; charset=utf-8",
        assembly);

    TryAddEmbeddedOrDiskAsset(
        assets,
        requestPath: "/vendor/daterangepicker/daterangepicker.js",
        embeddedResourceName: "HorasExtrasCdC.Frontend.StaticAssets.Vendor.DateRangePicker.Plugin",
        fallbackPath: Path.Combine(webRoot, "vendor", "daterangepicker", "daterangepicker.js"),
        contentType: "text/javascript; charset=utf-8",
        assembly);

    TryAddEmbeddedOrDiskAsset(
        assets,
        requestPath: "/vendor/daterangepicker/daterangepickerstyle.css",
        embeddedResourceName: "HorasExtrasCdC.Frontend.StaticAssets.Vendor.DateRangePicker.Style",
        fallbackPath: Path.Combine(webRoot, "vendor", "daterangepicker", "daterangepickerstyle.css"),
        contentType: "text/css; charset=utf-8",
        assembly);

    return assets;
}

static void TryAddEmbeddedOrDiskAsset(
    IDictionary<string, EmbeddedStaticAsset> assets,
    string requestPath,
    string embeddedResourceName,
    string fallbackPath,
    string contentType,
    Assembly assembly)
{
    byte[]? content = null;

    try
    {
        using var stream = assembly.GetManifestResourceStream(embeddedResourceName);
        if (stream is not null)
        {
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            content = buffer.ToArray();
            Console.WriteLine($"[StaticAssets] {requestPath} cargado desde embedded resource.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[StaticAssets] Error leyendo recurso embebido {embeddedResourceName}: {ex.Message}");
    }

    if (content is null && File.Exists(fallbackPath))
    {
        try
        {
            content = File.ReadAllBytes(fallbackPath);
            Console.WriteLine($"[StaticAssets] {requestPath} cargado desde disco ({fallbackPath}).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StaticAssets] Error leyendo archivo {fallbackPath}: {ex.Message}");
        }
    }

    if (content is { Length: > 0 })
    {
        assets[requestPath] = new EmbeddedStaticAsset(content, contentType);
    }
}

readonly record struct EmbeddedStaticAsset(byte[] Content, string ContentType);
