using System.Security.Cryptography;
using HorasExtrasCdC.Frontend.Models;
using Microsoft.Extensions.Options;

namespace HorasExtrasCdC.Frontend.Services;

public class AuthService : IAuthService
{
    private const int Pbkdf2Iterations = 10000;
    private const int Pbkdf2Bytes = 20;

    private readonly IHorasExtrasApiClient _apiClient;
    private readonly HashSet<string> _ghRoleIds;
    private readonly HashSet<string> _supervisorRoleIds;
    private readonly HashSet<string> _empleadoRoleIds;
    private readonly string[] _ghRoleNameKeywords;
    private readonly string[] _supervisorRoleNameKeywords;
    private readonly string[] _empleadoRoleNameKeywords;

    public AuthService(
        IHorasExtrasApiClient apiClient,
        IOptions<AuthRolesOptions> authRolesOptions)
    {
        _apiClient = apiClient;

        var options = authRolesOptions?.Value ?? new AuthRolesOptions();
        _ghRoleIds = BuildRoleIdSet(options.GhRoleIds, "00005");
        _supervisorRoleIds = BuildRoleIdSet(options.SupervisorRoleIds, "00002");
        _empleadoRoleIds = BuildRoleIdSet(options.EmpleadoRoleIds, "00001");
        _ghRoleNameKeywords = BuildRoleNameKeywords(options.GhRoleNameKeywords, "RRHH", "RECURSOS HUMANOS", "GESTION HUMANA");
        _supervisorRoleNameKeywords = BuildRoleNameKeywords(options.SupervisorRoleNameKeywords, "SUPERVISOR");
        _empleadoRoleNameKeywords = BuildRoleNameKeywords(options.EmpleadoRoleNameKeywords, "EMPLEADO");
    }

