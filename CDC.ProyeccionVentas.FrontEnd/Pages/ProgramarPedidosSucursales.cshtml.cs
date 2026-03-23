using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.HttpClients.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CDC.ProyeccionVentas.FrontEnd.Pages
{
    public class ProgramarPedidosSucursalesModel : PageModel
    {
        private readonly ICalendarioHttpClient _client;
        public ProgramarPedidosSucursalesModel(ICalendarioHttpClient client) => _client = client;

        public List<CalendarioMatrizRow> Matriz { get; private set; } = new();

        public async Task OnGetAsync()
        {
            ViewData["ShowSidebar"] = true;
            Matriz = await _client.ObtenerMatrizAsync();
        }

        public class ToggleReq
        {
            public string storeNo { get; set; } = "";
            public int diaSemanaIso { get; set; }
            public bool marcado { get; set; }
        }

        public async Task<IActionResult> OnPostToggleAsync([FromBody] ToggleReq req)
        {
            if (req is null || string.IsNullOrWhiteSpace(req.storeNo) || req.diaSemanaIso is < 1 or > 7)
                return BadRequest("Payload inválido.");

            var row = await _client.GuardarDiaAsync(req.storeNo, req.diaSemanaIso, req.marcado);
            return new JsonResult(row);
        }
    }
}
