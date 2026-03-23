using CDC.ProyeccionVentas.API.Models;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CDC.ProyeccionVentas.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class CalendarioController : ControllerBase
    {
        private readonly ICalendarioPedidosService _service;

        public CalendarioController(ICalendarioPedidosService service)
        {
            _service = service;
        }

        /// <summary>Devuelve la matriz completa (todas las sucursales y sus 7 días 0/1).</summary>
        /// GET /api/calendario
        [HttpGet]
        public async Task<IActionResult> GetMatriz(CancellationToken ct)
        {
            var data = await _service.ObtenerMatrizAsync(ct);
            return Ok(data);
        }

        /// <summary>Guarda el cambio de un checkbox y devuelve la fila actualizada para esa sucursal.</summary>
        /// POST /api/calendario/dia
        [HttpPost("dia")]
        public async Task<IActionResult> GuardarDia([FromBody] GuardarDiaRequest req, CancellationToken ct)
        {
            if (req is null || string.IsNullOrWhiteSpace(req.StoreNo))
                return BadRequest("StoreNo requerido.");

            if (req.DiaSemanaIso < 1 || req.DiaSemanaIso > 7)
                return BadRequest("DiaSemanaIso debe estar entre 1 y 7.");

            var fila = await _service.GuardarDiaAsync(req.StoreNo, req.DiaSemanaIso, req.Marcado, ct);
            return Ok(fila);
        }
    }
}
