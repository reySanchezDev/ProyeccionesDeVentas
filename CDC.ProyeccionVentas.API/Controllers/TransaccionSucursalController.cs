using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CDC.ProyeccionVentas.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransaccionSucursalController : ControllerBase
    {
        private readonly ITransaccionSucursalService _transaccionSucursalService;

        public TransaccionSucursalController(ITransaccionSucursalService transaccionSucursalService)
        {
            _transaccionSucursalService = transaccionSucursalService;
        }

        [HttpPost("descargar-plantilla")]
        public async Task<ActionResult<List<TransaccionSucursalDownloadItem>>> DescargarPlantilla([FromBody] List<string>? codSucursales)
        {
            var resultado = await _transaccionSucursalService.DescargarPlantillaAsync(codSucursales ?? new List<string>());
            return Ok(resultado);
        }

        [HttpPost("validar-carga")]
        public async Task<ActionResult<List<TransaccionSucursalExistingItem>>> ValidarCarga([FromBody] List<TransaccionSucursalBulkUploadItem> items)
        {
            if (items is null || items.Count == 0)
            {
                return BadRequest("No se recibieron filas para validar.");
            }

            try
            {
                var resultado = await _transaccionSucursalService.FiltrarExistentesCargaAsync(items);
                return Ok(resultado);
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("insertar-masivo")]
        public async Task<ActionResult<TransaccionSucursalBulkUploadResult>> InsertarMasivo([FromBody] TransaccionSucursalBulkUploadRequest request)
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
                var resultado = await _transaccionSucursalService.InsertarMasivoAsync(request);
                return Ok(resultado);
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("consultar")]
        public async Task<ActionResult<List<TransaccionSucursalItem>>> Consultar([FromBody] TransaccionSucursalConsultaFilter filter)
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
                var resultado = await _transaccionSucursalService.ConsultarAsync(filter);
                return Ok(resultado);
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("guardar")]
        public async Task<ActionResult<TransaccionSucursalSaveResponse>> Guardar([FromBody] TransaccionSucursalSaveRequest request)
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

            if (request.TransaccionProyectada < 0)
            {
                return BadRequest("TransaccionProyectada debe ser mayor o igual a cero.");
            }

            try
            {
                var resultado = await _transaccionSucursalService.ActualizarAsync(request);
                return Ok(resultado);
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("eliminar-mes")]
        public async Task<ActionResult<TransaccionSucursalDeleteMonthResult>> EliminarMes([FromBody] TransaccionSucursalDeleteMonthRequest request)
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
                var resultado = await _transaccionSucursalService.EliminarMesAsync(request);
                return Ok(resultado);
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
