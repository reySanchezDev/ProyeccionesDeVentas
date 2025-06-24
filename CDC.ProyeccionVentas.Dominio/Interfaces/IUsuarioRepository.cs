using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CDC.ProyeccionVentas.Dominio.Entidades;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.Dominio.Interfaces
{
    public interface IUsuarioRepository
    {
        Task<Usuario> ObtenerUsuarioPorNumeroEmpleadoAsync(string numeroEmpleado);

    }
}
