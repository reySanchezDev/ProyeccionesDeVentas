using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.Infraestructura.Repositorios
{
    public class ValidarFechasRepository : IValidarFechasRepository
    {
        private readonly string _connectionString;

        public ValidarFechasRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<ValidarFechaRequest>> ObtenerFechasYSucursalesExistentesAsync()
        {
            var resultados = new List<ValidarFechaRequest>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                SELECT Fecha, CodSucursal
                FROM dbo.ProyeccionVentas
                WHERE MONTH(Fecha) = MONTH(GETDATE()) AND YEAR(Fecha) = YEAR(GETDATE())
            ", connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                resultados.Add(new ValidarFechaRequest
                {
                    Fecha = reader.GetDateTime(0),
                    CodSucursal = reader.GetString(1)
                });
            }

            return resultados;
        }

        public async Task<List<ValidarFechaRequest>> FiltrarExistentesAsync(List<ValidarFechaRequest> datosArchivo)
        {
            var resultados = new List<ValidarFechaRequest>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var item in datosArchivo)
            {
                using var command = new SqlCommand(@"
                    SELECT 1
                    FROM dbo.ProyeccionVentas
                    WHERE CodSucursal = @CodSucursal AND Fecha = @Fecha", connection);

                command.Parameters.AddWithValue("@CodSucursal", item.CodSucursal);
                command.Parameters.AddWithValue("@Fecha", item.Fecha);

                var existe = await command.ExecuteScalarAsync();
                if (existe != null)
                {
                    resultados.Add(item);
                }
            }

            return resultados;
        }
    }
}
