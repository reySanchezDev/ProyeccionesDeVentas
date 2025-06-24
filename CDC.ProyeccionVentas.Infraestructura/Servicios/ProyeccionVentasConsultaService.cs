using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace CDC.ProyeccionVentas.Infraestructura.Servicios
{
    public class ProyeccionVentasConsultaService : IProyeccionVentasConsultaService
    {
        private readonly IConfiguration _configuration;

        public ProyeccionVentasConsultaService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<ProyeccionVentasToConsulta>> ObtenerProyeccionesFiltradasAsync(FiltroProyeccionVentas filtro)
        {
            var resultado = new List<ProyeccionVentasToConsulta>();

            string connectionString = _configuration.GetConnectionString("ReportesLS");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT Id, Fecha, CodSucursal, Monto 
                    FROM dbo.ProyeccionVentas 
                    WHERE Fecha >= @FechaInicio AND Fecha <= @FechaFin
                ";

                if (!string.IsNullOrEmpty(filtro.CodSucursal))
                {
                    query += " AND CodSucursal = @CodSucursal";
                }

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FechaInicio", filtro.FechaInicio);
                    command.Parameters.AddWithValue("@FechaFin", filtro.FechaFin);

                    if (!string.IsNullOrEmpty(filtro.CodSucursal))
                    {
                        command.Parameters.AddWithValue("@CodSucursal", filtro.CodSucursal);
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
                                Monto = reader.GetDecimal(3)
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

            string connectionString = _configuration.GetConnectionString("ReportesLS");

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                foreach (var item in cambios)
                {
                    var query = "UPDATE dbo.ProyeccionVentas SET Monto = @Monto, FechaAudit = GETDATE() WHERE Id = @Id";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Monto", item.Monto);
                        command.Parameters.AddWithValue("@Id", item.Id);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
        }



    }
}