using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.HttpClients.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using System.Security.Claims;

namespace CDC.ProyeccionVentas.FrontEnd.Pages
{
    [Authorize]
    public class SubirTicketStaffModel : PageModel
    {
        private readonly ITicketStaffHttpClient _ticketStaffHttpClient;

        public SubirTicketStaffModel(ITicketStaffHttpClient ticketStaffHttpClient)
        {
            _ticketStaffHttpClient = ticketStaffHttpClient;
        }

        public string CurrentMonthStart { get; private set; } = string.Empty;
        public string CurrentMonthEnd { get; private set; } = string.Empty;
        public string CurrentMonthLabel { get; private set; } = string.Empty;

        public void OnGet()
        {
            InitializeCurrentMonth();
        }

        public async Task<IActionResult> OnGetDescargarStaffAsync(string? numeroSupervisor)
        {
            try
            {
                var resultado = await _ticketStaffHttpClient.DescargarStaffBaseAsync(numeroSupervisor);
                return new JsonResult(resultado);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error al descargar el staff base: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnPostValidarExistentesAsync([FromBody] List<TicketStaffBulkUploadItem> items)
        {
            if (items is null || items.Count == 0)
            {
                return BadRequest("No se recibieron filas para validar.");
            }

            try
            {
                var resultado = await _ticketStaffHttpClient.FiltrarExistentesCargaAsync(items);
                return new JsonResult(resultado);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error al validar registros existentes: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnPostGuardarAsync([FromBody] TicketStaffBulkUploadRequest request)
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
                item.NumeroEmpleado = item.NumeroEmpleado?.Trim() ?? string.Empty;
                item.NombreStaff = item.NombreStaff?.Trim() ?? string.Empty;
            }

            try
            {
                var resultado = await _ticketStaffHttpClient.InsertarMasivoAsync(request);
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
            var lastDay = firstDay.AddMonths(1).AddDays(-1);
            var culture = CultureInfo.GetCultureInfo("es-NI");

            CurrentMonthStart = firstDay.ToString("yyyy-MM-dd");
            CurrentMonthEnd = lastDay.ToString("yyyy-MM-dd");
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
