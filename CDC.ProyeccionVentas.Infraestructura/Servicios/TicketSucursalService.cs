using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;

namespace CDC.ProyeccionVentas.Infraestructura.Servicios
{
    public class TicketSucursalService : ITicketSucursalService
    {
        private readonly string _reportesLsConnectionString;

        public TicketSucursalService(string reportesLsConnectionString)
        {
            _reportesLsConnectionString = reportesLsConnectionString;
        }

        public async Task<List<TicketSucursalDownloadItem>> DescargarPlantillaAsync(List<string> codSucursales)
        {
            var resultado = new List<TicketSucursalDownloadItem>();
            var codigosNormalizados = (codSucursales ?? new List<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_ProyeccionTicketSucursal_DescargarPlantillaBase", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("@CodSucursales", SqlDbType.NVarChar, -1).Value =
                codigosNormalizados.Count == 0
                    ? DBNull.Value
                    : string.Join(",", codigosNormalizados);

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                resultado.Add(new TicketSucursalDownloadItem
                {
                    CodSucursal = GetString(reader, "CodSucursal"),
                    NombreSucursal = GetString(reader, "NombreSucursal"),
                    TicketPromedio = GetNullableInt32(reader, "TicketPromedio")
                });
            }

            return resultado;
        }

        public async Task<List<TicketSucursalExistingItem>> FiltrarExistentesCargaAsync(List<TicketSucursalBulkUploadItem> items)
        {
            var resultado = new List<TicketSucursalExistingItem>();
            var tabla = BuildUploadDataTable(items);

            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_ProyeccionTicketSucursal_FiltrarExistentesCarga", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            var itemsParameter = command.Parameters.AddWithValue("@Items", tabla);
            itemsParameter.SqlDbType = SqlDbType.Structured;
            itemsParameter.TypeName = "dbo.ProyeccionTicketSucursalCargaMensualType";

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                resultado.Add(new TicketSucursalExistingItem
                {
                    Mes = GetInt32(reader, "Mes"),
                    Ano = GetInt32(reader, "Ano"),
                    CodSucursal = GetString(reader, "CodSucursal")
                });
            }

            return resultado;
        }

        public async Task<TicketSucursalBulkUploadResult> InsertarMasivoAsync(TicketSucursalBulkUploadRequest request)
        {
            var tabla = BuildUploadDataTable(request.Items);

            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_ProyeccionTicketSucursal_InsertarMasivo", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("@CodigoEmpleadoAccion", SqlDbType.VarChar, 20).Value = request.CodigoEmpleadoAccion.Trim();
            var itemsParameter = command.Parameters.AddWithValue("@Items", tabla);
            itemsParameter.SqlDbType = SqlDbType.Structured;
            itemsParameter.TypeName = "dbo.ProyeccionTicketSucursalCargaMensualType";

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException("La carga masiva no devolvió resultado.");
            }

            return new TicketSucursalBulkUploadResult
            {
                RegistrosInsertados = GetInt32(reader, "RegistrosInsertados"),
                Mensaje = GetString(reader, "Mensaje")
            };
        }

        public async Task<List<TicketSucursalItem>> ConsultarAsync(TicketSucursalConsultaFilter filter)
        {
            var resultado = new List<TicketSucursalItem>();
            var codigosNormalizados = (filter.CodSucursales ?? new List<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim().ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_ProyeccionTicketSucursal_Consultar", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("@Mes", SqlDbType.Int).Value = filter.Mes;
            command.Parameters.Add("@Ano", SqlDbType.Int).Value = filter.Ano;
            command.Parameters.Add("@CodSucursales", SqlDbType.NVarChar, -1).Value =
                codigosNormalizados.Count == 0
                    ? DBNull.Value
                    : string.Join(",", codigosNormalizados);

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                resultado.Add(new TicketSucursalItem
                {
                    Id = GetInt32(reader, "Id"),
                    Fecha = reader.GetDateTime(reader.GetOrdinal("Fecha")),
                    Mes = GetInt32(reader, "Mes"),
                    Ano = GetInt32(reader, "Ano"),
                    CodSucursal = GetString(reader, "CodSucursal"),
                    NombreSucursal = GetString(reader, "NombreSucursal"),
                    ExisteEnCatalogo = GetBoolean(reader, "ExisteEnCatalogo"),
                    TicketPromedio = GetInt32(reader, "TicketPromedio")
                });
            }

            return resultado;
        }

        public async Task<TicketSucursalSaveResponse> ActualizarAsync(TicketSucursalSaveRequest request)
        {
            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_ProyeccionTicketSucursal_Actualizar", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("@Id", SqlDbType.Int).Value = request.Id;
            command.Parameters.Add("@TicketPromedio", SqlDbType.Int).Value = request.TicketPromedio < 0 ? 0 : request.TicketPromedio;
            command.Parameters.Add("@CodigoEmpleadoAccion", SqlDbType.VarChar, 20).Value = request.CodigoEmpleadoAccion.Trim();

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException("La actualización no devolvió resultado.");
            }

            var accion = GetString(reader, "Accion");
            return new TicketSucursalSaveResponse
            {
                Accion = accion,
                Id = GetInt32(reader, "Id"),
                TicketPromedio = GetInt32(reader, "TicketPromedio"),
                UltimoValorTicketPromedio = reader["UltimoValorTicketPromedio"] == DBNull.Value
                    ? null
                    : Convert.ToInt32(reader["UltimoValorTicketPromedio"]),
                Mensaje = accion == "NOCHANGE"
                    ? "No hubo cambios para guardar."
                    : "Ticket promedio actualizado correctamente."
            };
        }

        public async Task<TicketSucursalDeleteMonthResult> EliminarMesAsync(TicketSucursalDeleteMonthRequest request)
        {
            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_ProyeccionTicketSucursal_EliminarMes", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("@Mes", SqlDbType.Int).Value = request.Mes;
            command.Parameters.Add("@Ano", SqlDbType.Int).Value = request.Ano;

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException("La eliminación no devolvió resultado.");
            }

            return new TicketSucursalDeleteMonthResult
            {
                RegistrosEliminados = GetInt32(reader, "RegistrosEliminados"),
                Mensaje = GetString(reader, "Mensaje")
            };
        }

        private static DataTable BuildUploadDataTable(IEnumerable<TicketSucursalBulkUploadItem>? items)
        {
            var table = new DataTable();
            table.Columns.Add("CodSucursal", typeof(string));
            table.Columns.Add("TicketPromedio", typeof(int));

            if (items is null)
            {
                return table;
            }

            foreach (var item in items)
            {
                var codSucursal = item.CodSucursal?.Trim() ?? string.Empty;
                var ticketPromedio = item.TicketPromedio < 0 ? 0 : item.TicketPromedio;
                table.Rows.Add(codSucursal, ticketPromedio);
            }

            return table;
        }

        private static string GetString(SqlDataReader reader, string columnName)
        {
            return reader[columnName] == DBNull.Value
                ? string.Empty
                : reader[columnName]?.ToString()?.Trim() ?? string.Empty;
        }

        private static int GetInt32(SqlDataReader reader, string columnName)
        {
            return reader[columnName] == DBNull.Value
                ? 0
                : Convert.ToInt32(reader[columnName]);
        }

        private static int? GetNullableInt32(SqlDataReader reader, string columnName)
        {
            return reader[columnName] == DBNull.Value
                ? null
                : Convert.ToInt32(reader[columnName]);
        }

        private static bool GetBoolean(SqlDataReader reader, string columnName)
        {
            return reader[columnName] != DBNull.Value && Convert.ToBoolean(reader[columnName]);
        }
    }
}
