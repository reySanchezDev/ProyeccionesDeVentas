using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HorasExtrasCdC.Frontend.Pages;

[Authorize]
public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        var rolPrincipal = User.FindFirst("rolPrincipal")?.Value;

        return rolPrincipal switch
        {
            "GH" => RedirectToPage("/ListadoGH"),
            "SUPERVISOR" => RedirectToPage("/ListadoxSupervisor"),
            "EMPLEADO" => RedirectToPage("/ListadoxSupervisor"),
            _ => RedirectToPage("/AccesoDenegado")
        };
    }
}
