using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.HttpClients.Interfaces;
using System.Net.Http.Json;

namespace CDC.ProyeccionVentas.HttpClients.Clients
{
    public class TicketSucursalHttpClient : ITicketSucursalHttpClient
    {
        private readonly HttpClient _httpClient;

        public TicketSucursalHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<TicketSucursalDownloadItem>> DescargarPlantillaAsync(List<string> codSucursales)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/ticketsucursal/descargar-plantilla", codSucursales ?? new List<string>());
            await EnsureSuccessAsync(response, "descargar la plantilla de Ticket por Sucursal");

            var resultado = await response.Content.ReadFromJsonAsync<List<TicketSucursalDownloadItem>>();
            return resultado ?? new List<TicketSucursalDownloadItem>();
        }

        public async Task<List<TicketSucursalExistingItem>> FiltrarExistentesCargaAsync(List<TicketSucursalBulkUploadItem> items)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/ticketsucursal/validar-carga", items);
            await EnsureSuccessAsync(response, "validar la carga por sucursal");

            var resultado = await response.Content.ReadFromJsonAsync<List<TicketSucursalExistingItem>>();
            return resultado ?? new List<TicketSucursalExistingItem>();
        }

        public async Task<TicketSucursalBulkUploadResult> InsertarMasivoAsync(TicketSucursalBulkUploadRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/ticketsucursal/insertar-masivo", request);
            await EnsureSuccessAsync(response, "procesar la carga masiva por sucursal");

            var resultado = await response.Content.ReadFromJsonAsync<TicketSucursalBulkUploadResult>();
            return resultado ?? new TicketSucursalBulkUploadResult
            {
                Mensaje = "La API no devolvió un resultado de carga masiva."
            };
        }

        public async Task<List<TicketSucursalItem>> ConsultarAsync(TicketSucursalConsultaFilter filter)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/ticketsucursal/consultar", filter);
            await EnsureSuccessAsync(response, "consultar los tickets promedio por sucursal");

            var resultado = await response.Content.ReadFromJsonAsync<List<TicketSucursalItem>>();
            return resultado ?? new List<TicketSucursalItem>();
        }

        public async Task<TicketSucursalSaveResponse> GuardarAsync(TicketSucursalSaveRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/ticketsucursal/guardar", request);
            await EnsureSuccessAsync(response, "guardar Ticket por Sucursal");

            var resultado = await response.Content.ReadFromJsonAsync<TicketSucursalSaveResponse>();
            return resultado ?? new TicketSucursalSaveResponse
            {
                Accion = "NOCHANGE",
                Id = request.Id,
                TicketPromedio = request.TicketPromedio,
                Mensaje = "La API no devolvió un resultado de guardado."
            };
        }

        public async Task<TicketSucursalDeleteMonthResult> EliminarMesAsync(TicketSucursalDeleteMonthRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/ticketsucursal/eliminar-mes", request);
            await EnsureSuccessAsync(response, "eliminar los tickets promedio del mes por sucursal");

            var resultado = await response.Content.ReadFromJsonAsync<TicketSucursalDeleteMonthResult>();
            return resultado ?? new TicketSucursalDeleteMonthResult
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
