using System.Text.Json.Serialization;

namespace HorasExtrasCdC.Frontend.Models;

public class HorasExtraConsolidadaItemResponse
{
    [JsonPropertyName("empleado")]
    public string Empleado { get; set; } = string.Empty;

    [JsonPropertyName("totalHE")]
    public decimal TotalHE { get; set; }

    [JsonPropertyName("nombreEmpleado")]
    public string NombreEmpleado { get; set; } = string.Empty;

    [JsonPropertyName("ubicacion")]
    public string Ubicacion { get; set; } = string.Empty;

    [JsonPropertyName("nombreSupervisor")]
    public string NombreSupervisor { get; set; } = string.Empty;
}
