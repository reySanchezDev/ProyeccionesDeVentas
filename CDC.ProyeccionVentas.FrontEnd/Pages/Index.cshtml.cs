
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CDC.ProyeccionVentas.FrontEnd.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            return RedirectToPage("/Login");
        }
    }
}


//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;

//namespace CDC.ProyeccionVentas.FrontEnd.Pages
//{
//    public class IndexModel : PageModel
//    {
//        private readonly ILogger<IndexModel> _logger;

//        public IndexModel(ILogger<IndexModel> logger)
//        {
//            _logger = logger;
//        }

//        public void OnGet()
//        {

//        }
//    }
//}
