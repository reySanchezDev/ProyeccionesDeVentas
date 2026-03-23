using HorasExtrasCdC.Frontend.Models;
using HorasExtrasCdC.Frontend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace HorasExtrasCdC.Frontend.Pages;

[Authorize(Policy = "SupervisorOnly")]
public class SuplentesModel : PageModel
{
    private readonly IHorasExtrasApiClient _apiClient;

    public SuplentesModel(IHorasExtrasApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [BindProperty]
    public string? EmpleadoSuplenteSeleccionado { get; set; }

    [BindProperty]
    public string? PeriodoInicia { get; set; }

    [BindProperty]
    public string? PeriodoFinaliza { get; set; }

    [BindProperty]
    public bool AnularSuplente { get; set; }

    public IReadOnlyList<HorasExtraEmpleadoSuplenteItemResponse> EmpleadosSuplentes { get; private set; } =
        Array.Empty<HorasExtraEmpleadoSuplenteItemResponse>();

    [TempData]
    public string? FlashInfoMessage { get; set; }

    public string? InfoMessage { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int CantidadSub { get; private set; }
    public bool PuedeGestionarSuplentes => CantidadSub > 0;

    public string IdSupervisorAutenticado => User.FindFirst("numeroEmpleado")?.Value ?? string.Empty;

    public string? CarnetSuplenteActual =>
        EmpleadosSuplentes.FirstOrDefault(x => x.Selected == 1)?.Carnet;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        InfoMessage = FlashInfoMessage;
        try
        {
            if (!await ResolverPermisosAsync(cancellationToken))
            {
                return Page();
            }
            await CargarEmpleadosAsync(cancellationToken);
            return Page();
        }
        catch (ApiConnectivityException ex)
        {
            return RedirectToConnectivityPage(ex, "Suplentes.OnGet");
        }
    }

    public async Task<IActionResult> OnPostGuardarAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(IdSupervisorAutenticado))
        {
            ErrorMessage = "No se encontro el numero de supervisor en la sesion.";
            await CargarEmpleadosAsync(cancellationToken);
            return Page();
        }

        if (!await ResolverPermisosAsync(cancellationToken))
        {
            return Page();
        }

        if (AnularSuplente)
        {
            return await EjecutarDeBajaDesdePostAsync(cancellationToken);
        }

        var empleado = (EmpleadoSuplenteSeleccionado ?? string.Empty).Trim();
        if (empleado.Length == 0)
        {
            ErrorMessage = "Debe seleccionar un colaborador suplente.";
            await CargarEmpleadosAsync(cancellationToken);
            return Page();
        }

        if (!TryNormalizeDate(PeriodoInicia, out var inicioNormalizado, out var fechaInicio))
        {
            ErrorMessage = "Debe ingresar una fecha de inicio valida.";
            await CargarEmpleadosAsync(cancellationToken);
            return Page();
        }

        string? finNormalizado = null;
        if (!string.IsNullOrWhiteSpace(PeriodoFinaliza))
        {
            if (!TryNormalizeDate(PeriodoFinaliza, out var finParsed, out var fechaFin))
            {
                ErrorMessage = "La fecha final no tiene un formato valido.";
                await CargarEmpleadosAsync(cancellationToken);
                return Page();
            }

            if (fechaFin.Date < fechaInicio.Date)
            {
                ErrorMessage = "La fecha final debe ser mayor o igual a la fecha de inicio.";
                await CargarEmpleadosAsync(cancellationToken);
                return Page();
            }

            finNormalizado = finParsed;
        }

