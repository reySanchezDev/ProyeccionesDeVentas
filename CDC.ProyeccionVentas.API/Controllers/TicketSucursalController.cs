using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CDC.ProyeccionVentas.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketSucursalController : ControllerBase
    {
        private readonly ITicketSucursalService _ticketSucursalService;

        public TicketSucursalController(ITicketSucursalService ticketSucursalService)
        {
            _ticketSucursalService = ticketSucursalService;
        }

        [HttpPost("descargar-plantilla")]
        public async Task<ActionResult<List<TicketSucursalDownloadItem>>> DescargarPlantilla([FromBody] List<string>? codSucursales)
        {
            var resultado = await _ticketSucursalService.DescargarPlantillaAsync(codSucursales ?? new List<string>());
            return Ok(resultado);
        }

        [HttpPost("validar-carga")]
        public async Task<ActionResult<List<TicketSucursalExistingItem>>> ValidarCarga([FromBody] List<TicketSucursalBulkUploadItem> items)
        {
            if (items is null || items.Count == 0)
            {
                return BadRequest("No se recibieron filas para validar.");
            }

            try
            {
                var resultado = await _ticketSucursalService.FiltrarExistentesCargaAsync(items);
                return Ok(resultado);
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("insertar-masivo")]
        public async Task<ActionResult<TicketSucursalBulkUploadResult>> InsertarMasivo([FromBody] TicketSucursalBulkUploadRequest request)
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
                var resultado = await _ticketSucursalService.InsertarMasivoAsync(request);
                return Ok(resultado);
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("consultar")]
        public async Task<ActionResult<List<TicketSucursalItem>>> Consultar([FromBody] TicketSucursalConsultaFilter filter)
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
                var resultado = await _ticketSucursalService.ConsultarAsync(filter);
                return Ok(resultado);
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("guardar")]
        public async Task<ActionResult<TicketSucursalSaveResponse>> Guardar([FromBody] TicketSucursalSaveRequest request)
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
                var resultado = await _ticketSucursalService.ActualizarAsync(request);
                return Ok(resultado);
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("eliminar-mes")]
        public async Task<ActionResult<TicketSucursalDeleteMonthResult>> EliminarMes([FromBody] TicketSucursalDeleteMonthRequest request)
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
                var resultado = await _ticketSucursalService.EliminarMesAsync(request);
                return Ok(resultado);
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
