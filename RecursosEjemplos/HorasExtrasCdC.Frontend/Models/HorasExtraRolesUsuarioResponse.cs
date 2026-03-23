using System.Text.Json.Serialization;

namespace HorasExtrasCdC.Frontend.Models;

public class HorasExtraRolesUsuarioResponse
{
    [JsonPropertyName("codigo")]
    public int Codigo { get; set; }

    [JsonPropertyName("mensaje")]
    public string Mensaje { get; set; } = string.Empty;

    [JsonPropertyName("nombreUsuario")]
    public string? NombreUsuario { get; set; }

    [JsonPropertyName("roles")]
    public List<HorasExtraRolItemResponse> Roles { get; set; } = new();
}
