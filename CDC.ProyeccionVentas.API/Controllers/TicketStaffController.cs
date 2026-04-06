using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CDC.ProyeccionVentas.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketStaffController : ControllerBase
    {
        private readonly ITicketStaffService _ticketStaffService;

        public TicketStaffController(ITicketStaffService ticketStaffService)
        {
            _ticketStaffService = ticketStaffService;
        }

        [HttpGet("catalogo-puestos")]
        public async Task<ActionResult<List<string>>> ObtenerCatalogoPuestos()
        {
            var resultado = await _ticketStaffService.ObtenerCatalogoPuestosAsync();
            return Ok(resultado);
        }

        [HttpPost("descargar-plantilla")]
        public async Task<ActionResult<List<TicketStaffDownloadItem>>> DescargarPlantilla([FromBody] List<string>? puestos)
        {
            var resultado = await _ticketStaffService.DescargarPlantillaAsync(puestos ?? new List<string>());
            return Ok(resultado);
        }

        [HttpPost("validar-carga")]
        public async Task<ActionResult<List<TicketStaffExistingItem>>> ValidarCarga([FromBody] List<TicketStaffBulkUploadItem> items)
        {
            if (items is null || items.Count == 0)
            {
                return BadRequest("No se recibieron filas para validar.");
            }

            try
            {
                var resultado = await _ticketStaffService.FiltrarExistentesCargaAsync(items);
                return Ok(resultado);
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("insertar-masivo")]
        public async Task<ActionResult<TicketStaffBulkUploadResult>> InsertarMasivo([FromBody] TicketStaffBulkUploadRequest request)
        {
            if (request is null)
            {
                return BadRequest("No se recibió información para la carga masiva.");
            }

            if (string.IsNullOrWhiteSpace(request.CodigoEmpleadoAccion))
            {
                return BadRequest("CodigoEmpleadoAccion es obligatorio.");
            }

            if (request.Items is null || request.Items.Count == 0)
            {
                return BadRequest("No se recibieron filas para procesar.");
            }

            try
            {
                var resultado = await _ticketStaffService.InsertarMasivoAsync(request);
                return Ok(resultado);
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("consultar")]
        public async Task<ActionResult<List<TicketStaffItem>>> Consultar([FromBody] TicketStaffConsultaFilter filter)
        {
            if (filter is null)
            {
                return BadRequest("No se recibió información para consultar.");
            }

            if (filter.Mes < 1 || filter.Mes > 12 || filter.Ano < 2000)
            {
                return BadRequest("Mes y Año son obligatorios.");
            }

            try
            {
                var resultado = await _ticketStaffService.ConsultarAsync(filter);
                return Ok(resultado);
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("guardar")]
        public async Task<ActionResult<TicketStaffSaveResponse>> Guardar([FromBody] TicketStaffSaveRequest request)
        {
            if (request is null)
            {
                return BadRequest("No se recibió información para guardar.");
            }

            if (request.Id <= 0)
            {
                return BadRequest("Id es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(request.CodigoEmpleadoAccion))
            {
                return BadRequest("CodigoEmpleadoAccion es obligatorio.");
            }

            if (request.TicketPromedio < 0)
            {
                return BadRequest("TicketPromedio debe ser mayor o igual a cero.");
            }

            try
            {
                var resultado = await _ticketStaffService.ActualizarAsync(request);
                return Ok(resultado);
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("eliminar-mes")]
        public async Task<ActionResult<TicketStaffDeleteMonthResult>> EliminarMes([FromBody] TicketStaffDeleteMonthRequest request)
        {
            if (request is null)
            {
                return BadRequest("No se recibió información para eliminar.");
            }

            if (request.Mes < 1 || request.Mes > 12 || request.Ano < 2000)
            {
                return BadRequest("Mes y Año son obligatorios.");
            }

            try
            {
                var resultado = await _ticketStaffService.EliminarMesAsync(request);
                return Ok(resultado);
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
