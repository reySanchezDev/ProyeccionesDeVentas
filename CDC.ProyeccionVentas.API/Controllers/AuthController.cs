using CDC.ProyeccionVentas.API.Models;
using CDC.ProyeccionVentas.AuthService.Servicios;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CDC.ProyeccionVentas.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.ValidarCredencialesAsync(request.NumeroEmpleado, request.Password);

                // result es un objeto como: { Success = true/false, Message = "..." }

                return Ok(new
                {
                    success = result.Success,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                // Devuelve el error para que el FrontEnd lo muestre
                return Ok(new
                {
                    success = false,
                    message = "Error en el API: " + ex.Message
                });
            }
        }
    }
}
