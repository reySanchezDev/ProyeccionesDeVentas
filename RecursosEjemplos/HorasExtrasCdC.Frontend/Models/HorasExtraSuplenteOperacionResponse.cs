using System.Text.Json.Serialization;

namespace HorasExtrasCdC.Frontend.Models;

public class HorasExtraSuplenteOperacionResponse
{
    [JsonPropertyName("codigo")]
    public int Codigo { get; set; }

    [JsonPropertyName("mensaje")]
    public string Mensaje { get; set; } = string.Empty;

    [JsonPropertyName("afectados")]
    public int Afectados { get; set; }
}
