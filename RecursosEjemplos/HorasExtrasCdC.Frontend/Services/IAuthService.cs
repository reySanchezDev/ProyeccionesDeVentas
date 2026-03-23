namespace HorasExtrasCdC.Frontend.Services;

public interface IAuthService
{
    Task<AuthResult> AuthenticateAsync(
        string numeroEmpleado,
        string passwordPlano,
        CancellationToken cancellationToken = default);
}
