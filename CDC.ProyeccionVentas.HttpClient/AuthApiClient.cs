using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;

//namespace CDC.ProyeccionVentas.HttpClient
namespace CDC.ProyeccionVentas.HttpClients.Auth

{
    public class AuthApiClient
    {
        private readonly System.Net.Http.HttpClient _httpClient;

        public AuthApiClient(System.Net.Http.HttpClient httpClient)
        {
            // Asegúrate de que BaseAddress esté bien seteada desde el Program.cs
            _httpClient = httpClient;
        }

        public async Task<(bool Success, string Message)> LoginAsync(string numeroEmpleado, string password)
        {
            var loginRequest = new
            {
                NumeroEmpleado = numeroEmpleado,
                Password = password
            };

            // Opción 1: URL relativa (requiere BaseAddress seteada en el DI)
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Opción 2: URL completa (ignora BaseAddress, más explícito para pruebas)
            // var response = await _httpClient.PostAsJsonAsync("http://localhost:5120/api/auth/login", loginRequest);

            if (!response.IsSuccessStatusCode)
                return (false, "No se pudo conectar con el servidor.");

            var data = await response.Content.ReadFromJsonAsync<LoginApiResponse>();

            return (data?.success == true, data?.message ?? "Error desconocido.");
        }

        private class LoginApiResponse
        {
            public bool success { get; set; }
            public string? message { get; set; }
        }
    }
}
