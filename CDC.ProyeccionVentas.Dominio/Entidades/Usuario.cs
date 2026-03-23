using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.Dominio.Entidades
{
    public class Usuario
    {
        public string NumeroEmpleado { get; set; } = string.Empty;
        public string Contrasenia { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}
