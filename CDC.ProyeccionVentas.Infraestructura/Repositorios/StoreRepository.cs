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
    public class StoreRepository : IStoreRepository
    {
        private readonly string _connectionString;

        public StoreRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<Store>> ObtenerStoresAsync()
        {
            var stores = new List<Store>();

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("SELECT [No], [StoreNo] FROM Stores", connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                stores.Add(new Store
                {
                    No = reader["No"]?.ToString() ?? string.Empty,
                    StoreNo = reader["StoreNo"]?.ToString() ?? string.Empty
                });
            }

            return stores;
        }
    }
}
