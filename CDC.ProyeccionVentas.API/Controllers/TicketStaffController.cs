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

        [HttpGet("descargar-staff")]
        public async Task<ActionResult<List<TicketStaffDownloadItem>>> DescargarStaff([FromQuery] string? numeroSupervisor)
        {
            var resultado = await _ticketStaffService.DescargarStaffBaseAsync(numeroSupervisor);
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

            if (filter.FechaInicio == default || filter.FechaFin == default)
            {
                return BadRequest("FechaInicio y FechaFin son obligatorias.");
            }

            if (filter.FechaInicio.Date > filter.FechaFin.Date)
            {
                return BadRequest("FechaInicio no puede ser mayor que FechaFin.");
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

            if (request.FechaInicio == default || request.FechaFin == default)
            {
                return BadRequest("FechaInicio y FechaFin son obligatorias.");
            }

            if (request.FechaInicio.Date > request.FechaFin.Date)
            {
                return BadRequest("FechaInicio no puede ser mayor que FechaFin.");
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
