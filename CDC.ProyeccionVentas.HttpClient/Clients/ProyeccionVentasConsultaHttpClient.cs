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

        public async Task<List<ProyeccionVentasToConsulta>> ObtenerFiltradoAsync(FiltroProyeccionVentas filtro)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/proyeccionventasconsulta/filtrar", filtro);
            response.EnsureSuccessStatusCode();

            var resultado = await response.Content.ReadFromJsonAsync<List<ProyeccionVentasToConsulta>>();
            return resultado ?? new List<ProyeccionVentasToConsulta>();
        }

        public async Task<bool> GuardarCambiosAsync(List<ActualizarProyeccionDto> cambios)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/proyeccionventasconsulta/guardar", cambios);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(errorBody))
            {
                throw new InvalidOperationException($"La API devolvió HTTP {(int)response.StatusCode} al guardar cambios.");
            }

            throw new InvalidOperationException(errorBody);
        }

        public async Task<EliminarProyeccionVentasResult> EliminarPorRangoAsync(EliminarProyeccionVentasRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/proyeccionventasconsulta/eliminar", request);
            if (response.IsSuccessStatusCode)
            {
                var resultado = await response.Content.ReadFromJsonAsync<EliminarProyeccionVentasResult>();
                return resultado ?? new EliminarProyeccionVentasResult
                {
                    RegistrosEliminados = 0,
                    Mensaje = "La API no devolvió un resultado de eliminación."
                };
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(errorBody))
            {
                throw new InvalidOperationException($"La API devolvió HTTP {(int)response.StatusCode} al eliminar proyecciones.");
            }

            throw new InvalidOperationException(errorBody);
        }
    }
}
