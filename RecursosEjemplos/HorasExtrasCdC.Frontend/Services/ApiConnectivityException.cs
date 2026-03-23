namespace HorasExtrasCdC.Frontend.Services;

public sealed class ApiConnectivityException : Exception
{
    public ApiConnectivityException(
        string endpoint,
        bool isTimeout,
        string userMessage,
        Exception? innerException = null)
        : base(userMessage, innerException)
    {
        Endpoint = endpoint;
        IsTimeout = isTimeout;
        UserMessage = userMessage;
    }

    public string Endpoint { get; }

    public bool IsTimeout { get; }

    public string UserMessage { get; }

    public static ApiConnectivityException Timeout(string endpoint, Exception? innerException = null)
    {
        return new ApiConnectivityException(
            endpoint,
            isTimeout: true,
            userMessage: "La solicitud al servicio de horas extras excedio el tiempo de espera.",
            innerException);
    }

    public static ApiConnectivityException Unavailable(string endpoint, Exception? innerException = null)
    {
        return new ApiConnectivityException(
            endpoint,
            isTimeout: false,
            userMessage: "No se pudo conectar con el servicio de horas extras.",
            innerException);
    }
}
