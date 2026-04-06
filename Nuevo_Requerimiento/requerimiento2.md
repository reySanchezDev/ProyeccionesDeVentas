Requerimientos de mejoras por etapas

Objetivo del cambio: Actualmente en la pantalla Subir Proyección de Ventas se graban proyecciones de ticket por sucursal, pero se estan haciendo por día. En Este cambio vamos a separarlo , es decir ya no se van a guardar desde ahi las proyecciones de ticket , ya que, lo haremos desde otro modulo.

ETAPA 1:

1. En la pantalla Subir Proyección de Ventas, quitar permanentemente el campo "TicketPromedio" , entonces a la hora de guardar ya no manderemos ese campo

2. Quitar el campo TicketPromedio de la tabla de ProyeccionVentas  y de todos los sp al insertar o actualizar.

3. quitar toda referencia del campo "TicketPromedio" en la pantalla Consulta de Proyecciones, esto aplica para sp de consultas y a nivel de interfaces.

4. todos los cambio que se hagan en sql, deberas de dejarmelo en un archivo .sql el cual pueda yo ir a produccion y ajecutar para actualizar o mas bien para migrar los cambios de mi local al servidor de produccion.

ETAPA 2:

1. en la pantalla principal en el menu, reemplazar TICKET PROMEDIO POR STAFF x TICKET PROMEDIO
2. en la pantalla principal en el menu, reemplazar Subir Tickets x Subir Tickets por staff
3. en la pantalla principal en el menu, reemplazar Consultar Tickets x Consultar Tickets por staff
4. Agregar 2 nuevas opciones dentro de este menu:"TICKET PROMEDIO"
OPCION 1: Subir Tickets por Sucusal
OPCION 2: Consultar Tickets por Sucursal

5. Crear una nueva pantalla para subir las proyecciones de tickets por sucursales, esta pantalla debe ser semejante a la pantalla "Subir Ticket Promedio Staff" 

Caracteristicas de esta nueva pantalla, debe contener los siguiente :

1. una opcion  de carga masiva similar a la que tiene la pantalla:"Subir Ticket Promedio Staff"
2. una opcion(combobox) que cargue las sucursales , este campo debe ser opcional, similar el campo Sucursal (opcional) que esta en la pantalla Consulta de Proyecciones y debe usar la misma fuente de datos.
3. una opcion para descargar la plantilla
4. Crear una tabla en SQL para almacenar las proyecciones de ticket promedio por sucursales similar a la tabla que se creo para guardar proyecciones de ticket por staff
4. la plantilla debe tener las siguiente columnas : CodSucursal, TicketPromedio
5. una opcion para leer archivo y validar
6. una opcion para Guardar en BD
7. una opcion para limpiar
8. las validaciones al leer el archivo deben ser: no numeros negativos, no filas vacias, tener las columnas minimas que se esperan
9. las validaciones al guardar en bd deben ser: validar las suvursales de la misma forma en la que se hacen en la pantalla "Subir Proyección de Ventas"


ETAPA 3:
1. crear una nueva pantalla para consultar y darle mantenimiento a las proyecciones de tickets promedio por sucursales similar a la pantalla "Consultar Ticket Promedio Staff" pero enfocada a proyecciones de tickets promedio por sucursales.