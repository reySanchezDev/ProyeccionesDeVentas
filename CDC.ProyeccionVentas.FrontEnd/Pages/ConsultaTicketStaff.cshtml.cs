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
    public class ConsultaTicketStaffModel : PageModel
    {
        private readonly ITicketStaffHttpClient _ticketStaffHttpClient;

        public ConsultaTicketStaffModel(ITicketStaffHttpClient ticketStaffHttpClient)
        {
            _ticketStaffHttpClient = ticketStaffHttpClient;
        }

        [BindProperty(SupportsGet = true)]
        public TicketStaffConsultaFilter Filtro { get; set; } = new();

        public List<TicketStaffItem> Resultados { get; set; } = new();
        public bool MostrarMensajeSinResultados { get; set; }
        public string? MensajeError { get; set; }
        public int AnioActual => DateTime.Today.Year;
        public IReadOnlyList<int> AniosEliminar => Enumerable.Range(AnioActual - 2, 5).ToList();
        public IReadOnlyList<(int Valor, string Nombre)> MesesEliminar => Enumerable.Range(1, 12)
            .Select(m => (m, CultureInfo.GetCultureInfo("es-NI").DateTimeFormat.GetMonthName(m)))
            .ToList();

        public void OnGet()
        {
            EnsureDefaultFilter();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            EnsureDefaultFilter();

            if (Filtro.FechaInicio == default || Filtro.FechaFin == default)
            {
                MensajeError = "Debe indicar un rango de fechas válido.";
                return Page();
            }

            if (Filtro.FechaInicio.Date > Filtro.FechaFin.Date)
            {
                MensajeError = "Fecha Inicio no puede ser mayor que Fecha Fin.";
                return Page();
            }

            try
            {
                Resultados = await _ticketStaffHttpClient.ConsultarAsync(Filtro);
                MostrarMensajeSinResultados = Resultados.Count == 0;
            }
            catch (Exception ex)
            {
                MensajeError = $"Ocurrió un error al consultar Ticket Promedio Staff: {ex.Message}";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostGuardarAsync([FromBody] TicketStaffSaveRequest request)
        {
            if (request is null)
            {
                return BadRequest("No se recibió información para guardar.");
            }

            if (request.Id <= 0)
            {
                return BadRequest("Id es obligatorio.");
            }

            request.CodigoEmpleadoAccion = ResolveLoggedEmployeeCode();
            if (string.IsNullOrWhiteSpace(request.CodigoEmpleadoAccion))
            {
                return BadRequest("No se encontró el código del usuario logeado.");
            }

            request.TicketPromedio = request.TicketPromedio < 0 ? 0 : request.TicketPromedio;

            try
            {
                var resultado = await _ticketStaffHttpClient.GuardarAsync(request);
                return new JsonResult(resultado);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error al guardar ticket promedio: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnPostEliminarAsync([FromBody] TicketStaffDeleteMonthRequest request)
        {
            if (request is null)
            {
                return BadRequest("No se recibió información para eliminar.");
            }

            if (request.FechaInicio == default || request.FechaFin == default)
            {
                return BadRequest("Debe seleccionar un mes y un año válidos.");
            }

            if (request.FechaInicio.Date > request.FechaFin.Date)
            {
                return BadRequest("El rango de fechas a eliminar no es válido.");
            }

            try
            {
                var resultado = await _ticketStaffHttpClient.EliminarMesAsync(request);
                return new JsonResult(resultado);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error al eliminar registros: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        private void EnsureDefaultFilter()
        {
            if (Filtro.FechaInicio != default && Filtro.FechaFin != default)
            {
                return;
            }

            var today = DateTime.Today;
            Filtro = new TicketStaffConsultaFilter
            {
                FechaInicio = new DateTime(today.Year, today.Month, 1),
                FechaFin = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)),
                NumeroEmpleado = Filtro.NumeroEmpleado
            };
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
