using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HorasExtrasCdC.Frontend.Pages;

[AllowAnonymous]
public class ErrorConexionModel : PageModel
{
    public string TitleText { get; private set; } = "Sin conexion con el servicio";

    public string MessageText { get; private set; } =
        "No se pudo completar la operacion porque el servicio no esta disponible.";

    public string TipoEtiqueta { get; private set; } = "Conexion";

    public string TipoCssClass { get; private set; } = "status-connection";

    public IReadOnlyList<string> Recomendaciones { get; private set; } = new[]
    {
        "Verifique su conexion y vuelva a intentar.",
        "Si el problema persiste, contacte al soporte tecnico."
    };

    public string ReturnUrl { get; private set; } = "/Login";

    public string? Origen { get; private set; }

    public string? Endpoint { get; private set; }

    public string? Fuente { get; private set; }

    public string? TraceId { get; private set; }

    public string? Codigo { get; private set; }

    public string? DetalleTecnico { get; private set; }

    public string TimestampLocal => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    public void OnGet(
        string? tipo,
        string? returnUrl,
        string? origen,
        string? mensaje,
        string? detalle,
        string? endpoint,
        string? fuente,
        string? traceId,
        int? codigo)
    {
        var tipoNormalizado = NormalizeTipo(tipo);
        ConfigureTextsByTipo(tipoNormalizado);

        if (!string.IsNullOrWhiteSpace(mensaje))
        {
            MessageText = mensaje.Trim();
        }

        Origen = CleanValue(origen);
        Endpoint = CleanValue(endpoint);
        Fuente = CleanValue(fuente);
        TraceId = CleanValue(traceId);
        Codigo = codigo.HasValue ? codigo.Value.ToString() : null;
        DetalleTecnico = CleanValue(detalle);
        ReturnUrl = ResolveReturnUrl(returnUrl);
    }

    private void ConfigureTextsByTipo(string tipo)
    {
        switch (tipo)
        {
            case "internet":
                TitleText = "Sin internet en este dispositivo";
                MessageText = "Se perdio la conexion a internet. Restablezca la red y vuelva a intentar.";
                TipoEtiqueta = "Sin Internet";
                TipoCssClass = "status-internet";
                Recomendaciones = new[]
                {
                    "Revise Wi-Fi, cable de red o VPN.",
                    "Valide que pueda abrir otros sitios.",
                    "Cuando la conexion regrese, pulse Reintentar."
                };
                break;
            case "timeout":
                TitleText = "Tiempo de espera agotado";
                MessageText = "El servidor tardo demasiado en responder. Intente nuevamente en unos minutos.";
                TipoEtiqueta = "Timeout";
                TipoCssClass = "status-timeout";
                Recomendaciones = new[]
                {
                    "Espere unos segundos y vuelva a intentar.",
                    "Si persiste, revise carga del servidor/API.",
                    "Si aplica, escale al equipo de infraestructura."
                };
                break;
            case "conexion":
                TitleText = "No hay conexion con el servidor";
                MessageText = "La aplicacion no pudo comunicarse con el servicio de horas extras.";
                TipoEtiqueta = "Servidor no disponible";
                TipoCssClass = "status-connection";
                Recomendaciones = new[]
                {
                    "Verifique que el API este arriba y accesible.",
                    "Revise DNS, firewall, certificados y puertos.",
                    "Si continua, comparta el diagnostico con soporte."
                };
                break;
            default:
                TitleText = "Error de conectividad";
                MessageText = "Ocurrio un error al intentar comunicarse con el servicio.";
                TipoEtiqueta = "Error";
                TipoCssClass = "status-error";
                Recomendaciones = new[]
                {
                    "Intente de nuevo.",
                    "Si el error continua, contacte soporte tecnico."
                };
                break;
        }
    }

    private static string NormalizeTipo(string? tipo)
    {
        var normalized = (tipo ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "internet" => "internet",
            "offline" => "internet",
            "sin-internet" => "internet",
            "timeout" => "timeout",
            "conexion" => "conexion",
            "servidor" => "conexion",
            _ => "error"
        };
    }

    private static string? CleanValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > 1500 ? trimmed[..1500] : trimmed;
    }

    private string ResolveReturnUrl(string? rawReturnUrl)
    {
        if (!string.IsNullOrWhiteSpace(rawReturnUrl) && Url.IsLocalUrl(rawReturnUrl))
        {
            return rawReturnUrl;
        }

        return User.Identity?.IsAuthenticated == true ? "/Index" : "/Login";
    }
}
