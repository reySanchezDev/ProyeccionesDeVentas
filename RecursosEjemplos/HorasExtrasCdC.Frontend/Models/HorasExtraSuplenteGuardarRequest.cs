using System.Text.Json.Serialization;

namespace HorasExtrasCdC.Frontend.Models;

public class HorasExtraSuplenteGuardarRequest
{
    [JsonPropertyName("empleadoSupervisor")]
    public string EmpleadoSupervisor { get; set; } = string.Empty;

    [JsonPropertyName("empleadoSuplente")]
    public string EmpleadoSuplente { get; set; } = string.Empty;

    [JsonPropertyName("periodoInicia")]
    public string PeriodoInicia { get; set; } = string.Empty;

    [JsonPropertyName("periodoFinaliza")]
    public string? PeriodoFinaliza { get; set; }
}
