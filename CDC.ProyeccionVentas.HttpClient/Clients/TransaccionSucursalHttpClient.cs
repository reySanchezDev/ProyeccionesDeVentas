using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.HttpClients.Interfaces;
using System.Net.Http.Json;

namespace CDC.ProyeccionVentas.HttpClients.Clients
{
    public class TransaccionSucursalHttpClient : ITransaccionSucursalHttpClient
    {
        private readonly HttpClient _httpClient;

        public TransaccionSucursalHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<TransaccionSucursalDownloadItem>> DescargarPlantillaAsync(List<string> codSucursales)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/transaccionsucursal/descargar-plantilla", codSucursales ?? new List<string>());
            await EnsureSuccessAsync(response, "descargar la plantilla de Transacciones por Sucursal");

            var resultado = await response.Content.ReadFromJsonAsync<List<TransaccionSucursalDownloadItem>>();
            return resultado ?? new List<TransaccionSucursalDownloadItem>();
        }

        public async Task<List<TransaccionSucursalExistingItem>> FiltrarExistentesCargaAsync(List<TransaccionSucursalBulkUploadItem> items)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/transaccionsucursal/validar-carga", items);
            await EnsureSuccessAsync(response, "validar la carga de transacciones por sucursal");

            var resultado = await response.Content.ReadFromJsonAsync<List<TransaccionSucursalExistingItem>>();
            return resultado ?? new List<TransaccionSucursalExistingItem>();
        }

        public async Task<TransaccionSucursalBulkUploadResult> InsertarMasivoAsync(TransaccionSucursalBulkUploadRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/transaccionsucursal/insertar-masivo", request);
            await EnsureSuccessAsync(response, "procesar la carga masiva de transacciones por sucursal");

            var resultado = await response.Content.ReadFromJsonAsync<TransaccionSucursalBulkUploadResult>();
            return resultado ?? new TransaccionSucursalBulkUploadResult
            {
                Mensaje = "La API no devolvió un resultado de carga masiva."
            };
        }

        public async Task<List<TransaccionSucursalItem>> ConsultarAsync(TransaccionSucursalConsultaFilter filter)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/transaccionsucursal/consultar", filter);
            await EnsureSuccessAsync(response, "consultar las transacciones proyectadas por sucursal");

            var resultado = await response.Content.ReadFromJsonAsync<List<TransaccionSucursalItem>>();
            return resultado ?? new List<TransaccionSucursalItem>();
        }

        public async Task<TransaccionSucursalSaveResponse> GuardarAsync(TransaccionSucursalSaveRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/transaccionsucursal/guardar", request);
            await EnsureSuccessAsync(response, "guardar Transacciones por Sucursal");

            var resultado = await response.Content.ReadFromJsonAsync<TransaccionSucursalSaveResponse>();
            return resultado ?? new TransaccionSucursalSaveResponse
            {
                Accion = "NOCHANGE",
                Id = request.Id,
                TransaccionProyectada = request.TransaccionProyectada,
                Mensaje = "La API no devolvió un resultado de guardado."
            };
        }

        public async Task<TransaccionSucursalDeleteMonthResult> EliminarMesAsync(TransaccionSucursalDeleteMonthRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/transaccionsucursal/eliminar-mes", request);
            await EnsureSuccessAsync(response, "eliminar las transacciones proyectadas del mes por sucursal");

            var resultado = await response.Content.ReadFromJsonAsync<TransaccionSucursalDeleteMonthResult>();
            return resultado ?? new TransaccionSucursalDeleteMonthResult
            {
                RegistrosEliminados = 0,
                Mensaje = "La API no devolvió un resultado de eliminación."
            };
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string action)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                throw new InvalidOperationException($"La API devolvió HTTP {(int)response.StatusCode} al {action}.");
            }

            throw new InvalidOperationException(body);
        }
    }
}
