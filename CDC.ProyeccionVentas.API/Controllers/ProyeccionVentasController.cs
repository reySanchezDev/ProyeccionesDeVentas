using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CDC.ProyeccionVentas.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProyeccionVentasController : ControllerBase
    {
        private readonly IProyeccionVentasRepository _repository;

        public ProyeccionVentasController(IProyeccionVentasRepository repository)
        {
            _repository = repository;
        }

        [HttpPost("insertar-masivo")]
        public async Task<IActionResult> InsertarProyecciones([FromBody] List<ProyeccionVentaDto> proyecciones)
        {
            if (proyecciones == null || !proyecciones.Any())
            {
                return BadRequest(new { success = false, message = "No se recibieron datos para insertar." });
            }

            try
            {
                await _repository.InsertarProyeccionesAsync(proyecciones);
                return Ok(new { success = true, message = "Datos guardados correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error al guardar: " + ex.Message });
            }
        }
    }
}
