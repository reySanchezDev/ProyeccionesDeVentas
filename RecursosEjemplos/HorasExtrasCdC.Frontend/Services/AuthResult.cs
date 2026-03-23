namespace HorasExtrasCdC.Frontend.Services;

public class AuthResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string NumeroEmpleado { get; init; } = string.Empty;
    public string NombreUsuario { get; init; } = string.Empty;
    public string NombreEmpleado { get; init; } = string.Empty;
    public string RolPrincipal { get; init; } = string.Empty;
    public string RolesRaw { get; init; } = string.Empty;
    public int CantidadSub { get; init; }
    public bool UsuarioExiste { get; init; }

    public static AuthResult Failure(string message) => new()
    {
        Success = false,
        Message = message
    };

    public static AuthResult Ok(
        string numeroEmpleado,
        string nombreUsuario,
        string nombreEmpleado,
        string rolPrincipal,
        string rolesRaw,
        int cantidadSub = 0,
        bool usuarioExiste = true) => new()
    {
        Success = true,
        Message = "Login exitoso.",
        NumeroEmpleado = numeroEmpleado,
        NombreUsuario = nombreUsuario,
        NombreEmpleado = nombreEmpleado,
        RolPrincipal = rolPrincipal,
        RolesRaw = rolesRaw,
        CantidadSub = cantidadSub,
        UsuarioExiste = usuarioExiste
    };
}
