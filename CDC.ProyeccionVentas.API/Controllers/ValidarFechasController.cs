using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CDC.ProyeccionVentas.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ValidarFechasController : ControllerBase
    {
        private readonly IValidarFechasService _service;

        public ValidarFechasController(IValidarFechasService service)
        {
            _service = service;
        }

        [HttpGet("existentes-mes-actual")]
        public async Task<IActionResult> ObtenerExistentes()
        {
            var datos = await _service.ObtenerFechasYSucursalesExistentesAsync();
            return Ok(datos);
        }

        // Nuevo método para validar duplicados desde archivo
        [HttpPost("filtrar-existentes")]
        public async Task<IActionResult> FiltrarExistentes([FromBody] List<ValidarFechaRequest> datosArchivo)
        {
            if (datosArchivo == null || !datosArchivo.Any())
                return BadRequest("La lista está vacía o no se recibió.");

            var duplicados = await _service.FiltrarFechasYaExistentesAsync(datosArchivo);
            return Ok(duplicados);
        }
    }
}