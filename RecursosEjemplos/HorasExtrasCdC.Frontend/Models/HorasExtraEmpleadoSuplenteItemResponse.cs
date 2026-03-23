using System.Text.Json.Serialization;

namespace HorasExtrasCdC.Frontend.Models;

public class HorasExtraEmpleadoSuplenteItemResponse
{
    [JsonPropertyName("nombresApellidos")]
    public string NombresApellidos { get; set; } = string.Empty;

    [JsonPropertyName("selected")]
    public int Selected { get; set; }

    [JsonPropertyName("carnet")]
    public string Carnet { get; set; } = string.Empty;
}
