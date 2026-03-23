using System.Globalization;
using HorasExtrasCdC.Frontend.Models;
using HorasExtrasCdC.Frontend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace HorasExtrasCdC.Frontend.Pages;

[Authorize(Policy = "GhOnly")]
public class ListadoGHModel : PageModel
{
    private readonly IHorasExtrasApiClient _apiClient;

    public ListadoGHModel(IHorasExtrasApiClient apiClient)
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

    public IReadOnlyList<HorasExtraConsolidadaItemResponse> Consolidadas { get; private set; } =
        Array.Empty<HorasExtraConsolidadaItemResponse>();

    public string? InfoMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            SetDefaultDateRangeIfEmpty();
            await CargarConsolidadasAsync(cancellationToken);
            return Page();
        }
        catch (ApiConnectivityException ex)
        {
            return RedirectToConnectivityPage(ex, "ListadoGH.OnGet");
        }
    }

    public async Task<IActionResult> OnPostBuscarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await CargarConsolidadasAsync(cancellationToken);
            return Page();
        }
        catch (ApiConnectivityException ex)
        {
            return RedirectToConnectivityPage(ex, "ListadoGH.OnPostBuscar");
        }
    }

    public async Task<IActionResult> OnPostLimpiarAsync(CancellationToken cancellationToken)
    {
        try
        {
            ModelState.Clear();
            EmpleadoFiltro = string.Empty;
            SetDefaultDateRange();
            await CargarConsolidadasAsync(cancellationToken);
            return Page();
        }
        catch (ApiConnectivityException ex)
        {
            return RedirectToConnectivityPage(ex, "ListadoGH.OnPostLimpiar");
        }
    }

    public async Task<IActionResult> OnGetExportConsolidadoDataAsync(
        [FromQuery] string? empleadoFiltro,
        [FromQuery] string? fechaEntradaInicio,
        [FromQuery] string? fechaEntradaFin,
        [FromQuery] string? fechaEntradaRango,
        CancellationToken cancellationToken)
    {
        try
        {
            var filtros = ObtenerFiltrosExportacion(empleadoFiltro, fechaEntradaInicio, fechaEntradaFin, fechaEntradaRango);
            var datos = await _apiClient.ListarConsolidadasAsync(
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
                    idEmpleado = filtros.IdEmpleado,
                    fechaI = filtros.FechaI,
                    fechaF = filtros.FechaF
                }
            });
        }
        catch (ApiConnectivityException ex)
        {
            return BuildConnectivityErrorResponse(ex, "ListadoGH.OnGetExportConsolidadoData");
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
        try
        {
            var filtros = ObtenerFiltrosExportacion(empleadoFiltro, fechaEntradaInicio, fechaEntradaFin, fechaEntradaRango);
            var datos = await _apiClient.ListarReporteMarcadasAsync(
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
                    idEmpleado = filtros.IdEmpleado,
                    fechaI = filtros.FechaI,
                    fechaF = filtros.FechaF
                }
            });
        }
        catch (ApiConnectivityException ex)
        {
            return BuildConnectivityErrorResponse(ex, "ListadoGH.OnGetExportMarcadasData");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { codigo = 500, mensaje = ex.Message });
        }
    }

    private async Task CargarConsolidadasAsync(CancellationToken cancellationToken)
    {
        try
        {
            var filtros = ObtenerFiltrosExportacion();
            Consolidadas = await _apiClient.ListarConsolidadasAsync(
                filtros.IdEmpleado,
                filtros.FechaI,
                filtros.FechaF,
                cancellationToken);

            InfoMessage = $"Registros consolidados: {Consolidadas.Count}.";
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

    private (string? IdEmpleado, string? FechaI, string? FechaF) ObtenerFiltrosExportacion(
        string? empleadoFiltro = null,
        string? fechaEntradaInicio = null,
        string? fechaEntradaFin = null,
        string? fechaEntradaRango = null)
    {
        SetDefaultDateRangeIfEmpty();

        var idEmpleado = (empleadoFiltro ?? EmpleadoFiltro ?? string.Empty).Trim();
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
                fuente = "gh-page",
                codigo = StatusCodes.Status503ServiceUnavailable
            })!;
    }
}
