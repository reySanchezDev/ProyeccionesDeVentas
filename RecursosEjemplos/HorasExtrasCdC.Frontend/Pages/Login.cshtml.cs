using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HorasExtrasCdC.Frontend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HorasExtrasCdC.Frontend.Pages;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly IAuthService _authService;

    public LoginModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    [Required]
    public string NumeroEmpleado { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    public string Contrasenia { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Debe completar numero de empleado y contrasenia.";
            return Page();
        }

        AuthResult authResult;
        try
        {
            authResult = await _authService.AuthenticateAsync(
                NumeroEmpleado,
                Contrasenia,
                cancellationToken);
        }
        catch (ApiConnectivityException ex)
        {
            return RedirectToConnectivityPage(ex, "Login.OnPost");
        }

        if (!authResult.Success)
        {
            ErrorMessage = authResult.Message;
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, authResult.NumeroEmpleado),
            new("numeroEmpleado", authResult.NumeroEmpleado),
            new("nombreUsuario", authResult.NombreUsuario),
            new("nombreEmpleado", authResult.NombreEmpleado),
            new("rolPrincipal", authResult.RolPrincipal),
            new("rolesRaw", authResult.RolesRaw),
            new("cantidadSub", authResult.CantidadSub.ToString()),
            new("usuarioExiste", authResult.UsuarioExiste ? "1" : "0")
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = false,
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        var rutaDestino = authResult.RolPrincipal == "GH"
            ? "/ListadoGH"
            : "/ListadoxSupervisor";

        return RedirectToPage(rutaDestino);
    }

    private IActionResult RedirectToConnectivityPage(ApiConnectivityException ex, string origen)
    {
        var tipo = ex.IsTimeout ? "timeout" : "conexion";
        var endpoint = string.IsNullOrWhiteSpace(ex.Endpoint) ? "N/D" : ex.Endpoint.Trim();
        var baseDetail = $"Endpoint: {endpoint}.";
        var inner = ex.InnerException?.Message;
        var detail = string.IsNullOrWhiteSpace(inner)
            ? baseDetail
            : $"{baseDetail} Causa: {inner.Trim()}";
        var detalle = detail.Length <= 900 ? detail : detail[..900];

        return RedirectToPage(
            "/ErrorConexion",
            new
            {
                tipo,
                mensaje = ex.UserMessage,
                detalle,
                endpoint = ex.Endpoint,
                origen,
                returnUrl = $"{Request.Path}{Request.QueryString}",
                fuente = "login-page",
                codigo = StatusCodes.Status503ServiceUnavailable
            })!;
    }
}
