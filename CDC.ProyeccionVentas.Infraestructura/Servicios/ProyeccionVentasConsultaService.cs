using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Linq;

namespace CDC.ProyeccionVentas.Infraestructura.Servicios
{
    public class ProyeccionVentasConsultaService : IProyeccionVentasConsultaService
    {
        private readonly string _connectionString;

        public ProyeccionVentasConsultaService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ReportesLS")
                ?? throw new InvalidOperationException("Falta la cadena de conexión 'ReportesLS'.");
        }

        public async Task<List<ProyeccionVentasToConsulta>> ObtenerProyeccionesFiltradasAsync(FiltroProyeccionVentas filtro)
        {
            var resultado = new List<ProyeccionVentasToConsulta>();
            var codSucursales = (filtro.CodSucursales ?? new List<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!codSucursales.Any() && !string.IsNullOrWhiteSpace(filtro.CodSucursal))
            {
                codSucursales.Add(filtro.CodSucursal.Trim());
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT Id, Fecha, CodSucursal, Monto, TicketPromedio
                    FROM dbo.ProyeccionVentas 
                    WHERE Fecha >= @FechaInicio AND Fecha <= @FechaFin
                ";

                if (codSucursales.Any())
                {
                    var parametrosSucursales = codSucursales
                        .Select((_, index) => $"@CodSucursal{index}")
                        .ToList();

                    query += $" AND CodSucursal IN ({string.Join(", ", parametrosSucursales)})";
                }

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FechaInicio", filtro.FechaInicio);
                    command.Parameters.AddWithValue("@FechaFin", filtro.FechaFin);

                    for (var index = 0; index < codSucursales.Count; index++)
                    {
                        command.Parameters.AddWithValue($"@CodSucursal{index}", codSucursales[index]);
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            resultado.Add(new ProyeccionVentasToConsulta
                            {
                                Id = reader.GetInt32(0),
                                Fecha = reader.GetDateTime(1),
                                CodSucursal = reader.GetString(2),
                                Monto = reader.GetDecimal(3),
                                TicketPromedio = reader.IsDBNull(4) ? null : reader.GetInt32(4)
                            });
                        }
                    }
                }
            }

            return resultado;
        }

        public async Task GuardarCambiosAsync(List<ActualizarProyeccionDto> cambios)
        {
            if (cambios == null || !cambios.Any())
                return;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                foreach (var item in cambios)
                {
                    var query = "UPDATE dbo.ProyeccionVentas SET Monto = @Monto, TicketPromedio = @TicketPromedio, FechaAudit = GETDATE() WHERE Id = @Id";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Monto", item.Monto);
                        command.Parameters.Add("@TicketPromedio", SqlDbType.Int).Value = (object?)item.TicketPromedio ?? DBNull.Value;
                        command.Parameters.AddWithValue("@Id", item.Id);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public async Task<EliminarProyeccionVentasResult> EliminarPorRangoAsync(EliminarProyeccionVentasRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.FechaInicio == default || request.FechaFin == default)
                throw new ArgumentException("FechaInicio y FechaFin son obligatorias.");

            if (request.FechaInicio.Date > request.FechaFin.Date)
                throw new ArgumentException("FechaInicio no puede ser mayor que FechaFin.");

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("dbo.sp_ProyeccionVentas_EliminarPorRangoFecha", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("@FechaInicio", SqlDbType.Date).Value = request.FechaInicio.Date;
            command.Parameters.Add("@FechaFin", SqlDbType.Date).Value = request.FechaFin.Date;

            var resultado = await command.ExecuteScalarAsync();
            var registrosEliminados = resultado == null || resultado == DBNull.Value
                ? 0
                : Convert.ToInt32(resultado);

            return new EliminarProyeccionVentasResult
            {
                RegistrosEliminados = registrosEliminados,
                Mensaje = registrosEliminados > 0
                    ? $"Se eliminaron {registrosEliminados} registros del período seleccionado."
                    : "No se encontraron registros para eliminar en el período seleccionado."
            };
        }

    }
}
