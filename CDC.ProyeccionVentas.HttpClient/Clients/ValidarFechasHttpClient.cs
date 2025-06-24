using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.HttpClients.Interfaces;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.HttpClients.Clients
{
    public class ValidarFechasHttpClient : IValidarFechasHttpClient
    {
        private readonly HttpClient _httpClient;

        public ValidarFechasHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ValidarFechaRequest>> FiltrarExistentesAsync(List<ValidarFechaRequest> datos)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/validarfechas/filtrar-existentes", datos);

            response.EnsureSuccessStatusCode();

            var resultado = await response.Content.ReadFromJsonAsync<List<ValidarFechaRequest>>();
            return resultado ?? new List<ValidarFechaRequest>();
        }
    }
}
