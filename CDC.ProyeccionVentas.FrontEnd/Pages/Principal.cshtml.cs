using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CDC.ProyeccionVentas.FrontEnd.Pages
{
    [Authorize]
    public class PrincipalModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
