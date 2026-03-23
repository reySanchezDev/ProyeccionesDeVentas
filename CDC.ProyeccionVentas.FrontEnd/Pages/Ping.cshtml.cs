using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace CDC.ProyeccionVentas.FrontEnd.Pages
{
    public class PingModel : PageModel
    {
        public IActionResult OnGet()
        {
            return Content("OK"); // Siempre devuelve 200 si el servidor está arriba
        }
    }
}
