using System.Text.Json.Serialization;

namespace HorasExtrasCdC.Frontend.Models;

public class HorasExtraSuplenteDeBajaRequest
{
    [JsonPropertyName("idSupervisor")]
    public string IdSupervisor { get; set; } = string.Empty;
}
