using HorasExtrasCdC.Frontend.Models;
using HorasExtrasCdC.Frontend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HorasExtrasCdC.Frontend.Pages;

[Authorize(Policy = "SupervisorPanelAccess")]
public class ListadoxSupervisorModel : PageModel
{
    private readonly IHorasExtrasApiClient _apiClient;

    public ListadoxSupervisorModel(IHorasExtrasApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [BindProperty]
    public string? EmpleadoFiltro { get; set; }

    [BindProperty]
    public string? FechaEntradaInicio { get; set; }

    [BindProperty]
    public string? FechaEntradaFin { get; set; }

    [BindProperty]
    public string? FechaEntradaRango { get; set; }

    public IReadOnlyList<HorasExtraSupervisorItemResponse> ListadoSupervisor { get; private set; } = Array.Empty<HorasExtraSupervisorItemResponse>();
    public IReadOnlyList<HorasExtraConsolidadaItemResponse> ConsolidadoSupervisor { get; private set; } = Array.Empty<HorasExtraConsolidadaItemResponse>();

    public string? InfoMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    public string IdSupervisorAutenticado => User.FindFirst("numeroEmpleado")?.Value ?? string.Empty;
    public int CantidadSub { get; private set; }
    public bool PuedeAprobar => CantidadSub > 0;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            SetDefaultDateRangeIfEmpty();
            await CargarListadoAsync(cancellationToken);
            return Page();
        }
        catch (ApiConnectivityException ex)
        {
            return RedirectToConnectivityPage(ex, "ListadoxSupervisor.OnGet");
        }
    }

    public async Task<IActionResult> OnPostBuscarAsync(CancellationToken cancellationToken)
    {
        try
        {
            SetDefaultDateRangeIfEmpty();
            await CargarListadoAsync(cancellationToken);
            return Page();
        }
        catch (ApiConnectivityException ex)
        {
            return RedirectToConnectivityPage(ex, "ListadoxSupervisor.OnPostBuscar");
        }
    }

    public async Task<IActionResult> OnPostLimpiarAsync(CancellationToken cancellationToken)
    {
        try
        {
            ModelState.Clear();
            EmpleadoFiltro = string.Empty;
            SetDefaultDateRange();
            await CargarListadoAsync(cancellationToken);
            return Page();
        }
        catch (ApiConnectivityException ex)
        {
            return RedirectToConnectivityPage(ex, "ListadoxSupervisor.OnPostLimpiar");
        }
    }

    public async Task<IActionResult> OnPostAprobarHoraExtraAsync([FromBody] AprobarHoraExtraInput? input, CancellationToken cancellationToken)
    {
        if (input is null)
        {
            return BadRequest(new { codigo = 400, mensaje = "La solicitud es requerida." });
        }

        if (!TryValidateModel(input))
        {
            var errores = ModelState.Values
                .SelectMany(value => value.Errors.Select(error => error.ErrorMessage))
                .Where(error => !string.IsNullOrWhiteSpace(error))
                .ToArray();

            return BadRequest(new
            {
                codigo = 400,
                mensaje = "Datos de entrada invalidos.",
                errores
            });
        }

        if (string.IsNullOrWhiteSpace(IdSupervisorAutenticado))
        {
            return StatusCode(401, new { codigo = 401, mensaje = "Sesion expirada. Inicie sesion nuevamente." });
        }

        if (!await ResolverContextoSupervisorAsync(cancellationToken))
        {
            var mensaje = string.IsNullOrWhiteSpace(ErrorMessage)
                ? "No se pudo resolver el contexto del supervisor."
                : ErrorMessage;
            return StatusCode(500, new { codigo = 500, mensaje });
        }

        if (!PuedeAprobar)
        {
            return StatusCode(403, new
            {
                codigo = 403,
                mensaje = "No esta autorizado para aprobar horas extras."
            });
        }

        var request = new HorasExtraAgregarRequest
        {
            IdTabla = input.IdTabla,
            HExt = input.HExt.Trim(),
            Descripcion = (input.Descripcion ?? string.Empty).Trim(),
            UserAprueba = IdSupervisorAutenticado.Trim(),
            IdSqlite = (input.IdSqlite ?? string.Empty).Trim(),
            Sucursal = (input.Sucursal ?? string.Empty).Trim(),
            Empleado = (input.Empleado ?? string.Empty).Trim(),
            FechaEntro = (input.FechaEntro ?? string.Empty).Trim()
        };

        HorasExtraAgregarResponse resultado;
        try
        {
            resultado = await _apiClient.AgregarHorasExtrasAsync(request, cancellationToken);
        }
        catch (ApiConnectivityException ex)
        {
            return BuildConnectivityErrorResponse(ex, "ListadoxSupervisor.OnPostAprobarHoraExtra");
        }

        return resultado.Codigo switch
        {
            0 => new JsonResult(resultado),
            400 => BadRequest(resultado),
            _ => StatusCode(500, resultado),
        };
    }

    public async Task<IActionResult> OnGetExportConsolidadoDataAsync(
        [FromQuery] string? empleadoFiltro,
        [FromQuery] string? fechaEntradaInicio,
        [FromQuery] string? fechaEntradaFin,
        [FromQuery] string? fechaEntradaRango,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(IdSupervisorAutenticado))
        {
            return StatusCode(401, new { codigo = 401, mensaje = "Sesion expirada. Inicie sesion nuevamente." });
        }

        try
        {
            if (!await ResolverContextoSupervisorAsync(cancellationToken))
            {
                return StatusCode(500, new
                {
                    codigo = 500,
                    mensaje = string.IsNullOrWhiteSpace(ErrorMessage)
                        ? "No se pudo resolver el contexto del supervisor."
                        : ErrorMessage
                });
            }

            var supervisorConsulta = ObtenerSupervisorConsulta();
            var filtros = ObtenerFiltrosExportacion(empleadoFiltro, fechaEntradaInicio, fechaEntradaFin, fechaEntradaRango);
            var datos = await _apiClient.ListarConsolidadasSupervisorAsync(
                supervisorConsulta,
                filtros.IdEmpleado,
                filtros.FechaI,
                filtros.FechaF,
                cancellationToken);

            return new JsonResult(new
            {
                codigo = 0,
                mensaje = "OK",
                datos,
                filtros = new
                {
                    supervisor = supervisorConsulta,
                    idEmpleado = filtros.IdEmpleado,
                    fechaI = filtros.FechaI,
                    fechaF = filtros.FechaF
                }
            });
        }
        catch (ApiConnectivityException ex)
        {
            return BuildConnectivityErrorResponse(ex, "ListadoxSupervisor.OnGetExportConsolidadoData");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { codigo = 500, mensaje = ex.Message });
        }
    }

    public async Task<IActionResult> OnGetExportMarcadasDataAsync(
        [FromQuery] string? empleadoFiltro,
        [FromQuery] string? fechaEntradaInicio,
        [FromQuery] string? fechaEntradaFin,
        [FromQuery] string? fechaEntradaRango,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(IdSupervisorAutenticado))
        {
            return StatusCode(401, new { codigo = 401, mensaje = "Sesion expirada. Inicie sesion nuevamente." });
        }

        try
        {
            if (!await ResolverContextoSupervisorAsync(cancellationToken))
            {
                return StatusCode(500, new
                {
                    codigo = 500,
                    mensaje = string.IsNullOrWhiteSpace(ErrorMessage)
                        ? "No se pudo resolver el contexto del supervisor."
                        : ErrorMessage
                });
            }

            var supervisorConsulta = ObtenerSupervisorConsulta();
            var filtros = ObtenerFiltrosExportacion(empleadoFiltro, fechaEntradaInicio, fechaEntradaFin, fechaEntradaRango);
            IReadOnlyList<HorasExtraReporteHeSupervisorItemResponse> datos;
            if (!PuedeAprobar)
            {
                // Paridad operativa con la vista principal: en modo empleado
                // exporta exactamente el mismo dataset mostrado en pantalla.
                var listadoFiltrado = await _apiClient.ListarListadoSupervisorFiltradoAsync(
                    supervisorConsulta,
                    filtros.IdEmpleado,
                    filtros.FechaI,
                    filtros.FechaF,
                    cancellationToken);

                datos = listadoFiltrado
                    .Select(MapearAReporteMarcadas)
                    .ToList();
            }
            else
            {
                datos = await _apiClient.ListarReporteHeSupervisorAsync(
                    supervisorConsulta,
                    filtros.IdEmpleado,
                    filtros.FechaI,
                    filtros.FechaF,
                    cancellationToken);
            }

            return new JsonResult(new
            {
                codigo = 0,
                mensaje = "OK",
                datos,
                filtros = new
                {
                    supervisor = supervisorConsulta,
                    idEmpleado = filtros.IdEmpleado,
                    fechaI = filtros.FechaI,
                    fechaF = filtros.FechaF
                }
            });
        }
        catch (ApiConnectivityException ex)
        {
            return BuildConnectivityErrorResponse(ex, "ListadoxSupervisor.OnGetExportMarcadasData");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { codigo = 500, mensaje = ex.Message });
        }
    }

    private static HorasExtraReporteHeSupervisorItemResponse MapearAReporteMarcadas(HorasExtraSupervisorItemResponse row)
    {
        return new HorasExtraReporteHeSupervisorItemResponse
        {
            Empleado = (row.Empleado ?? string.Empty).Trim(),
            Nombres = (row.NombreCompleto ?? row.NombreEmpleado ?? string.Empty).Trim(),
            Apellidos = string.Empty,
            UbicadoEn = (row.Ubicacion ?? row.UbicadoEn ?? string.Empty).Trim(),
            MarcaEn = (row.Sucursal ?? string.Empty).Trim(),
            Fecha = row.Fecha,
            Entrada = row.Entrada,
            Salida = row.Salida,
            Laboradas = row.HLaboradas ?? 0m,
            Aprobadas = row.HExt ?? 0m,
            Observaciones = row.Descripcion
        };
    }

    private async Task CargarListadoAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(IdSupervisorAutenticado))
        {
            ErrorMessage = "No se encontro el numero de supervisor en la sesion.";
            return;
        }

        try
        {
            if (!await ResolverContextoSupervisorAsync(cancellationToken))
            {
                return;
            }

            if (!PuedeAprobar)
            {
                // Paridad con legacy: cuando no tiene subordinados, queda anclado a su propio empleado.
                EmpleadoFiltro = IdSupervisorAutenticado.Trim();
            }

            var supervisorConsulta = ObtenerSupervisorConsulta();
            var filtros = ObtenerFiltrosExportacion();

            ListadoSupervisor = await _apiClient.ListarListadoSupervisorFiltradoAsync(
                supervisorConsulta,
                filtros.IdEmpleado,
                filtros.FechaI,
                filtros.FechaF,
                cancellationToken);

            var listadoContexto = await _apiClient.ListarListadoSupervisorFiltradoAsync(
                supervisorConsulta,
                filtros.IdEmpleado,
                null,
                null,
                cancellationToken);

            ConsolidadoSupervisor = await _apiClient.ListarConsolidadasSupervisorAsync(
                supervisorConsulta,
                filtros.IdEmpleado,
                filtros.FechaI,
                filtros.FechaF,
                cancellationToken);

            var contexto = PuedeAprobar
                ? "Aprobacion habilitada."
                : "Solo lectura: no autorizado para aprobar.";
            InfoMessage = $"Registros encontrados: {ListadoSupervisor.Count} de {listadoContexto.Count}. {contexto}";
        }
        catch (ApiConnectivityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task<bool> ResolverContextoSupervisorAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cantidadSub = await _apiClient.ObtenerCantidadSubAsync(IdSupervisorAutenticado, cancellationToken);
            if (cantidadSub.Codigo != 0)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(cantidadSub.Mensaje)
                    ? "No se pudo obtener la cantidad de subordinados."
                    : cantidadSub.Mensaje;
                return false;
            }

            CantidadSub = Math.Max(0, cantidadSub.CantidadSub);

            return true;
        }
        catch (ApiConnectivityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }

    private string ObtenerSupervisorConsulta()
    {
        return IdSupervisorAutenticado.Trim();
    }

    private (string? IdEmpleado, string? FechaI, string? FechaF) ObtenerFiltrosExportacion(
        string? empleadoFiltro = null,
        string? fechaEntradaInicio = null,
        string? fechaEntradaFin = null,
        string? fechaEntradaRango = null)
    {
        SetDefaultDateRangeIfEmpty();

        var idEmpleado = (empleadoFiltro ?? EmpleadoFiltro ?? string.Empty).Trim();
        if (!PuedeAprobar)
        {
            idEmpleado = IdSupervisorAutenticado.Trim();
        }

        var fechaInicio = ParseFilterDate(fechaEntradaInicio ?? FechaEntradaInicio);
        var fechaFin = ParseFilterDate(fechaEntradaFin ?? FechaEntradaFin);

        if (!fechaInicio.HasValue && !fechaFin.HasValue &&
            TryParseRangeDate(fechaEntradaRango ?? FechaEntradaRango, out var fechaInicioRango, out var fechaFinRango))
        {
            fechaInicio = fechaInicioRango;
            fechaFin = fechaFinRango;
        }

        if (fechaInicio.HasValue && fechaFin.HasValue && fechaFin.Value.Date < fechaInicio.Value.Date)
        {
            (fechaInicio, fechaFin) = (fechaFin, fechaInicio);
        }

        return (
            IdEmpleado: idEmpleado.Length == 0 ? null : idEmpleado,
            FechaI: fechaInicio?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            FechaF: fechaFin?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    private void SetDefaultDateRangeIfEmpty()
    {
        if (string.IsNullOrWhiteSpace(FechaEntradaInicio) &&
            string.IsNullOrWhiteSpace(FechaEntradaFin) &&
            string.IsNullOrWhiteSpace(FechaEntradaRango))
        {
            SetDefaultDateRange();
            return;
        }

        if (!string.IsNullOrWhiteSpace(FechaEntradaInicio) &&
            !string.IsNullOrWhiteSpace(FechaEntradaFin) &&
            string.IsNullOrWhiteSpace(FechaEntradaRango))
        {
            if (TryParseFlexibleDate(FechaEntradaInicio, out var inicio) &&
                TryParseFlexibleDate(FechaEntradaFin, out var fin))
            {
                FechaEntradaRango = $"{inicio:MM/dd/yyyy} - {fin:MM/dd/yyyy}";
            }
        }
    }

    private void SetDefaultDateRange()
    {
        var hoy = DateTime.Today;
        var desde = hoy.AddDays(-15);

        FechaEntradaInicio = desde.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        FechaEntradaFin = hoy.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        FechaEntradaRango = $"{desde:MM/dd/yyyy} - {hoy:MM/dd/yyyy}";
    }

    private static DateTime? ParseFilterDate(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return null;
        }

        var filterFormats = new[]
        {
            "yyyy-MM-dd",
            "MM/dd/yyyy",
            "M/d/yyyy",
            "dd/MM/yyyy",
            "d/M/yyyy"
        };

        if (DateTime.TryParseExact(normalized, filterFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exactDate))
        {
            return exactDate.Date;
        }

        return TryParseFlexibleDate(normalized, out var genericDate) ? genericDate.Date : null;
    }

    private static bool TryParseRangeDate(string? rangeValue, out DateTime? fechaInicio, out DateTime? fechaFin)
    {
        fechaInicio = null;
        fechaFin = null;

        var normalized = (rangeValue ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return false;
        }

        var parts = ExtractRangeParts(normalized);
        if (parts is null)
        {
            return false;
        }

        var start = ParseFilterDate(parts.Value.Start);
        var end = ParseFilterDate(parts.Value.End);
        if (!start.HasValue || !end.HasValue)
        {
            return false;
        }

        if (start.Value.Date <= end.Value.Date)
        {
            fechaInicio = start.Value.Date;
            fechaFin = end.Value.Date;
        }
        else
        {
            fechaInicio = end.Value.Date;
            fechaFin = start.Value.Date;
        }

        return true;
    }

    private static (string Start, string End)? ExtractRangeParts(string normalized)
    {
        var parts = normalized.Split(" - ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            return (parts[0], parts[1]);
        }

        var compactMatch = Regex.Match(
            normalized,
            @"^(?<start>(?:\d{4}-\d{1,2}-\d{1,2})|(?:\d{1,2}/\d{1,2}/\d{4}))\s*[-–—]\s*(?<end>(?:\d{4}-\d{1,2}-\d{1,2})|(?:\d{1,2}/\d{1,2}/\d{4}))$");
        if (!compactMatch.Success)
        {
            return null;
        }

        return (
            compactMatch.Groups["start"].Value.Trim(),
            compactMatch.Groups["end"].Value.Trim());
    }

    private static bool TryParseFlexibleDate(string? value, out DateTime date)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            date = default;
            return false;
        }

        var formats = new[]
        {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd",
            "dd/MM/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm",
            "dd/MM/yyyy",
            "MM/dd/yyyy HH:mm:ss",
            "MM/dd/yyyy HH:mm",
            "MM/dd/yyyy"
        };

        if (DateTime.TryParseExact(
            normalized,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out date))
        {
            return true;
        }

        return DateTime.TryParse(
            normalized,
            CultureInfo.GetCultureInfo("es-ES"),
            DateTimeStyles.AllowWhiteSpaces,
            out date)
            || DateTime.TryParse(
                normalized,
                CultureInfo.GetCultureInfo("en-US"),
                DateTimeStyles.AllowWhiteSpaces,
            out date);
    }

    private IActionResult BuildConnectivityErrorResponse(ApiConnectivityException ex, string origen)
    {
        var tipo = ex.IsTimeout ? "timeout" : "conexion";
        var detalleTecnico = BuildConnectivityDetail(ex);
        var redirectUrl = Url.Page(
            "/ErrorConexion",
            pageHandler: null,
            values: new
            {
                tipo,
                mensaje = ex.UserMessage,
                detalle = detalleTecnico,
                endpoint = ex.Endpoint,
                origen,
                returnUrl = $"{Request.Path}{Request.QueryString}",
                fuente = "frontend-handler",
                codigo = StatusCodes.Status503ServiceUnavailable
            },
            protocol: null) ?? "/ErrorConexion";

        return StatusCode(StatusCodes.Status503ServiceUnavailable, new
        {
            codigo = 503,
            tipo,
            mensaje = ex.UserMessage,
            detalleTecnico,
            endpoint = ex.Endpoint,
            origen,
            redirectUrl
        });
    }

    private static string BuildConnectivityDetail(ApiConnectivityException ex)
    {
        var endpoint = string.IsNullOrWhiteSpace(ex.Endpoint) ? "N/D" : ex.Endpoint.Trim();
        var baseDetail = $"Endpoint: {endpoint}.";
        var inner = ex.InnerException?.Message;
        if (string.IsNullOrWhiteSpace(inner))
        {
            return baseDetail;
        }

        var detail = $"{baseDetail} Causa: {inner.Trim()}";
        return detail.Length <= 900 ? detail : detail[..900];
    }

    private IActionResult RedirectToConnectivityPage(ApiConnectivityException ex, string origen)
    {
        var tipo = ex.IsTimeout ? "timeout" : "conexion";
        var detalle = BuildConnectivityDetail(ex);

        return RedirectToPage(
            "/ErrorConexion",
            new
            {
                tipo,
                mensaje = ex.UserMessage,
                detalle,
                endpoint = ex.Endpoint,
                origen,
                returnUrl = $"{Request.Path}{Request.QueryString}",
                fuente = "supervisor-page",
                codigo = StatusCodes.Status503ServiceUnavailable
            })!;
    }

    public class AprobarHoraExtraInput : IValidatableObject
    {
        [Range(1, int.MaxValue, ErrorMessage = "El campo idTabla debe ser mayor que cero.")]
        public int? IdTabla { get; set; }

        [Required(ErrorMessage = "El campo hExt es requerido.")]
        [RegularExpression(@"^\d{1,3}([.,]\d{1,2})?$", ErrorMessage = "El campo hExt debe ser numerico con hasta 2 decimales.")]
        public string HExt { get; set; } = string.Empty;

        [Required(ErrorMessage = "El campo descripcion es requerido.")]
        [MaxLength(500, ErrorMessage = "El campo descripcion no puede exceder 500 caracteres.")]
        public string? Descripcion { get; set; }

        [MaxLength(100, ErrorMessage = "El campo idSqlite no puede exceder 100 caracteres.")]
        public string? IdSqlite { get; set; }

        [MaxLength(10, ErrorMessage = "El campo sucursal no puede exceder 10 caracteres.")]
        public string? Sucursal { get; set; }

        [MaxLength(20, ErrorMessage = "El campo empleado no puede exceder 20 caracteres.")]
        public string? Empleado { get; set; }

        [MaxLength(30, ErrorMessage = "El campo fechaEntro no puede exceder 30 caracteres.")]
        public string? FechaEntro { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var hasIdTabla = IdTabla.HasValue && IdTabla.Value > 0;
            var hasLegacyFallback =
                !string.IsNullOrWhiteSpace(IdSqlite) &&
                !string.IsNullOrWhiteSpace(Sucursal) &&
                !string.IsNullOrWhiteSpace(Empleado) &&
                !string.IsNullOrWhiteSpace(FechaEntro);

            if (!hasIdTabla && !hasLegacyFallback)
            {
                yield return new ValidationResult(
                    "Debe enviar idTabla o identificadores legacy (idSqlite, sucursal, empleado, fechaEntro).",
                    new[] { nameof(IdTabla), nameof(IdSqlite), nameof(Sucursal), nameof(Empleado), nameof(FechaEntro) });
            }
        }
    }
}
