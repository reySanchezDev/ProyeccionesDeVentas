using CDC.ProyeccionVentas.Dominio.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDC.ProyeccionVentas.HttpClients.Interfaces
{
    public interface IProyeccionVentasConsultaHttpClient
    {
        Task<List<ProyeccionVentasToConsulta>> ObtenerFiltradoAsync(FiltroProyeccionVentas filtro);

        Task<bool> GuardarCambiosAsync(List<ActualizarProyeccionDto> cambios);

        Task<EliminarProyeccionVentasResult> EliminarPorRangoAsync(EliminarProyeccionVentasRequest request);
    }
}
