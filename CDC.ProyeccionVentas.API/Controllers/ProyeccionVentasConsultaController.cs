using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CDC.ProyeccionVentas.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProyeccionVentasConsultaController : ControllerBase
    {
        private readonly IProyeccionVentasConsultaService _consultaService;

        public ProyeccionVentasConsultaController(IProyeccionVentasConsultaService consultaService)
        {
            _consultaService = consultaService;
        }

        [HttpPost("filtrar")]
        public async Task<ActionResult<List<ProyeccionVentasToConsulta>>> ObtenerFiltrado([FromBody] FiltroProyeccionVentas filtro)
        {
            var resultado = await _consultaService.ObtenerProyeccionesFiltradasAsync(filtro);

            if (resultado == null || resultado.Count == 0)
                return NoContent();

            return Ok(resultado);
        }

        [HttpPost("guardar")]
        public async Task<IActionResult> GuardarCambios([FromBody] List<ActualizarProyeccionDto> cambios)
        {
            if (cambios == null || !cambios.Any())
                return BadRequest("No se recibieron datos para actualizar.");

            try
            {
                await _consultaService.GuardarCambiosAsync(cambios);
                return Ok(new { mensaje = "Cambios guardados correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al guardar los cambios: {ex.Message}");
            }
        }
    }
}