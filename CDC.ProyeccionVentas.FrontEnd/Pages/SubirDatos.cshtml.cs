using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CDC.ProyeccionVentas.HttpClients.Interfaces;
using CDC.ProyeccionVentas.Dominio.Entidades;

namespace CDC.ProyeccionVentas.FrontEnd.Pages
{
    public class SubirDatosModel : PageModel
    {
        private readonly IValidarFechasHttpClient _validarFechasHttpClient;
        private readonly IConfiguration _configuration;

        public string ApiBaseUrl { get; private set; }

        public SubirDatosModel(IValidarFechasHttpClient validarFechasHttpClient, IConfiguration configuration)
        {
            _validarFechasHttpClient = validarFechasHttpClient;
            _configuration = configuration;
        }

        public void OnGet()
        {
            ApiBaseUrl = _configuration["Servicios:ProyeccionVentasAPI"];
        }

        // Este método se puede usar en el flujo de validación antes de guardar
        public async Task<List<ValidarFechaRequest>> ValidarDuplicadosAsync(List<ValidarFechaRequest> datosArchivo)
        {
            return await _validarFechasHttpClient.FiltrarExistentesAsync(datosArchivo);
        }
    }
}
