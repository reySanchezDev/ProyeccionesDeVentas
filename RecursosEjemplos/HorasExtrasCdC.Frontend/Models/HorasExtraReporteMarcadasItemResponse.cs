using System.Text.Json.Serialization;

namespace HorasExtrasCdC.Frontend.Models;

public class HorasExtraReporteMarcadasItemResponse
{
    [JsonPropertyName("empleado")]
    public string Empleado { get; set; } = string.Empty;

    [JsonPropertyName("nombres")]
    public string? Nombres { get; set; }

    [JsonPropertyName("apellidos")]
    public string? Apellidos { get; set; }

    [JsonPropertyName("ubicadoEn")]
    public string? UbicadoEn { get; set; }

    [JsonPropertyName("marcaEn")]
    public string? MarcaEn { get; set; }

    [JsonPropertyName("fecha")]
    public string? Fecha { get; set; }

    [JsonPropertyName("entrada")]
    public string? Entrada { get; set; }

    [JsonPropertyName("salida")]
    public string? Salida { get; set; }

    [JsonPropertyName("laboradas")]
    public decimal? Laboradas { get; set; }
}
