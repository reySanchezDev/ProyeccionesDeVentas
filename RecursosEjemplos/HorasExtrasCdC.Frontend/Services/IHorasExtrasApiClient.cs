using HorasExtrasCdC.Frontend.Models;

namespace HorasExtrasCdC.Frontend.Services;

public interface IHorasExtrasApiClient
{
    Task<HorasExtraUsuarioResponse> ObtenerUsuarioAsync(string numeroEmpleado, CancellationToken cancellationToken = default);
    Task<HorasExtraRolesUsuarioResponse> ObtenerRolesAsync(string nombreUsuario, CancellationToken cancellationToken = default);
    Task<HorasExtraUsuarioExisteResponse> UsuarioExisteAsync(string noEmpleado, CancellationToken cancellationToken = default);
    Task<HorasExtraCantidadSubResponse> ObtenerCantidadSubAsync(string noEmpleado, CancellationToken cancellationToken = default);
    Task<HorasExtraNoSupervisorResponse> ObtenerNoSupervisorAsync(string noEmpleado, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HorasExtraConsolidadaItemResponse>> ListarConsolidadasAsync(
        string? idEmpleado,
        string? fechaI,
        string? fechaF,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HorasExtraConsolidadaItemResponse>> ListarConsolidadasSupervisorAsync(
        string supervisor,
        string? idEmpleado,
        string? fechaI,
        string? fechaF,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HorasExtraSupervisorItemResponse>> ListarListadoSupervisorAsync(
        string idSupervisor,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HorasExtraSupervisorItemResponse>> ListarListadoSupervisorFiltradoAsync(
        string idSupervisor,
        string? idEmpleado,
        string? fechaI,
        string? fechaF,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HorasExtraReporteHeSupervisorItemResponse>> ListarReporteHeSupervisorAsync(
        string idSupervisor,
        string? idEmpleado,
        string? fechaI,
        string? fechaF,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HorasExtraReporteMarcadasItemResponse>> ListarReporteMarcadasAsync(
        string? idEmpleado,
        string? fechaI,
        string? fechaF,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HorasExtraEmpleadoSuplenteItemResponse>> ListarEmpleadosSuplentesAsync(
        string idSupervisor,
        CancellationToken cancellationToken = default);
    Task<HorasExtraSuplenteOperacionResponse> GuardarSuplenteAsync(
        HorasExtraSuplenteGuardarRequest request,
        CancellationToken cancellationToken = default);
    Task<HorasExtraSuplenteOperacionResponse> DeBajaSuplenteAsync(
        HorasExtraSuplenteDeBajaRequest request,
        CancellationToken cancellationToken = default);
    Task<HorasExtraAgregarResponse> AgregarHorasExtrasAsync(
        HorasExtraAgregarRequest request,
        CancellationToken cancellationToken = default);
}
