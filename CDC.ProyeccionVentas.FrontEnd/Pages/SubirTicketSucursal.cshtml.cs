using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.HttpClients.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Globalization;
using System.Security.Claims;

namespace CDC.ProyeccionVentas.FrontEnd.Pages
{
    [Authorize]
    public class SubirTicketSucursalModel : PageModel
    {
        private readonly ITicketSucursalHttpClient _ticketSucursalHttpClient;
        private readonly IStoresHttpClient _storesHttpClient;

        public SubirTicketSucursalModel(
            ITicketSucursalHttpClient ticketSucursalHttpClient,
            IStoresHttpClient storesHttpClient)
        {
            _ticketSucursalHttpClient = ticketSucursalHttpClient;
            _storesHttpClient = storesHttpClient;
        }

        public string CurrentMonthLabel { get; private set; } = string.Empty;
        public List<Store> StoresDisponibles { get; private set; } = new();
        public string? CatalogoError { get; private set; }
        public string StoreCodesDisponiblesJson { get; private set; } = "[]";
        public string StoreNameMapJson { get; private set; } = "{}";

        public async Task OnGetAsync()
        {
            InitializeCurrentMonth();

            try
            {
                StoresDisponibles = (await _storesHttpClient.ObtenerStoresAsync())
                    .Where(s =>
                        !string.IsNullOrWhiteSpace(s.No) &&
                        (s.No.StartsWith("SK", StringComparison.OrdinalIgnoreCase) ||
                         s.No.StartsWith("SR", StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(s => s.No)
                    .ThenBy(s => s.StoreNo)
                    .ToList();
            }
            catch (Exception ex)
            {
                CatalogoError = $"No se pudo cargar el catálogo de sucursales: {ex.Message}";
                StoresDisponibles = new List<Store>();
            }

            var storesNormalizadas = StoresDisponibles
                .Where(s => !string.IsNullOrWhiteSpace(s.No))
                .GroupBy(s => s.No.Trim().ToUpperInvariant())
                .Select(g => new
                {
                    CodSucursal = g.Key,
                    NombreSucursal = g.First().StoreNo ?? string.Empty
                })
                .ToList();

            StoreCodesDisponiblesJson = JsonSerializer.Serialize(
                storesNormalizadas.Select(s => s.CodSucursal).ToList());
            StoreNameMapJson = JsonSerializer.Serialize(
                storesNormalizadas.ToDictionary(s => s.CodSucursal, s => s.NombreSucursal));
        }

        public async Task<IActionResult> OnPostDescargarPlantillaAsync([FromBody] List<string>? codSucursales)
        {
            var codigos = (codSucursales ?? new List<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            try
            {
                var resultado = await _ticketSucursalHttpClient.DescargarPlantillaAsync(codigos);
                return new JsonResult(resultado);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error al descargar la plantilla: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnPostValidarExistentesAsync([FromBody] List<TicketSucursalBulkUploadItem> items)
        {
            if (items is null || items.Count == 0)
            {
                return BadRequest("No se recibieron filas para validar.");
            }

            try
            {
                var resultado = await _ticketSucursalHttpClient.FiltrarExistentesCargaAsync(items);
                return new JsonResult(resultado);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error al validar registros existentes: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnPostGuardarAsync([FromBody] TicketSucursalBulkUploadRequest request)
        {
            if (request is null)
            {
                return BadRequest("No se recibió información para guardar.");
            }

            if (request.Items is null || request.Items.Count == 0)
            {
                return BadRequest("No se recibieron filas para guardar.");
            }

            request.CodigoEmpleadoAccion = ResolveLoggedEmployeeCode();
            if (string.IsNullOrWhiteSpace(request.CodigoEmpleadoAccion))
            {
                return BadRequest("No se encontró el código del usuario logeado.");
            }

            foreach (var item in request.Items)
            {
                item.TicketPromedio = item.TicketPromedio < 0 ? 0 : item.TicketPromedio;
                item.CodSucursal = item.CodSucursal?.Trim() ?? string.Empty;
            }

            try
            {
                var resultado = await _ticketSucursalHttpClient.InsertarMasivoAsync(request);
                return new JsonResult(resultado);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error al guardar la carga masiva: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        private void InitializeCurrentMonth()
        {
            var today = DateTime.Today;
            var firstDay = new DateTime(today.Year, today.Month, 1);
            var culture = CultureInfo.GetCultureInfo("es-NI");

            CurrentMonthLabel = culture.TextInfo.ToTitleCase(firstDay.ToString("MMMM yyyy", culture));
        }

        private string ResolveLoggedEmployeeCode()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("NumeroEmpleado")
                ?? User.Identity?.Name
                ?? string.Empty;
        }
    }
}
