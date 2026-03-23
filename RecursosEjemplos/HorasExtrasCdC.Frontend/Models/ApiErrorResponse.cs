using System.Text.Json.Serialization;

namespace HorasExtrasCdC.Frontend.Models;

public class ApiErrorResponse
{
    [JsonPropertyName("mensaje")]
    public string? Mensaje { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
