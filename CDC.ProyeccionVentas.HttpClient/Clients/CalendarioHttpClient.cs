using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.HttpClients.Interfaces;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.HttpClients.Clients
{
    public class CalendarioHttpClient : ICalendarioHttpClient
    {
        private readonly HttpClient _http;
        public CalendarioHttpClient(HttpClient http) => _http = http;

        public async Task<List<CalendarioMatrizRow>> ObtenerMatrizAsync()
        {
            var r = await _http.GetAsync("/api/Calendario");
            r.EnsureSuccessStatusCode();
            return await r.Content.ReadFromJsonAsync<List<CalendarioMatrizRow>>() ?? new();
        }

        public async Task<CalendarioMatrizRow> GuardarDiaAsync(string storeNo, int diaSemanaIso, bool marcado)
        {
            var payload = new { StoreNo = storeNo, DiaSemanaIso = diaSemanaIso, Marcado = marcado };
            var r = await _http.PostAsJsonAsync("/api/Calendario/dia", payload);
            r.EnsureSuccessStatusCode();
            return (await r.Content.ReadFromJsonAsync<CalendarioMatrizRow>())!;
        }
    }
}
