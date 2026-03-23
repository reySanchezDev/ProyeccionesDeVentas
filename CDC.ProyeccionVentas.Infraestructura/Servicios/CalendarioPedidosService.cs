using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CDC.ProyeccionVentas.Infraestructura.Servicios
{
    /// <summary>
    /// Implementación ADO.NET basada en IConfiguration, igual que tu ProyeccionVentasConsultaService.
    /// Ejecuta SP:
    ///   - sp_Calendario_MatrizPivot    (SELECT matriz completa)
    ///   - sp_Calendario_GuardarDia     (upsert/delete + fila actualizada)
    /// </summary>
    public sealed class CalendarioPedidosService : ICalendarioPedidosService
    {
        private readonly string _connectionString;

        public CalendarioPedidosService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ReportesLS")
                                ?? throw new System.InvalidOperationException("Falta ConnectionStrings:ReportesLS");
        }

        public async Task<IReadOnlyList<CalendarioMatrizRow>> ObtenerMatrizAsync(CancellationToken ct = default)
        {
            var lista = new List<CalendarioMatrizRow>();

            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync(ct);

            using var cmd = new SqlCommand("sp_Calendario_MatrizPivot", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
            {
                var row = new CalendarioMatrizRow
                {
                    StoreNo = rd["StoreNo"].ToString() ?? string.Empty,
                    StoreName = rd["StoreName"].ToString() ?? string.Empty,
                    Lunes = GetInt(rd, "Lunes"),
                    Martes = GetInt(rd, "Martes"),
                    Miercoles = GetInt(rd, "Miercoles"),
                    Jueves = GetInt(rd, "Jueves"),
                    Viernes = GetInt(rd, "Viernes"),
                    Sabado = GetInt(rd, "Sabado"),
                    Domingo = GetInt(rd, "Domingo")
                };
                lista.Add(row);
            }

            return lista;
        }

        public async Task<CalendarioMatrizRow> GuardarDiaAsync(string storeNo, byte diaSemanaIso, bool marcado, CancellationToken ct = default)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync(ct);

            using var cmd = new SqlCommand("sp_Calendario_GuardarDia", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@StoreNo", SqlDbType.NVarChar, 20) { Value = storeNo });
            cmd.Parameters.Add(new SqlParameter("@DiaSemanaIso", SqlDbType.TinyInt) { Value = diaSemanaIso });
            cmd.Parameters.Add(new SqlParameter("@Marcado", SqlDbType.Bit) { Value = marcado });

            using var rd = await cmd.ExecuteReaderAsync(ct);
            if (await rd.ReadAsync(ct))
            {
                return new CalendarioMatrizRow
                {
                    StoreNo = rd["StoreNo"].ToString() ?? storeNo,
                    StoreName = rd.ColumnExists("StoreName") ? (rd["StoreName"].ToString() ?? string.Empty) : string.Empty,
                    Lunes = GetInt(rd, "Lunes"),
                    Martes = GetInt(rd, "Martes"),
                    Miercoles = GetInt(rd, "Miercoles"),
                    Jueves = GetInt(rd, "Jueves"),
                    Viernes = GetInt(rd, "Viernes"),
                    Sabado = GetInt(rd, "Sabado"),
                    Domingo = GetInt(rd, "Domingo")
                };
            }

            // Si por alguna razón no viene fila, devolvemos el StoreNo para que la UI no falle.
            return new CalendarioMatrizRow { StoreNo = storeNo };
        }

        private static int GetInt(IDataRecord rd, string col)
            => rd[col] is int i ? i : rd[col] is short s ? s : rd[col] is byte b ? b : 0;
    }

    internal static class DataRecordExtensions
    {
        public static bool ColumnExists(this IDataRecord dr, string columnName)
        {
            for (int i = 0; i < dr.FieldCount; i++)
                if (dr.GetName(i).Equals(columnName))
                    return true;
            return false;
        }
    }
}
