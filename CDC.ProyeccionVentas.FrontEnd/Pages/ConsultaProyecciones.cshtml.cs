using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.HttpClients.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.FrontEnd.Pages
{
    public class ConsultaProyeccionesModel : PageModel
    {
        private readonly IProyeccionVentasConsultaHttpClient _consultaClient;

        public ConsultaProyeccionesModel(IProyeccionVentasConsultaHttpClient consultaClient)
        {
            _consultaClient = consultaClient;
        }

        [BindProperty(SupportsGet = true)] // Habilitar Binding también para GET si necesitas que los filtros se mantengan en el URL o al navegar
        public FiltroProyeccionVentas Filtro { get; set; } // NO inicializar aquí

        public List<ProyeccionVentasToConsulta> Resultados { get; set; } = new();

        public string? Mensaje { get; set; }
        public string? MensajeError { get; set; }
        public bool MostrarMensajeSinResultados { get; set; } = false;

        public void OnGet()
        {
            // Solo inicializamos si el filtro está nulo (primera carga GET sin parámetros en URL)
            // Si SupportsGet = true y hay parámetros en la URL, Filtro ya estaría poblado.
            if (Filtro == null || (Filtro.FechaInicio == default(DateTime) && Filtro.FechaFin == default(DateTime)))
            {
                Filtro = new FiltroProyeccionVentas
                {
                    FechaInicio = DateTime.Today,
                    FechaFin = DateTime.Today
                };
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Aquí, gracias a [BindProperty], Filtro ya debería contener los valores del formulario.
            // No necesitamos hacer nada extra para rellenarlo con los valores post.

            if (!ModelState.IsValid)
            {
                MensajeError = "Hay errores en el formulario. Por favor, revise los campos.";
                // Si hay errores de validación, return Page() automáticamente mantendrá los valores
                // que se intentaron vincular a Filtro.
                return Page();
            }

            // Si llegamos aquí, Filtro debería tener los valores correctos de la última búsqueda.
            try
            {
                Resultados = await _consultaClient.ObtenerFiltradoAsync(Filtro.FechaInicio, Filtro.FechaFin, Filtro.CodSucursal);

                if (Resultados == null || !Resultados.Any())
                {
                    MostrarMensajeSinResultados = true;
                }
            }
            catch (Exception ex)
            {
                MensajeError = $"Ocurrió un error al consultar las proyecciones: {ex.Message}";
                // En caso de error, también regresamos la página y mantenemos los filtros
            }

            return Page(); // Renderiza la página, usando el estado actual de Filtro
        }

        public async Task<IActionResult> OnPostGuardarAsync([FromBody] List<ActualizarProyeccionDto> cambios)
        {
            if (cambios == null || !cambios.Any())
                return BadRequest("No hay cambios que guardar.");

            try
            {
                await _consultaClient.GuardarCambiosAsync(cambios);
                // Después de guardar, usualmente querrás recargar la página para ver los cambios
                // y refrescar los totales. Redirigir a la misma página manteniendo los filtros
                // es una buena estrategia.
                // return RedirectToPage(new { FechaInicio = Filtro.FechaInicio, FechaFin = Filtro.FechaFin, CodSucursal = Filtro.CodSucursal });
                // Sin embargo, si quieres mantener el comportamiento actual de recarga total del navegador
                // vía JavaScript, entonces el return new JsonResult() es correcto para la llamada AJAX.
                return new JsonResult(new { mensaje = "Cambios guardados correctamente." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error al guardar cambios: {ex.Message}" }) { StatusCode = 500 };
            }
        }
    }
}