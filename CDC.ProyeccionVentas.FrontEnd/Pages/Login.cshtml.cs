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
        public string NumeroEmpleado { get; set; } = string.Empty;
        [BindProperty]
        public string Contraseña { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

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

            var numeroEmpleadoNormalizado = NumeroEmpleado.Trim();
            var result = await _authApiClient.LoginAsync(numeroEmpleadoNormalizado, Contraseña);

            if (result.Success)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, numeroEmpleadoNormalizado),
                    new Claim(ClaimTypes.NameIdentifier, numeroEmpleadoNormalizado),
                    new Claim("NumeroEmpleado", numeroEmpleadoNormalizado)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

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