    public async Task<AuthResult> AuthenticateAsync(
        string numeroEmpleado,
        string passwordPlano,
        CancellationToken cancellationToken = default)
    {
        AuthResult Fail(string message)
        {
            return AuthResult.Failure(message);
        }

        if (string.IsNullOrWhiteSpace(numeroEmpleado) ||
            string.IsNullOrWhiteSpace(passwordPlano))
        {
            return Fail("Debe completar numero de empleado y contrasenia.");
        }

        var numeroEmpleadoNormalizado = numeroEmpleado.Trim();
        var usuario = await _apiClient.ObtenerUsuarioAsync(numeroEmpleadoNormalizado, cancellationToken);

        if (usuario.Codigo != 0)
        {
            return Fail(string.IsNullOrWhiteSpace(usuario.Mensaje)
                ? "No se pudo validar el usuario."
                : usuario.Mensaje);
        }

        if (usuario.Activo != true)
        {
            return Fail("El usuario esta inactivo.");
        }

        if (string.IsNullOrWhiteSpace(usuario.Contrasenia))
        {
            return Fail("El usuario no tiene contrasenia configurada.");
        }

        if (!PasswordMatches(passwordPlano, usuario.Contrasenia, usuario.Salt))
        {
            return Fail("Contrasenia incorrecta.");
        }

        var numeroEmpleadoCanonico = !string.IsNullOrWhiteSpace(usuario.NumeroEmpleado)
            ? usuario.NumeroEmpleado.Trim()
            : numeroEmpleadoNormalizado;
        var candidatosRoles = BuildRoleCandidates(numeroEmpleadoCanonico, usuario.Correo);
        HorasExtraRolesUsuarioResponse? roles = null;

        foreach (var candidato in candidatosRoles)
        {
            var intento = await _apiClient.ObtenerRolesAsync(candidato, cancellationToken);

            if (intento.Codigo == 0 && intento.Roles.Count > 0)
            {
                roles = intento;
                break;
            }

            if (roles is null)
            {
                roles = intento;
            }
        }

        if (roles is null || roles.Codigo != 0)
        {
            return Fail(string.IsNullOrWhiteSpace(roles?.Mensaje)
                ? "No se pudieron obtener los roles del usuario."
                : roles.Mensaje);
        }

        if (roles.Roles.Count == 0)
        {
            return Fail("El usuario no tiene roles activos para ingresar.");
        }

        var usuarioExisteResponse = await _apiClient.UsuarioExisteAsync(numeroEmpleadoCanonico, cancellationToken);
        if (usuarioExisteResponse.Codigo != 0)
        {
            return Fail(string.IsNullOrWhiteSpace(usuarioExisteResponse.Mensaje)
                ? "No se pudo validar la autorizacion del usuario."
                : usuarioExisteResponse.Mensaje);
        }

        if (!usuarioExisteResponse.Existe)
        {
            return Fail("No esta autorizado a visualizar esta pagina.");
        }

        var cantidadSubResponse = await _apiClient.ObtenerCantidadSubAsync(numeroEmpleadoCanonico, cancellationToken);
        if (cantidadSubResponse.Codigo != 0)
        {
            return Fail(string.IsNullOrWhiteSpace(cantidadSubResponse.Mensaje)
                ? "No se pudo calcular la cantidad de subordinados."
                : cantidadSubResponse.Mensaje);
        }

        var esSupervisor = roles.Roles.Any(r =>
            RoleIdMatches(r.RolId, _supervisorRoleIds) ||
            RoleNameMatches(r.Nombre, _supervisorRoleNameKeywords));
        var esGh = roles.Roles.Any(r =>
            RoleIdMatches(r.RolId, _ghRoleIds) ||
            RoleNameMatches(r.Nombre, _ghRoleNameKeywords));
        var esEmpleado = roles.Roles.Any(r =>
            RoleIdMatches(r.RolId, _empleadoRoleIds) ||
            RoleNameMatches(r.Nombre, _empleadoRoleNameKeywords));

        if (!esSupervisor && !esGh && !esEmpleado)
        {
            var rolesDetectados = roles.Roles
                .Select(r => r.RolId?.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (rolesDetectados.Length > 0)
            {
                var rolesTexto = string.Join(", ", rolesDetectados);
                return Fail(
                    $"Su usuario tiene rol(es) {rolesTexto}, pero ninguno esta habilitado para Horas Extras. " +
                    "Roles habilitados: 00001 (Empleado), 00002 (Supervisor) y 00005 (Gestion Humana).");
            }

            return Fail("El usuario no tiene un rol autorizado para ingresar.");
        }

        // Prioridad: GH > SUPERVISOR > EMPLEADO.
        var rolPrincipal = esGh
            ? "GH"
            : esSupervisor
                ? "SUPERVISOR"
                : "EMPLEADO";
        var rolesRaw = string.Join(',', roles.Roles.Select(r => r.RolId).Where(v => !string.IsNullOrWhiteSpace(v)));
        var nombreUsuarioFinal = string.IsNullOrWhiteSpace(roles.NombreUsuario)
            ? numeroEmpleadoCanonico
            : roles.NombreUsuario.Trim();
        var nombreEmpleadoFinal = !string.IsNullOrWhiteSpace(usuario.Nombre)
            ? usuario.Nombre.Trim()
            : numeroEmpleadoCanonico;

        return AuthResult.Ok(
            numeroEmpleadoCanonico,
            nombreUsuarioFinal,
            nombreEmpleadoFinal,
            rolPrincipal,
            rolesRaw,
            cantidadSubResponse.CantidadSub,
            usuarioExisteResponse.Existe);
    }

    private static bool PasswordMatches(
        string passwordPlano,
        string? hashAlmacenado,
        string? salt)
    {
        if (string.IsNullOrWhiteSpace(hashAlmacenado))
        {
            return false;
        }

        var hashEsperado = hashAlmacenado.Trim();
        if (!TryDecodeSalt(salt, out var saltBytes))
        {
            return false;
        }

        var hashGenerado = ComputePbkdf2Hash(passwordPlano, saltBytes);
        return string.Equals(hashGenerado, hashEsperado, StringComparison.Ordinal);
    }

    private static string ComputePbkdf2Hash(string password, byte[] saltBytes)
    {
        using var deriveBytes = new Rfc2898DeriveBytes(
            password,
            saltBytes,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA1);
        var hashBytes = deriveBytes.GetBytes(Pbkdf2Bytes);
        return Convert.ToBase64String(hashBytes);
    }

    private static bool TryDecodeSalt(string? salt, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(salt))
        {
            return false;
        }

        var normalized = salt.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        try
        {
            bytes = Convert.FromBase64String(normalized);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static HashSet<string> BuildRoleIdSet(IEnumerable<string>? configuredValues, params string[] defaults)
    {
        var values = (configuredValues ?? Array.Empty<string>())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToList();

        if (values.Count == 0)
        {
            values.AddRange(defaults.Where(v => !string.IsNullOrWhiteSpace(v)));
        }

        return new HashSet<string>(values, StringComparer.Ordinal);
    }

    private static string[] BuildRoleNameKeywords(IEnumerable<string>? configuredValues, params string[] defaults)
    {
        var values = (configuredValues ?? Array.Empty<string>())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (values.Length > 0)
        {
            return values;
        }

        return defaults
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool RoleIdMatches(string? roleId, HashSet<string> allowedRoleIds)
    {
        if (allowedRoleIds.Count == 0 || string.IsNullOrWhiteSpace(roleId))
        {
            return false;
        }

        return allowedRoleIds.Contains(roleId.Trim());
    }

    private static bool RoleNameMatches(string? roleName, IEnumerable<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return false;
        }

        var normalizedRoleName = roleName.Trim();
        foreach (var keyword in keywords)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if (normalizedRoleName.Contains(keyword.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static List<string> BuildRoleCandidates(string numeroEmpleado, string? correo)
    {
        var candidatos = new List<string>();
        var numero = numeroEmpleado.Trim();
        if (!string.IsNullOrWhiteSpace(numero))
        {
            candidatos.Add(numero);
        }

        if (!string.IsNullOrWhiteSpace(correo))
        {
            var correoTrim = correo.Trim();
            if (!string.IsNullOrWhiteSpace(correoTrim))
            {
                candidatos.Add(correoTrim);
            }

            var userPart = correoTrim.Split('@')[0];
            if (!string.IsNullOrWhiteSpace(userPart))
            {
                candidatos.Add(userPart);
            }
        }

        return candidatos
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
