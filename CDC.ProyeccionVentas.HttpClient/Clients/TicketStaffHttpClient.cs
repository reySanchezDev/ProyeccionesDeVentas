using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.HttpClients.Interfaces;
using System.Net.Http.Json;

namespace CDC.ProyeccionVentas.HttpClients.Clients
{
    public class TicketStaffHttpClient : ITicketStaffHttpClient
    {
        private readonly HttpClient _httpClient;

        public TicketStaffHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<TicketStaffDownloadItem>> DescargarStaffBaseAsync(string? numeroSupervisor)
        {
            var query = string.IsNullOrWhiteSpace(numeroSupervisor)
                ? string.Empty
                : $"?numeroSupervisor={Uri.EscapeDataString(numeroSupervisor.Trim())}";

            var response = await _httpClient.GetAsync($"/api/ticketstaff/descargar-staff{query}");
            await EnsureSuccessAsync(response, "descargar el staff base");

            var resultado = await response.Content.ReadFromJsonAsync<List<TicketStaffDownloadItem>>();
            return resultado ?? new List<TicketStaffDownloadItem>();
        }

        public async Task<List<TicketStaffExistingItem>> FiltrarExistentesCargaAsync(List<TicketStaffBulkUploadItem> items)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/ticketstaff/validar-carga", items);
            await EnsureSuccessAsync(response, "validar la carga");

            var resultado = await response.Content.ReadFromJsonAsync<List<TicketStaffExistingItem>>();
            return resultado ?? new List<TicketStaffExistingItem>();
        }

        public async Task<TicketStaffBulkUploadResult> InsertarMasivoAsync(TicketStaffBulkUploadRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/ticketstaff/insertar-masivo", request);
            await EnsureSuccessAsync(response, "procesar la carga masiva");

            var resultado = await response.Content.ReadFromJsonAsync<TicketStaffBulkUploadResult>();
            return resultado ?? new TicketStaffBulkUploadResult
            {
                Mensaje = "La API no devolvió un resultado de carga masiva."
            };
        }

        public async Task<List<TicketStaffItem>> ConsultarAsync(TicketStaffConsultaFilter filter)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/ticketstaff/consultar", filter);
            await EnsureSuccessAsync(response, "consultar los tickets promedio por staff");

            var resultado = await response.Content.ReadFromJsonAsync<List<TicketStaffItem>>();
            return resultado ?? new List<TicketStaffItem>();
        }

        public async Task<TicketStaffSaveResponse> GuardarAsync(TicketStaffSaveRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/ticketstaff/guardar", request);
            await EnsureSuccessAsync(response, "guardar Ticket Staff");

            var resultado = await response.Content.ReadFromJsonAsync<TicketStaffSaveResponse>();
            return resultado ?? new TicketStaffSaveResponse
            {
                Accion = "NOCHANGE",
                Id = request.Id,
                TicketPromedio = request.TicketPromedio,
                Mensaje = "La API no devolvió un resultado de guardado."
            };
        }

        public async Task<TicketStaffDeleteMonthResult> EliminarMesAsync(TicketStaffDeleteMonthRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/ticketstaff/eliminar-mes", request);
            await EnsureSuccessAsync(response, "eliminar los tickets promedio del mes");

            var resultado = await response.Content.ReadFromJsonAsync<TicketStaffDeleteMonthResult>();
            return resultado ?? new TicketStaffDeleteMonthResult
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
