namespace HorasExtrasCdC.Frontend.Models;

public class AuthRolesOptions
{
    public const string SectionName = "AuthRoles";

    public List<string> GhRoleIds { get; set; } = new() { "00005" };

    public List<string> SupervisorRoleIds { get; set; } = new() { "00002" };

    public List<string> EmpleadoRoleIds { get; set; } = new() { "00001" };

    public List<string> GhRoleNameKeywords { get; set; } = new()
    {
        "RRHH",
        "RECURSOS HUMANOS",
        "GESTION HUMANA"
    };

    public List<string> SupervisorRoleNameKeywords { get; set; } = new()
    {
        "SUPERVISOR"
    };

    public List<string> EmpleadoRoleNameKeywords { get; set; } = new()
    {
        "EMPLEADO"
    };
}
