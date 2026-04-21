using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;

namespace CDC.ProyeccionVentas.Infraestructura.Servicios
{
    public class TransaccionSucursalService : ITransaccionSucursalService
    {
        private readonly string _reportesLsConnectionString;

        public TransaccionSucursalService(string reportesLsConnectionString)
        {
            _reportesLsConnectionString = reportesLsConnectionString;
        }

        public async Task<List<TransaccionSucursalDownloadItem>> DescargarPlantillaAsync(List<string> codSucursales)
        {
            var resultado = new List<TransaccionSucursalDownloadItem>();
            var codigosNormalizados = (codSucursales ?? new List<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_ProyeccionTransaccionesSucursal_DescargarPlantillaBase", connection)
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
                resultado.Add(new TransaccionSucursalDownloadItem
                {
                    CodSucursal = GetString(reader, "CodSucursal"),
                    NombreSucursal = GetString(reader, "NombreSucursal"),
                    TransaccionProyectada = GetNullableDecimal(reader, "TransaccionProyectada")
                });
            }

            return resultado;
        }

        public async Task<List<TransaccionSucursalExistingItem>> FiltrarExistentesCargaAsync(List<TransaccionSucursalBulkUploadItem> items)
        {
            var resultado = new List<TransaccionSucursalExistingItem>();
            var tabla = BuildUploadDataTable(items);

            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_ProyeccionTransaccionesSucursal_FiltrarExistentesCarga", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            var itemsParameter = command.Parameters.AddWithValue("@Items", tabla);
            itemsParameter.SqlDbType = SqlDbType.Structured;
            itemsParameter.TypeName = "dbo.ProyeccionTransaccionesSucursalCargaMensualType";

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                resultado.Add(new TransaccionSucursalExistingItem
                {
                    Mes = GetInt32(reader, "Mes"),
                    Ano = GetInt32(reader, "Ano"),
                    CodSucursal = GetString(reader, "CodSucursal")
                });
            }

            return resultado;
        }

        public async Task<TransaccionSucursalBulkUploadResult> InsertarMasivoAsync(TransaccionSucursalBulkUploadRequest request)
        {
            var tabla = BuildUploadDataTable(request.Items);

            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_ProyeccionTransaccionesSucursal_InsertarMasivo", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("@CodigoEmpleadoAccion", SqlDbType.VarChar, 20).Value = request.CodigoEmpleadoAccion.Trim();
            var itemsParameter = command.Parameters.AddWithValue("@Items", tabla);
            itemsParameter.SqlDbType = SqlDbType.Structured;
            itemsParameter.TypeName = "dbo.ProyeccionTransaccionesSucursalCargaMensualType";

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException("La carga masiva no devolvió resultado.");
            }

            return new TransaccionSucursalBulkUploadResult
            {
                RegistrosInsertados = GetInt32(reader, "RegistrosInsertados"),
                Mensaje = GetString(reader, "Mensaje")
            };
        }

        public async Task<List<TransaccionSucursalItem>> ConsultarAsync(TransaccionSucursalConsultaFilter filter)
        {
            var resultado = new List<TransaccionSucursalItem>();
            var codigosNormalizados = (filter.CodSucursales ?? new List<string>())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim().ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_ProyeccionTransaccionesSucursal_Consultar", connection)
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
                resultado.Add(new TransaccionSucursalItem
                {
                    Id = GetInt32(reader, "Id"),
                    Fecha = reader.GetDateTime(reader.GetOrdinal("Fecha")),
                    Mes = GetInt32(reader, "Mes"),
                    Ano = GetInt32(reader, "Ano"),
                    CodSucursal = GetString(reader, "CodSucursal"),
                    NombreSucursal = GetString(reader, "NombreSucursal"),
                    ExisteEnCatalogo = GetBoolean(reader, "ExisteEnCatalogo"),
                    TransaccionProyectada = GetDecimal(reader, "TransaccionProyectada")
                });
            }

            return resultado;
        }

        public async Task<TransaccionSucursalSaveResponse> ActualizarAsync(TransaccionSucursalSaveRequest request)
        {
            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_ProyeccionTransaccionesSucursal_Actualizar", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("@Id", SqlDbType.Int).Value = request.Id;
            var transaccionParameter = command.Parameters.Add("@TransaccionProyectada", SqlDbType.Decimal);
            transaccionParameter.Precision = 10;
            transaccionParameter.Scale = 2;
            transaccionParameter.Value = request.TransaccionProyectada < 0 ? 0 : request.TransaccionProyectada;
            command.Parameters.Add("@CodigoEmpleadoAccion", SqlDbType.VarChar, 20).Value = request.CodigoEmpleadoAccion.Trim();

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException("La actualización no devolvió resultado.");
            }

            var accion = GetString(reader, "Accion");
            return new TransaccionSucursalSaveResponse
            {
                Accion = accion,
                Id = GetInt32(reader, "Id"),
                TransaccionProyectada = GetDecimal(reader, "TransaccionProyectada"),
                UltimoValorTransaccionProyectada = GetNullableDecimal(reader, "UltimoValorTransaccionProyectada"),
                Mensaje = accion == "NOCHANGE"
                    ? "No hubo cambios para guardar."
                    : "Transacción proyectada actualizada correctamente."
            };
        }

        public async Task<TransaccionSucursalDeleteMonthResult> EliminarMesAsync(TransaccionSucursalDeleteMonthRequest request)
        {
            using var connection = new SqlConnection(_reportesLsConnectionString);
            using var command = new SqlCommand("dbo.sp_ProyeccionTransaccionesSucursal_EliminarMes", connection)
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

            return new TransaccionSucursalDeleteMonthResult
            {
                RegistrosEliminados = GetInt32(reader, "RegistrosEliminados"),
                Mensaje = GetString(reader, "Mensaje")
            };
        }

        private static DataTable BuildUploadDataTable(IEnumerable<TransaccionSucursalBulkUploadItem>? items)
        {
            var table = new DataTable();
            table.Columns.Add("CodSucursal", typeof(string));
            table.Columns.Add("TransaccionProyectada", typeof(decimal));

            if (items is null)
            {
                return table;
            }

            foreach (var item in items)
            {
                var codSucursal = item.CodSucursal?.Trim() ?? string.Empty;
                var transaccionProyectada = item.TransaccionProyectada < 0 ? 0 : item.TransaccionProyectada;
                table.Rows.Add(codSucursal, transaccionProyectada);
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

        private static decimal GetDecimal(SqlDataReader reader, string columnName)
        {
            return reader[columnName] == DBNull.Value
                ? 0
                : Convert.ToDecimal(reader[columnName]);
        }

        private static decimal? GetNullableDecimal(SqlDataReader reader, string columnName)
        {
            return reader[columnName] == DBNull.Value
                ? null
                : Convert.ToDecimal(reader[columnName]);
        }

        private static bool GetBoolean(SqlDataReader reader, string columnName)
        {
            return reader[columnName] != DBNull.Value && Convert.ToBoolean(reader[columnName]);
        }
    }
}
