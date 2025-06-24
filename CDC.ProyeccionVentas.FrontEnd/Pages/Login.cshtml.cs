using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using CDC.ProyeccionVentas.HttpClients.Auth;
 // O el namespace real de tu AuthApiClient

namespace CDC.ProyeccionVentas.FrontEnd.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string NumeroEmpleado { get; set; }
        [BindProperty]
        public string Contraseña { get; set; }

        public string ErrorMessage { get; set; }

        private readonly AuthApiClient _authApiClient;

        public LoginModel(AuthApiClient authApiClient)
        {
            _authApiClient = authApiClient;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(NumeroEmpleado) || string.IsNullOrWhiteSpace(Contraseña))
            {
                ErrorMessage = "Debe ingresar el número de empleado y la contraseña.";
                return Page();
            }

            var result = await _authApiClient.LoginAsync(NumeroEmpleado, Contraseña);

            if (result.Success)
            {
                // Creamos los claims (puedes agregar más si quieres)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, NumeroEmpleado)
                    // Puedes agregar más claims aquí si necesitas
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Emitimos la cookie de autenticación
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                // Redirige a la página principal (o la que desees)
                return RedirectToPage("/Principal");
            }
            else
            {
                ErrorMessage = result.Message;
                return Page();
            }
        }
    }
}
