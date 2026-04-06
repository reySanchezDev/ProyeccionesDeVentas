using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;

namespace CDC.ProyeccionVentas.Infraestructura.Servicios
{
    public class TicketStaffService : ITicketStaffService
    {
        private readonly string _reportesLsConnectionString;

        public TicketStaffService(string reportesLsConnectionString)
        {
            _reportesLsConnectionString = reportesLsConnectionString;
        }

        public async Task<List<string>> ObtenerCatalogoPuestosAsync()
        {
            var resultado = new List<string>();

            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_catologoPuesto", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                resultado.Add(GetString(reader, "Puesto"));
            }

            return resultado
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<List<TicketStaffDownloadItem>> DescargarPlantillaAsync(List<string> puestos)
        {
            var resultado = new List<TicketStaffDownloadItem>();
            var puestosNormalizados = (puestos ?? new List<string>())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_ProyeccionTicketStaff_DescargarStaffBase", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("@Puestos", SqlDbType.NVarChar, -1).Value =
                puestosNormalizados.Count == 0
                    ? DBNull.Value
                    : string.Join(",", puestosNormalizados);

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                resultado.Add(new TicketStaffDownloadItem
                {
                    NumeroEmpleado = GetString(reader, "NumeroEmpleado"),
                    NombreStaff = GetString(reader, "NombreStaff"),
                    Puesto = GetString(reader, "Puesto"),
                    Ubicacion = GetString(reader, "Ubicacion"),
                    TicketPromedio = GetNullableInt32(reader, "TicketPromedio")
                });
            }

            return resultado;
        }

        public async Task<List<TicketStaffExistingItem>> FiltrarExistentesCargaAsync(List<TicketStaffBulkUploadItem> items)
        {
            var resultado = new List<TicketStaffExistingItem>();
            var tabla = BuildUploadDataTable(items);

            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_ProyeccionTicketStaff_FiltrarExistentesCarga", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            var itemsParameter = command.Parameters.AddWithValue("@Items", tabla);
            itemsParameter.SqlDbType = SqlDbType.Structured;
            itemsParameter.TypeName = "dbo.ProyeccionTicketStaffCargaMensualType";

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                resultado.Add(new TicketStaffExistingItem
                {
                    Mes = GetInt32(reader, "Mes"),
                    Ano = GetInt32(reader, "Ano"),
                    NumeroEmpleado = GetString(reader, "NumeroEmpleado")
                });
            }

            return resultado;
        }

        public async Task<TicketStaffBulkUploadResult> InsertarMasivoAsync(TicketStaffBulkUploadRequest request)
        {
            var tabla = BuildUploadDataTable(request.Items);

            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_ProyeccionTicketStaff_InsertarMasivo", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("@CodigoEmpleadoAccion", SqlDbType.VarChar, 20).Value = request.CodigoEmpleadoAccion.Trim();
            var itemsParameter = command.Parameters.AddWithValue("@Items", tabla);
            itemsParameter.SqlDbType = SqlDbType.Structured;
            itemsParameter.TypeName = "dbo.ProyeccionTicketStaffCargaMensualType";

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException("La carga masiva no devolvió resultado.");
            }

            return new TicketStaffBulkUploadResult
            {
                RegistrosInsertados = GetInt32(reader, "RegistrosInsertados"),
                Mensaje = GetString(reader, "Mensaje")
            };
        }

        public async Task<List<TicketStaffItem>> ConsultarAsync(TicketStaffConsultaFilter filter)
        {
            var resultado = new List<TicketStaffItem>();

            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_ProyeccionTicketStaff_Consultar", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("@Mes", SqlDbType.Int).Value = filter.Mes;
            command.Parameters.Add("@Ano", SqlDbType.Int).Value = filter.Ano;
            command.Parameters.Add("@NumeroEmpleado", SqlDbType.VarChar, 20).Value =
                string.IsNullOrWhiteSpace(filter.NumeroEmpleado)
                    ? DBNull.Value
                    : filter.NumeroEmpleado.Trim();

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                resultado.Add(new TicketStaffItem
                {
                    Id = GetInt32(reader, "Id"),
                    Fecha = reader.GetDateTime(reader.GetOrdinal("Fecha")),
                    Mes = GetInt32(reader, "Mes"),
                    Ano = GetInt32(reader, "Ano"),
                    NumeroEmpleado = GetString(reader, "NumeroEmpleado"),
                    NombreStaff = GetString(reader, "NombreStaff"),
                    ExisteEnCDC = GetBoolean(reader, "ExisteEnCDC"),
                    TicketPromedio = GetInt32(reader, "TicketPromedio")
                });
            }

            return resultado;
        }

        public async Task<TicketStaffSaveResponse> ActualizarAsync(TicketStaffSaveRequest request)
        {
            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_ProyeccionTicketStaff_Actualizar", connection)
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
            return new TicketStaffSaveResponse
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

        public async Task<TicketStaffDeleteMonthResult> EliminarMesAsync(TicketStaffDeleteMonthRequest request)
        {
            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_ProyeccionTicketStaff_EliminarMes", connection)
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

            return new TicketStaffDeleteMonthResult
            {
                RegistrosEliminados = GetInt32(reader, "RegistrosEliminados"),
                Mensaje = GetString(reader, "Mensaje")
            };
        }

        private static DataTable BuildUploadDataTable(IEnumerable<TicketStaffBulkUploadItem>? items)
        {
            var table = new DataTable();
            table.Columns.Add("NumeroEmpleado", typeof(string));
            table.Columns.Add("TicketPromedio", typeof(int));

            if (items is null)
            {
                return table;
            }

            foreach (var item in items)
            {
                var numeroEmpleado = item.NumeroEmpleado?.Trim() ?? string.Empty;
                var ticketPromedio = item.TicketPromedio < 0 ? 0 : item.TicketPromedio;
                table.Rows.Add(numeroEmpleado, ticketPromedio);
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
