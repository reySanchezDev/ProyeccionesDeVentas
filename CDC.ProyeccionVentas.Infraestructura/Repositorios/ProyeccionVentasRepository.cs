using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.Infraestructura.Repositorios
{
    public class ProyeccionVentasRepository : IProyeccionVentasRepository
    {
        private readonly string _connectionString;

        public ProyeccionVentasRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task InsertarProyeccionesAsync(IEnumerable<ProyeccionVentaDto> proyecciones)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var item in proyecciones)
            {
                var command = new SqlCommand(@"
                    INSERT INTO dbo.ProyeccionVentas (Fecha, CodSucursal, Monto, FechaAudit)
                    VALUES (@Fecha, @CodSucursal, @Monto, GETDATE())
                ", connection);

                command.Parameters.AddWithValue("@Fecha", item.Fecha);
                command.Parameters.AddWithValue("@CodSucursal", item.CodSucursal);
                command.Parameters.AddWithValue("@Monto", item.Monto);

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
