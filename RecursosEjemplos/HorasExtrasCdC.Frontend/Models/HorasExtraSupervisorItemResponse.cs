using System.Text.Json.Serialization;

namespace HorasExtrasCdC.Frontend.Models;

public class HorasExtraSupervisorItemResponse
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("idSqlite")]
    public string? IdSqlite { get; set; }

    [JsonPropertyName("empleado")]
    public string Empleado { get; set; } = string.Empty;

    [JsonPropertyName("sucursal")]
    public string? Sucursal { get; set; }

    [JsonPropertyName("ubicadoEn")]
    public string? UbicadoEn { get; set; }

    [JsonPropertyName("fecha")]
    public string? Fecha { get; set; }

    [JsonPropertyName("entrada")]
    public string? Entrada { get; set; }

    [JsonPropertyName("salida")]
    public string? Salida { get; set; }

    [JsonPropertyName("hExt")]
    public decimal? HExt { get; set; }

    [JsonPropertyName("totalHE")]
    public decimal? TotalHE { get; set; }

    [JsonPropertyName("descripcion")]
    public string? Descripcion { get; set; }

    [JsonPropertyName("fAprueba")]
    public string? FAprueba { get; set; }

    [JsonPropertyName("userAprueba")]
    public string? UserAprueba { get; set; }

    [JsonPropertyName("nombreEmpleado")]
    public string? NombreEmpleado { get; set; }

    [JsonPropertyName("nombreCompleto")]
    public string? NombreCompleto { get; set; }

    [JsonPropertyName("fechaentro")]
    public string? FechaEntro { get; set; }

    [JsonPropertyName("ubicacion")]
    public string? Ubicacion { get; set; }

    [JsonPropertyName("nombreSupervisor")]
    public string? NombreSupervisor { get; set; }

    [JsonPropertyName("numeroSupervisor")]
    public string? NumeroSupervisor { get; set; }

    [JsonPropertyName("hLaboradas")]
    public decimal? HLaboradas { get; set; }
}
