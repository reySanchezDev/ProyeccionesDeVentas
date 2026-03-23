using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class ValidarFechaRequest
    {
        public string CodSucursal { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }
}
