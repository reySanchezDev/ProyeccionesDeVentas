using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.AuthService.Servicios
{
    public interface IAuthService
    {
        Task<(bool Success, string Message)> ValidarCredencialesAsync(string numeroEmpleado, string passwordPlano);
    }
}
