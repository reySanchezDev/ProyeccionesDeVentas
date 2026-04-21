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
    public class ConsultaTransaccionesModel : PageModel
    {
        private readonly ITransaccionSucursalHttpClient _transaccionSucursalHttpClient;
        private readonly IStoresHttpClient _storesHttpClient;

        public ConsultaTransaccionesModel(
            ITransaccionSucursalHttpClient transaccionSucursalHttpClient,
            IStoresHttpClient storesHttpClient)
        {
            _transaccionSucursalHttpClient = transaccionSucursalHttpClient;
            _storesHttpClient = storesHttpClient;
        }

        [BindProperty(SupportsGet = true)]
        public TransaccionSucursalConsultaFilter Filtro { get; set; } = new();

        public List<TransaccionSucursalItem> Resultados { get; set; } = new();
        public List<Store> StoresDisponibles { get; set; } = new();
        public bool MostrarMensajeSinResultados { get; set; }
        public string? MensajeError { get; set; }
        public int AnioActual => DateTime.Today.Year;
        public IReadOnlyList<int> AniosDisponibles => Enumerable.Range(AnioActual - 2, 5).ToList();
        public IReadOnlyList<int> AniosEliminar => AniosDisponibles;
        public IReadOnlyList<(int Valor, string Nombre)> MesesDisponibles => Enumerable.Range(1, 12)
            .Select(m => (m, CultureInfo.GetCultureInfo("es-NI").DateTimeFormat.GetMonthName(m)))
            .ToList();
        public IReadOnlyList<(int Valor, string Nombre)> MesesEliminar => MesesDisponibles;

        public string ResumenSucursalesSeleccionadas
        {
            get
            {
                var seleccionadas = (Filtro.CodSucursales ?? new List<string>())
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (!seleccionadas.Any())
                {
                    return "Todas las sucursales";
                }

                if (seleccionadas.Count <= 2)
                {
                    return string.Join(", ", seleccionadas);
                }

                return $"{seleccionadas.Count} sucursales seleccionadas";
            }
        }

        public async Task OnGetAsync()
        {
            EnsureDefaultFilter();
            await CargarStoresAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            EnsureDefaultFilter();
            await CargarStoresAsync();

            if (Filtro.Mes < 1 || Filtro.Mes > 12 || Filtro.Ano < 2000)
            {
                MensajeError = "Debe seleccionar un mes y un año válidos.";
                return Page();
            }

            try
            {
                Resultados = await _transaccionSucursalHttpClient.ConsultarAsync(Filtro);
                MostrarMensajeSinResultados = Resultados.Count == 0;
            }
            catch (Exception ex)
            {
                MensajeError = $"Ocurrió un error al consultar Transacciones por Sucursal: {ex.Message}";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostGuardarAsync([FromBody] TransaccionSucursalSaveRequest request)
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

            request.TransaccionProyectada = request.TransaccionProyectada < 0 ? 0 : request.TransaccionProyectada;

            try
            {
                var resultado = await _transaccionSucursalHttpClient.GuardarAsync(request);
                return new JsonResult(resultado);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error al guardar transacción proyectada: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnPostEliminarAsync([FromBody] TransaccionSucursalDeleteMonthRequest request)
        {
            if (request is null)
            {
                return BadRequest("No se recibió información para eliminar.");
            }

            if (request.Mes < 1 || request.Mes > 12 || request.Ano < 2000)
            {
                return BadRequest("Debe seleccionar un mes y un año válidos.");
            }

            try
            {
                var resultado = await _transaccionSucursalHttpClient.EliminarMesAsync(request);
                return new JsonResult(resultado);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error al eliminar registros: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        private async Task CargarStoresAsync()
        {
            StoresDisponibles = (await _storesHttpClient.ObtenerStoresAsync())
                .Where(s =>
                    !string.IsNullOrWhiteSpace(s.No) &&
                    (s.No.StartsWith("SK", StringComparison.OrdinalIgnoreCase) ||
                     s.No.StartsWith("SR", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(s => s.No)
                .ThenBy(s => s.StoreNo)
                .ToList();

            Filtro.CodSucursales = (Filtro.CodSucursales ?? new List<string>())
                .Where(c =>
                    !string.IsNullOrWhiteSpace(c) &&
                    (c.StartsWith("SK", StringComparison.OrdinalIgnoreCase) ||
                     c.StartsWith("SR", StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void EnsureDefaultFilter()
        {
            if (Filtro.Mes >= 1 && Filtro.Mes <= 12 && Filtro.Ano >= 2000)
            {
                return;
            }

            var today = DateTime.Today;
            Filtro = new TransaccionSucursalConsultaFilter
            {
                Mes = today.Month,
                Ano = today.Year,
                CodSucursales = Filtro.CodSucursales
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
