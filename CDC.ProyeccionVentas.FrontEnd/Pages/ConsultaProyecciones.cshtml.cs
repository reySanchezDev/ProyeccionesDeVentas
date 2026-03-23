using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.HttpClients.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.FrontEnd.Pages
{
    public class ConsultaProyeccionesModel : PageModel
    {
        private readonly IProyeccionVentasConsultaHttpClient _consultaClient;
        private readonly IStoresHttpClient _storesClient;

        public ConsultaProyeccionesModel(IProyeccionVentasConsultaHttpClient consultaClient, IStoresHttpClient storesClient)
        {
            _consultaClient = consultaClient;
            _storesClient = storesClient;
        }

        [BindProperty(SupportsGet = true)]
        public FiltroProyeccionVentas Filtro { get; set; } = new();

        public List<ProyeccionVentasToConsulta> Resultados { get; set; } = new();
        public List<Store> StoresDisponibles { get; set; } = new();

        public string? Mensaje { get; set; }
        public string? MensajeError { get; set; }
        public bool MostrarMensajeSinResultados { get; set; }
        public int AnioActual => DateTime.Today.Year;
        public IReadOnlyList<int> AniosEliminar => Enumerable.Range(AnioActual - 2, 5).ToList();
        public IReadOnlyList<(int Valor, string Nombre)> MesesEliminar => Enumerable.Range(1, 12)
            .Select(m => (m, CultureInfo.GetCultureInfo("es-NI").DateTimeFormat.GetMonthName(m)))
            .ToList();

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

        private async Task CargarStoresAsync()
        {
            StoresDisponibles = (await _storesClient.ObtenerStoresAsync())
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

        public async Task OnGetAsync()
        {
            if (Filtro == null || (Filtro.FechaInicio == default && Filtro.FechaFin == default))
            {
                Filtro = new FiltroProyeccionVentas
                {
                    FechaInicio = DateTime.Today,
                    FechaFin = DateTime.Today
                };
            }

            await CargarStoresAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await CargarStoresAsync();

            if (!ModelState.IsValid)
            {
                MensajeError = "Hay errores en el formulario. Por favor, revise los campos.";
                return Page();
            }

            try
            {
                Resultados = await _consultaClient.ObtenerFiltradoAsync(Filtro);

                if (Resultados == null || !Resultados.Any())
                {
                    MostrarMensajeSinResultados = true;
                }
            }
            catch (Exception ex)
            {
                MensajeError = $"Ocurrió un error al consultar las proyecciones: {ex.Message}";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostGuardarAsync([FromBody] List<ActualizarProyeccionDto> cambios)
        {
            if (cambios == null || !cambios.Any())
                return BadRequest("No hay cambios que guardar.");

            try
            {
                await _consultaClient.GuardarCambiosAsync(cambios);
                return new JsonResult(new { mensaje = "Cambios guardados correctamente." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error al guardar cambios: {ex.Message}" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnPostEliminarAsync([FromBody] EliminarProyeccionVentasRequest request)
        {
            if (request == null)
                return BadRequest("No se recibió información para eliminar.");

            if (request.FechaInicio == default || request.FechaFin == default)
                return BadRequest("Debe seleccionar un mes y un año válidos.");

            if (request.FechaInicio.Date > request.FechaFin.Date)
                return BadRequest("El rango de fechas a eliminar no es válido.");

            try
            {
                var resultado = await _consultaClient.EliminarPorRangoAsync(request);
                return new JsonResult(resultado);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error al eliminar registros: {ex.Message}" }) { StatusCode = 500 };
            }
        }
    }
}
