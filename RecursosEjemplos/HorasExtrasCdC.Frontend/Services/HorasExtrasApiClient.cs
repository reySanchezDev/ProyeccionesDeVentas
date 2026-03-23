using System.Text.Json;
using System.Net.Http.Json;
using System.Net;
using HorasExtrasCdC.Frontend.Models;

namespace HorasExtrasCdC.Frontend.Services;

public class HorasExtrasApiClient : IHorasExtrasApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public HorasExtrasApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HorasExtraUsuarioResponse> ObtenerUsuarioAsync(string numeroEmpleado, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(("numeroEmpleado", numeroEmpleado));
        var endpoint = $"/api/HorasExtra/usuario{query}";
        using var response = await SendGetAsync(endpoint, cancellationToken);
        var payload = await ReadPayloadAsync(response, endpoint, cancellationToken);
        ThrowIfConnectivityHttpError(response, endpoint, payload);

        if (TryDeserialize<HorasExtraUsuarioResponse>(payload, out var data) && data is not null)
        {
            if (!response.IsSuccessStatusCode && data.Codigo == 0)
            {
                data.Codigo = (int)response.StatusCode;
                data.Mensaje = string.IsNullOrWhiteSpace(data.Mensaje)
                    ? $"Error HTTP {(int)response.StatusCode}."
                    : data.Mensaje;
            }

            return data;
        }

        return new HorasExtraUsuarioResponse
        {
            Codigo = response.IsSuccessStatusCode ? 500 : (int)response.StatusCode,
            Mensaje = ExtractErrorMessage(payload) ?? "No se pudo interpretar la respuesta de usuario."
        };
    }

    public async Task<HorasExtraRolesUsuarioResponse> ObtenerRolesAsync(string nombreUsuario, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(("nombreUsuario", nombreUsuario));
        var endpoint = $"/api/HorasExtra/roles{query}";
        using var response = await SendGetAsync(endpoint, cancellationToken);
        var payload = await ReadPayloadAsync(response, endpoint, cancellationToken);
        ThrowIfConnectivityHttpError(response, endpoint, payload);

        if (TryDeserialize<HorasExtraRolesUsuarioResponse>(payload, out var data) && data is not null)
        {
            if (!response.IsSuccessStatusCode && data.Codigo == 0)
            {
                data.Codigo = (int)response.StatusCode;
                data.Mensaje = string.IsNullOrWhiteSpace(data.Mensaje)
                    ? $"Error HTTP {(int)response.StatusCode}."
                    : data.Mensaje;
            }

            return data;
        }

        return new HorasExtraRolesUsuarioResponse
        {
            Codigo = response.IsSuccessStatusCode ? 500 : (int)response.StatusCode,
            Mensaje = ExtractErrorMessage(payload) ?? "No se pudo interpretar la respuesta de roles.",
            NombreUsuario = nombreUsuario
        };
    }

    public async Task<HorasExtraUsuarioExisteResponse> UsuarioExisteAsync(string noEmpleado, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(("noEmpleado", noEmpleado));
        var endpoint = $"/api/HorasExtra/usuario-existe{query}";
        using var response = await SendGetAsync(endpoint, cancellationToken);
        var payload = await ReadPayloadAsync(response, endpoint, cancellationToken);
        ThrowIfConnectivityHttpError(response, endpoint, payload);

        if (TryDeserialize<HorasExtraUsuarioExisteResponse>(payload, out var data) && data is not null)
        {
            if (!response.IsSuccessStatusCode && data.Codigo == 0)
            {
                data.Codigo = (int)response.StatusCode;
                data.Mensaje = string.IsNullOrWhiteSpace(data.Mensaje)
                    ? $"Error HTTP {(int)response.StatusCode}."
                    : data.Mensaje;
            }

            return data;
        }

        return new HorasExtraUsuarioExisteResponse
        {
            Codigo = response.IsSuccessStatusCode ? 500 : (int)response.StatusCode,
            Mensaje = ExtractErrorMessage(payload) ?? "No se pudo interpretar la respuesta de usuario-existe.",
            Existe = false
        };
    }

    public async Task<HorasExtraCantidadSubResponse> ObtenerCantidadSubAsync(string noEmpleado, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(("noEmpleado", noEmpleado));
        var endpoint = $"/api/HorasExtra/cantidad-sub{query}";
        using var response = await SendGetAsync(endpoint, cancellationToken);
        var payload = await ReadPayloadAsync(response, endpoint, cancellationToken);
        ThrowIfConnectivityHttpError(response, endpoint, payload);

        if (TryDeserialize<HorasExtraCantidadSubResponse>(payload, out var data) && data is not null)
        {
            if (!response.IsSuccessStatusCode && data.Codigo == 0)
            {
                data.Codigo = (int)response.StatusCode;
                data.Mensaje = string.IsNullOrWhiteSpace(data.Mensaje)
                    ? $"Error HTTP {(int)response.StatusCode}."
                    : data.Mensaje;
            }

            return data;
        }

        return new HorasExtraCantidadSubResponse
        {
            Codigo = response.IsSuccessStatusCode ? 500 : (int)response.StatusCode,
            Mensaje = ExtractErrorMessage(payload) ?? "No se pudo interpretar la respuesta de cantidad-sub.",
            CantidadSub = 0
        };
    }

    public async Task<HorasExtraNoSupervisorResponse> ObtenerNoSupervisorAsync(string noEmpleado, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(("noEmpleado", noEmpleado));
        var endpoint = $"/api/HorasExtra/no-supervisor{query}";
        using var response = await SendGetAsync(endpoint, cancellationToken);
        var payload = await ReadPayloadAsync(response, endpoint, cancellationToken);
        ThrowIfConnectivityHttpError(response, endpoint, payload);

        if (TryDeserialize<HorasExtraNoSupervisorResponse>(payload, out var data) && data is not null)
        {
            if (!response.IsSuccessStatusCode && data.Codigo == 0)
            {
                data.Codigo = (int)response.StatusCode;
                data.Mensaje = string.IsNullOrWhiteSpace(data.Mensaje)
                    ? $"Error HTTP {(int)response.StatusCode}."
                    : data.Mensaje;
            }

            return data;
        }

        return new HorasExtraNoSupervisorResponse
        {
            Codigo = response.IsSuccessStatusCode ? 500 : (int)response.StatusCode,
            Mensaje = ExtractErrorMessage(payload) ?? "No se pudo interpretar la respuesta de no-supervisor.",
            NumeroSupervisor = null
        };
    }

    public Task<IReadOnlyList<HorasExtraConsolidadaItemResponse>> ListarConsolidadasAsync(
        string? idEmpleado,
        string? fechaI,
        string? fechaF,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            ("idEmpleado", idEmpleado),
            ("fechaI", fechaI),
            ("fechaF", fechaF));

        return GetListAsync<HorasExtraConsolidadaItemResponse>($"/api/HorasExtra/consolidadas{query}", cancellationToken);
    }

    public Task<IReadOnlyList<HorasExtraConsolidadaItemResponse>> ListarConsolidadasSupervisorAsync(
        string supervisor,
        string? idEmpleado,
        string? fechaI,
        string? fechaF,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            ("supervisor", supervisor),
            ("idEmpleado", idEmpleado),
            ("fechaI", fechaI),
            ("fechaF", fechaF));

        return GetListAsync<HorasExtraConsolidadaItemResponse>($"/api/HorasExtra/consolidadas-supervisor{query}", cancellationToken);
    }

    public Task<IReadOnlyList<HorasExtraSupervisorItemResponse>> ListarListadoSupervisorAsync(
        string idSupervisor,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(("idSupervisor", idSupervisor));
        return GetListAsync<HorasExtraSupervisorItemResponse>($"/api/HorasExtra/listado-supervisor{query}", cancellationToken);
    }

    public Task<IReadOnlyList<HorasExtraSupervisorItemResponse>> ListarListadoSupervisorFiltradoAsync(
        string idSupervisor,
        string? idEmpleado,
        string? fechaI,
        string? fechaF,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            ("idSupervisor", idSupervisor),
            ("idEmpleado", idEmpleado),
            ("fechaI", fechaI),
            ("fechaF", fechaF));

        return GetListAsync<HorasExtraSupervisorItemResponse>($"/api/HorasExtra/listado-supervisor-filtrado{query}", cancellationToken);
    }

    public Task<IReadOnlyList<HorasExtraReporteHeSupervisorItemResponse>> ListarReporteHeSupervisorAsync(
        string idSupervisor,
        string? idEmpleado,
        string? fechaI,
        string? fechaF,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            ("idSupervisor", idSupervisor),
            ("idEmpleado", idEmpleado),
            ("fechaI", fechaI),
            ("fechaF", fechaF));

        return GetListAsync<HorasExtraReporteHeSupervisorItemResponse>($"/api/HorasExtra/reporte-he-supervisor{query}", cancellationToken);
    }

    public Task<IReadOnlyList<HorasExtraReporteMarcadasItemResponse>> ListarReporteMarcadasAsync(
        string? idEmpleado,
        string? fechaI,
        string? fechaF,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            ("idEmpleado", idEmpleado),
            ("fechaI", fechaI),
            ("fechaF", fechaF));

        return GetListAsync<HorasExtraReporteMarcadasItemResponse>($"/api/HorasExtra/reporte-marcadas{query}", cancellationToken);
    }

    public Task<IReadOnlyList<HorasExtraEmpleadoSuplenteItemResponse>> ListarEmpleadosSuplentesAsync(
        string idSupervisor,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(("idSupervisor", idSupervisor));
        return GetListAsync<HorasExtraEmpleadoSuplenteItemResponse>($"/api/HorasExtra/empleados-suplentes{query}", cancellationToken);
    }

    public async Task<HorasExtraSuplenteOperacionResponse> GuardarSuplenteAsync(
        HorasExtraSuplenteGuardarRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return new HorasExtraSuplenteOperacionResponse
            {
                Codigo = 400,
                Mensaje = "La solicitud es requerida.",
                Afectados = 0
            };
        }

        const string endpoint = "/api/HorasExtra/suplentes";
        using var response = await SendPostAsJsonAsync(endpoint, request, cancellationToken);
        var payload = await ReadPayloadAsync(response, endpoint, cancellationToken);
        ThrowIfConnectivityHttpError(response, endpoint, payload);

        if (TryDeserialize<HorasExtraSuplenteOperacionResponse>(payload, out var data) && data is not null)
        {
            if (!response.IsSuccessStatusCode && data.Codigo == 0)
            {
                data.Codigo = (int)response.StatusCode;
                data.Mensaje = string.IsNullOrWhiteSpace(data.Mensaje)
                    ? $"Error HTTP {(int)response.StatusCode}."
                    : data.Mensaje;
            }

            return data;
        }

        return new HorasExtraSuplenteOperacionResponse
        {
            Codigo = response.IsSuccessStatusCode ? 500 : (int)response.StatusCode,
            Mensaje = ExtractErrorMessage(payload) ?? "No se pudo interpretar la respuesta de suplentes.",
            Afectados = 0
        };
    }

    public async Task<HorasExtraSuplenteOperacionResponse> DeBajaSuplenteAsync(
        HorasExtraSuplenteDeBajaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return new HorasExtraSuplenteOperacionResponse
            {
                Codigo = 400,
                Mensaje = "La solicitud es requerida.",
                Afectados = 0
            };
        }

        const string endpoint = "/api/HorasExtra/suplentes/de-baja";
        using var response = await SendPostAsJsonAsync(endpoint, request, cancellationToken);
        var payload = await ReadPayloadAsync(response, endpoint, cancellationToken);
        ThrowIfConnectivityHttpError(response, endpoint, payload);

        if (TryDeserialize<HorasExtraSuplenteOperacionResponse>(payload, out var data) && data is not null)
        {
            if (!response.IsSuccessStatusCode && data.Codigo == 0)
            {
                data.Codigo = (int)response.StatusCode;
                data.Mensaje = string.IsNullOrWhiteSpace(data.Mensaje)
                    ? $"Error HTTP {(int)response.StatusCode}."
                    : data.Mensaje;
            }

            return data;
        }

        return new HorasExtraSuplenteOperacionResponse
        {
            Codigo = response.IsSuccessStatusCode ? 500 : (int)response.StatusCode,
            Mensaje = ExtractErrorMessage(payload) ?? "No se pudo interpretar la respuesta de baja de suplente.",
            Afectados = 0
        };
    }

    public async Task<HorasExtraAgregarResponse> AgregarHorasExtrasAsync(
        HorasExtraAgregarRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return new HorasExtraAgregarResponse
            {
                Codigo = 400,
                Mensaje = "La solicitud es requerida.",
                Resultado = 0
            };
        }

        const string endpoint = "/api/HorasExtra/agregar-horas-extras";
        using var response = await SendPostAsJsonAsync(endpoint, request, cancellationToken);
        var payload = await ReadPayloadAsync(response, endpoint, cancellationToken);
        ThrowIfConnectivityHttpError(response, endpoint, payload);

        if (TryDeserialize<HorasExtraAgregarResponse>(payload, out var data) && data is not null)
        {
            if (!response.IsSuccessStatusCode && data.Codigo == 0)
            {
                data.Codigo = (int)response.StatusCode;
                data.Mensaje = string.IsNullOrWhiteSpace(data.Mensaje)
                    ? $"Error HTTP {(int)response.StatusCode}."
                    : data.Mensaje;
            }

            return data;
        }

        return new HorasExtraAgregarResponse
        {
            Codigo = response.IsSuccessStatusCode ? 500 : (int)response.StatusCode,
            Mensaje = ExtractErrorMessage(payload) ?? "No se pudo interpretar la respuesta de actualizacion de horas extras.",
            Resultado = -1
        };
    }

    private async Task<IReadOnlyList<T>> GetListAsync<T>(string endpoint, CancellationToken cancellationToken)
    {
        using var response = await SendGetAsync(endpoint, cancellationToken);
        var payload = await ReadPayloadAsync(response, endpoint, cancellationToken);
        ThrowIfConnectivityHttpError(response, endpoint, payload);

        if (!response.IsSuccessStatusCode)
        {
            var message = ExtractErrorMessage(payload) ?? $"Error HTTP {(int)response.StatusCode} al consultar {endpoint}.";
            throw new InvalidOperationException(message);
        }

        if (TryDeserialize<List<T>>(payload, out var data) && data is not null)
        {
            return data;
        }

        return Array.Empty<T>();
    }

    private static bool TryDeserialize<T>(string payload, out T? result)
    {
        try
        {
            result = JsonSerializer.Deserialize<T>(payload, JsonOptions);
            return result is not null;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    private static string? ExtractErrorMessage(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        if (TryDeserialize<ApiErrorResponse>(payload, out var apiError) && apiError is not null)
        {
            if (!string.IsNullOrWhiteSpace(apiError.Mensaje))
            {
                return apiError.Mensaje;
            }

            if (!string.IsNullOrWhiteSpace(apiError.Message))
            {
                return apiError.Message;
            }
        }

        return null;
    }

    private static string BuildQuery(params (string Key, string? Value)[] values)
    {
        var parts = values
            .Where(v => !string.IsNullOrWhiteSpace(v.Value))
            .Select(v => $"{Uri.EscapeDataString(v.Key)}={Uri.EscapeDataString(v.Value!)}")
            .ToList();

        return parts.Count == 0 ? string.Empty : $"?{string.Join("&", parts)}";
    }

    private async Task<HttpResponseMessage> SendGetAsync(string endpoint, CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.GetAsync(endpoint, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw ApiConnectivityException.Timeout(endpoint, ex);
        }
        catch (HttpRequestException ex)
        {
            throw ApiConnectivityException.Unavailable(endpoint, ex);
        }
    }

    private async Task<HttpResponseMessage> SendPostAsJsonAsync<TPayload>(
        string endpoint,
        TPayload payload,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw ApiConnectivityException.Timeout(endpoint, ex);
        }
        catch (HttpRequestException ex)
        {
            throw ApiConnectivityException.Unavailable(endpoint, ex);
        }
    }

    private static async Task<string> ReadPayloadAsync(
        HttpResponseMessage response,
        string endpoint,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw ApiConnectivityException.Timeout(endpoint, ex);
        }
    }

    private static void ThrowIfConnectivityHttpError(HttpResponseMessage response, string endpoint, string payload)
    {
        var apiMessage = ExtractErrorMessage(payload);
        var looksLikeConnectivityMessage = IsConnectivityMessage(apiMessage) || IsConnectivityMessage(payload);
        var isConnectivityHttpStatus = IsConnectivityHttpStatus(response.StatusCode);
        var isServerErrorWithConnectivityPayload =
            response.StatusCode == HttpStatusCode.InternalServerError && looksLikeConnectivityMessage;

        if (!isConnectivityHttpStatus && !isServerErrorWithConnectivityPayload)
        {
            return;
        }

        var isTimeout = response.StatusCode == HttpStatusCode.RequestTimeout
            || response.StatusCode == HttpStatusCode.GatewayTimeout
            || IsTimeoutMessage(apiMessage)
            || IsTimeoutMessage(payload);

        if (isTimeout)
        {
            throw new ApiConnectivityException(
                endpoint,
                isTimeout: true,
                userMessage: string.IsNullOrWhiteSpace(apiMessage)
                    ? "La solicitud al servicio de horas extras excedio el tiempo de espera."
                    : apiMessage.Trim());
        }

        throw new ApiConnectivityException(
            endpoint,
            isTimeout: false,
            userMessage: string.IsNullOrWhiteSpace(apiMessage)
                ? "No se pudo conectar con el servicio de horas extras."
                : apiMessage.Trim());
    }

    private static bool IsConnectivityHttpStatus(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.RequestTimeout
            || statusCode == HttpStatusCode.BadGateway
            || statusCode == HttpStatusCode.ServiceUnavailable
            || statusCode == HttpStatusCode.GatewayTimeout;
    }

    private static bool IsConnectivityMessage(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim().ToLowerInvariant();
        return normalized.Contains("conexion")
            || normalized.Contains("conectar")
            || normalized.Contains("network")
            || normalized.Contains("failed to fetch")
            || normalized.Contains("host")
            || normalized.Contains("socket")
            || normalized.Contains("service unavailable")
            || normalized.Contains("servicio no disponible");
    }

    private static bool IsTimeoutMessage(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim().ToLowerInvariant();
        return normalized.Contains("timeout")
            || normalized.Contains("tiempo de espera")
            || normalized.Contains("agotado")
            || normalized.Contains("timed out");
    }
}
