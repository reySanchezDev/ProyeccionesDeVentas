using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.HttpClients.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.HttpClients.Clients
{
    public class ProyeccionVentasConsultaHttpClient : IProyeccionVentasConsultaHttpClient
    {
        private readonly HttpClient _httpClient;

        public ProyeccionVentasConsultaHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ProyeccionVentasToConsulta>> ObtenerFiltradoAsync(DateTime fechaInicio, DateTime fechaFin, string? codSucursal)
        {
            var request = new
            {
                fechaInicio,
                fechaFin,
                codSucursal
            };

            var response = await _httpClient.PostAsJsonAsync("/api/proyeccionventasconsulta/filtrar", request);
            response.EnsureSuccessStatusCode();

            var resultado = await response.Content.ReadFromJsonAsync<List<ProyeccionVentasToConsulta>>();
            return resultado ?? new List<ProyeccionVentasToConsulta>();
        }

        public async Task<bool> GuardarCambiosAsync(List<ActualizarProyeccionDto> cambios)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/proyeccionventasconsulta/guardar", cambios);
            return response.IsSuccessStatusCode;
        }
    }
}
