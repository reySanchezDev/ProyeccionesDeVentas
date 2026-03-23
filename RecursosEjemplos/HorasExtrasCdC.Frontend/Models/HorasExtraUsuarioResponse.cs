using System.Text.Json.Serialization;

namespace HorasExtrasCdC.Frontend.Models;

public class HorasExtraUsuarioResponse
{
    [JsonPropertyName("codigo")]
    public int Codigo { get; set; }

    [JsonPropertyName("mensaje")]
    public string Mensaje { get; set; } = string.Empty;

    [JsonPropertyName("numeroEmpleado")]
    public string? NumeroEmpleado { get; set; }

    [JsonPropertyName("nombre")]
    public string? Nombre { get; set; }

    [JsonPropertyName("cedula")]
    public string? Cedula { get; set; }

    [JsonPropertyName("fechaIngreso")]
    public DateTime? FechaIngreso { get; set; }

    [JsonPropertyName("contrasenia")]
    public string? Contrasenia { get; set; }

    [JsonPropertyName("salt")]
    public string? Salt { get; set; }

    [JsonPropertyName("correo")]
    public string? Correo { get; set; }

    [JsonPropertyName("activo")]
    public bool? Activo { get; set; }
}
