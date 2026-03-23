using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.HttpClients.Interfaces;
using System.Net.Http.Json;

namespace CDC.ProyeccionVentas.HttpClients.Clients
{
    public class StoresHttpClient : IStoresHttpClient
    {
        private readonly HttpClient _httpClient;

        public StoresHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Store>> ObtenerStoresAsync()
        {
            var response = await _httpClient.GetAsync("/api/stores");
            response.EnsureSuccessStatusCode();

            var stores = await response.Content.ReadFromJsonAsync<List<Store>>();
            return stores ?? new List<Store>();
        }
    }
}
