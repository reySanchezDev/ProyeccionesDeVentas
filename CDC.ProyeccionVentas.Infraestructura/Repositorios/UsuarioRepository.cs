using CDC.ProyeccionVentas.Dominio.Entidades;
using CDC.ProyeccionVentas.Dominio.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.Infraestructura.Repositorios
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly string _connectionString;

        public UsuarioRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Usuario> ObtenerUsuarioPorNumeroEmpleadoAsync(string numeroEmpleado)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"SELECT [NumeroEmpleado], [contrasenia], [salt], [correo], [Activo]
                              FROM [dbo].[Usuarios]
                              WHERE [NumeroEmpleado] = @NumeroEmpleado And Activo= 1";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NumeroEmpleado", numeroEmpleado);

                    using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow))
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Usuario
                            {
                                NumeroEmpleado = reader["NumeroEmpleado"].ToString(),
                                Contrasenia = reader["contrasenia"].ToString(),
                                Salt = reader["salt"].ToString(),
                                Correo = reader["correo"].ToString(),
                                Activo = (bool)reader["Activo"]
                            };
                        }
                    }
                }
            }
            return null;
        }

    }


}