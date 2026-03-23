using System.Text.Json.Serialization;

namespace HorasExtrasCdC.Frontend.Models;

public class HorasExtraAgregarRequest
{
    [JsonPropertyName("hExt")]
    public string HExt { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("userAprueba")]
    public string UserAprueba { get; set; } = string.Empty;

    [JsonPropertyName("idTabla")]
    public int? IdTabla { get; set; }

    [JsonPropertyName("idSqlite")]
    public string? IdSqlite { get; set; }

    [JsonPropertyName("sucursal")]
    public string? Sucursal { get; set; }

    [JsonPropertyName("empleado")]
    public string? Empleado { get; set; }

    [JsonPropertyName("fechaEntro")]
    public string? FechaEntro { get; set; }
}
