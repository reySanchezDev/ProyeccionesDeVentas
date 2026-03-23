using System.Text.Json.Serialization;

namespace HorasExtrasCdC.Frontend.Models;

public class HorasExtraUsuarioExisteResponse
{
    [JsonPropertyName("codigo")]
    public int Codigo { get; set; }

    [JsonPropertyName("mensaje")]
    public string Mensaje { get; set; } = string.Empty;

    [JsonPropertyName("existe")]
    public bool Existe { get; set; }
}
