USE [ReportesLS];
GO

IF TYPE_ID('dbo.ProyeccionTicketStaffCargaMensualType') IS NULL
BEGIN
    CREATE TYPE dbo.ProyeccionTicketStaffCargaMensualType AS TABLE
    (
        Fecha DATE NOT NULL,
        NumeroEmpleado VARCHAR(20) NOT NULL,
        TicketPromedio INT NOT NULL
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTicketStaff_DescargarStaffBase
    @NumeroSupervisor VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NumeroSupervisorNormalizado VARCHAR(20) = NULLIF(LTRIM(RTRIM(@NumeroSupervisor)), '');

    SELECT
        LTRIM(RTRIM(ISNULL(e.NumeroEmpleado, ''))) AS NumeroEmpleado,
        LTRIM(RTRIM(
            CONCAT(
                ISNULL(LTRIM(RTRIM(e.NombreEmpleado)), ''),
                CASE
                    WHEN NULLIF(LTRIM(RTRIM(ISNULL(e.ApellidosEmpleado, ''))), '') IS NULL THEN ''
                    ELSE ' - ' + LTRIM(RTRIM(e.ApellidosEmpleado))
                END
            )
        )) AS NombreStaff
    FROM CDC.dbo.Empleados e
    WHERE @NumeroSupervisorNormalizado IS NULL
       OR e.NumeroSupervisor = @NumeroSupervisorNormalizado
    ORDER BY
        e.NombreEmpleado,
        e.ApellidosEmpleado,
        e.NumeroEmpleado;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTicketStaff_FiltrarExistentesCarga
    @Items dbo.ProyeccionTicketStaffCargaMensualType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Normalizados AS
    (
        SELECT DISTINCT
            CAST(i.Fecha AS DATE) AS Fecha,
            LTRIM(RTRIM(i.NumeroEmpleado)) AS NumeroEmpleado
        FROM @Items i
        WHERE i.Fecha IS NOT NULL
          AND NULLIF(LTRIM(RTRIM(i.NumeroEmpleado)), '') IS NOT NULL
    )
    SELECT
        n.Fecha,
        n.NumeroEmpleado
    FROM Normalizados n
    INNER JOIN dbo.ProyeccionTicketStaff pts
        ON pts.Fecha = n.Fecha
       AND pts.NumeroEmpleado = n.NumeroEmpleado
    ORDER BY
        n.Fecha,
        n.NumeroEmpleado;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTicketStaff_InsertarMasivo
    @CodigoEmpleadoAccion VARCHAR(20),
    @Items dbo.ProyeccionTicketStaffCargaMensualType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CodigoEmpleadoAccionNormalizado VARCHAR(20) = NULLIF(LTRIM(RTRIM(@CodigoEmpleadoAccion)), '');
    DECLARE @MesInicioActual DATE = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);
    DECLARE @MesFinActual DATE = EOMONTH(GETDATE());

    IF @CodigoEmpleadoAccionNormalizado IS NULL
        THROW 50100, 'CodigoEmpleadoAccion es obligatorio.', 1;

    IF NOT EXISTS (SELECT 1 FROM @Items)
        THROW 50101, 'No se recibieron filas para insertar.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @Items i
        WHERE i.TicketPromedio < 0
    )
        THROW 50102, 'TicketPromedio debe ser mayor o igual a cero en todas las filas.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @Items i
        WHERE i.Fecha < @MesInicioActual
           OR i.Fecha > @MesFinActual
    )
        THROW 50103, 'Todas las fechas deben pertenecer al mes en curso.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @Items i
        WHERE NULLIF(LTRIM(RTRIM(i.NumeroEmpleado)), '') IS NULL
    )
        THROW 50104, 'NumeroEmpleado es obligatorio en todas las filas.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM
        (
            SELECT
                i.Fecha,
                LTRIM(RTRIM(i.NumeroEmpleado)) AS NumeroEmpleado,
                COUNT(1) AS N
            FROM @Items i
            GROUP BY
                i.Fecha,
                LTRIM(RTRIM(i.NumeroEmpleado))
        ) d
        WHERE d.N > 1
    )
        THROW 50105, 'El archivo contiene filas duplicadas para la combinación Fecha + NumeroEmpleado.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @Items i
        INNER JOIN dbo.ProyeccionTicketStaff pts
            ON pts.Fecha = i.Fecha
           AND pts.NumeroEmpleado = LTRIM(RTRIM(i.NumeroEmpleado))
    )
        THROW 50106, 'Ya existen registros guardados para una o más combinaciones de Fecha + NumeroEmpleado.', 1;

    INSERT INTO dbo.ProyeccionTicketStaff
    (
        NumeroEmpleado,
        Fecha,
        TicketPromedio,
        FechaAt,
        FechaModificacion,
        CodigoEmpleadoAccion,
        UltimoValorTicketPromedio
    )
    SELECT
        LTRIM(RTRIM(i.NumeroEmpleado)) AS NumeroEmpleado,
        i.Fecha,
        i.TicketPromedio,
        SYSDATETIME(),
        NULL,
        @CodigoEmpleadoAccionNormalizado,
        NULL
    FROM @Items i;

    SELECT
        COUNT(1) AS RegistrosInsertados,
        'Datos guardados correctamente.' AS Mensaje
    FROM @Items;
END;
GO