        try
        {
            var request = new HorasExtraSuplenteGuardarRequest
            {
                EmpleadoSupervisor = IdSupervisorAutenticado.Trim(),
                EmpleadoSuplente = empleado,
                PeriodoInicia = inicioNormalizado,
                PeriodoFinaliza = finNormalizado
            };

            var response = await _apiClient.GuardarSuplenteAsync(request, cancellationToken);
            if (response.Codigo == 0)
            {
                FlashInfoMessage = "Suplente guardado correctamente.";
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = string.IsNullOrWhiteSpace(response.Mensaje)
                    ? "No se pudo guardar el suplente."
                    : response.Mensaje;
            }
        }
        catch (ApiConnectivityException ex)
        {
            return RedirectToConnectivityPage(ex, "Suplentes.OnPostGuardar");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        await CargarEmpleadosAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDeBajaAsync(CancellationToken cancellationToken)
    {
        return await EjecutarDeBajaDesdePostAsync(cancellationToken);
    }

    private async Task<IActionResult> EjecutarDeBajaDesdePostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(IdSupervisorAutenticado))
        {
            ErrorMessage = "No se encontro el numero de supervisor en la sesion.";
            await CargarEmpleadosAsync(cancellationToken);
            return Page();
        }

        if (!await ResolverPermisosAsync(cancellationToken))
        {
            return Page();
        }

        try
        {
            var request = new HorasExtraSuplenteDeBajaRequest
            {
                IdSupervisor = IdSupervisorAutenticado.Trim()
            };

            var response = await _apiClient.DeBajaSuplenteAsync(request, cancellationToken);
            if (response.Codigo == 0)
            {
                FlashInfoMessage = response.Afectados > 0
                    ? "Suplente anulado correctamente."
                    : "No habia un suplente activo para anular.";
                return RedirectToPage();
            }
            else
            {
                ErrorMessage = string.IsNullOrWhiteSpace(response.Mensaje)
                    ? "No se pudo anular el suplente."
                    : response.Mensaje;
            }
        }
        catch (ApiConnectivityException ex)
        {
            return RedirectToConnectivityPage(ex, "Suplentes.OnPostDeBaja");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        await CargarEmpleadosAsync(cancellationToken);
        return Page();
    }

    private async Task<bool> ResolverPermisosAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(IdSupervisorAutenticado))
        {
            ErrorMessage = "No se encontro el numero de supervisor en la sesion.";
            CantidadSub = 0;
            return false;
        }

        try
        {
            var cantidadSub = await _apiClient.ObtenerCantidadSubAsync(IdSupervisorAutenticado, cancellationToken);
            if (cantidadSub.Codigo != 0)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(cantidadSub.Mensaje)
                    ? "No se pudo validar permisos de suplentes."
                    : cantidadSub.Mensaje;
                CantidadSub = 0;
                return false;
            }

            CantidadSub = Math.Max(0, cantidadSub.CantidadSub);
            if (!PuedeGestionarSuplentes)
            {
                ErrorMessage = "No esta autorizado para gestionar suplentes.";
                return false;
            }

            return true;
        }
        catch (ApiConnectivityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            CantidadSub = 0;
            return false;
        }
    }

    private async Task CargarEmpleadosAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(IdSupervisorAutenticado))
        {
            ErrorMessage = "No se encontro el numero de supervisor en la sesion.";
            return;
        }

        try
        {
            var listado = await _apiClient.ListarEmpleadosSuplentesAsync(IdSupervisorAutenticado, cancellationToken);
            EmpleadosSuplentes = listado;

            if (string.IsNullOrWhiteSpace(PeriodoInicia))
            {
                PeriodoInicia = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            if (string.IsNullOrWhiteSpace(EmpleadoSuplenteSeleccionado))
            {
                EmpleadoSuplenteSeleccionado = CarnetSuplenteActual ?? string.Empty;
            }
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

    private static bool TryNormalizeDate(string? value, out string normalized, out DateTime parsed)
    {
        normalized = string.Empty;
        parsed = default;

        var raw = (value ?? string.Empty).Trim();
        if (raw.Length == 0)
        {
            return false;
        }

        var formats = new[]
        {
            "yyyy-MM-dd",
            "MM/dd/yyyy",
            "M/d/yyyy",
            "dd/MM/yyyy",
            "d/M/yyyy"
        };

        if (!DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            return false;
        }

        normalized = parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return true;
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
                fuente = "suplentes-page",
                codigo = StatusCodes.Status503ServiceUnavailable
            })!;
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
}
