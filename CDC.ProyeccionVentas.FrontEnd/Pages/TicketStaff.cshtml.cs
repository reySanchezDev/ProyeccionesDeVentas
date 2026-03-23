using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CDC.ProyeccionVentas.FrontEnd.Pages
{
    [Authorize]
    public class TicketStaffModel : PageModel
    {
        public IActionResult OnGet()
        {
            return RedirectToPage("/SubirTicketStaff");
        }
    }
}
