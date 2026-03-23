USE [ReportesLS];
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTicketStaff_Consultar
    @FechaInicio DATE,
    @FechaFin DATE,
    @NumeroEmpleado VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @FechaInicio IS NULL OR @FechaFin IS NULL
        THROW 50120, 'FechaInicio y FechaFin son obligatorias.', 1;

    IF @FechaInicio > @FechaFin
        THROW 50121, 'FechaInicio no puede ser mayor que FechaFin.', 1;

    DECLARE @NumeroEmpleadoNormalizado VARCHAR(20) = NULLIF(LTRIM(RTRIM(@NumeroEmpleado)), '');

    SELECT
        pts.Id,
        pts.Fecha,
        pts.NumeroEmpleado,
        CASE
            WHEN e.NumeroEmpleado IS NULL THEN 'Empleado no existe en CDC. Revisar registro'
            ELSE LTRIM(RTRIM(
                CONCAT(
                    ISNULL(LTRIM(RTRIM(e.NombreEmpleado)), ''),
                    CASE
                        WHEN NULLIF(LTRIM(RTRIM(ISNULL(e.ApellidosEmpleado, ''))), '') IS NULL THEN ''
                        ELSE ' - ' + LTRIM(RTRIM(e.ApellidosEmpleado))
                    END
                )
            ))
        END AS NombreStaff,
        CAST(CASE WHEN e.NumeroEmpleado IS NULL THEN 0 ELSE 1 END AS BIT) AS ExisteEnCDC,
        pts.TicketPromedio
    FROM dbo.ProyeccionTicketStaff pts
    LEFT JOIN CDC.dbo.Empleados e
        ON e.NumeroEmpleado COLLATE DATABASE_DEFAULT = pts.NumeroEmpleado
    WHERE pts.Fecha >= @FechaInicio
      AND pts.Fecha <= @FechaFin
      AND (
            @NumeroEmpleadoNormalizado IS NULL
            OR pts.NumeroEmpleado LIKE '%' + @NumeroEmpleadoNormalizado + '%'
          )
    ORDER BY
        pts.Fecha,
        pts.NumeroEmpleado;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTicketStaff_Actualizar
    @Id INT,
    @TicketPromedio INT,
    @CodigoEmpleadoAccion VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CodigoEmpleadoAccionNormalizado VARCHAR(20) = NULLIF(LTRIM(RTRIM(@CodigoEmpleadoAccion)), '');
    DECLARE @TicketActual INT = NULL;

    IF @Id <= 0
        THROW 50130, 'Id es obligatorio.', 1;

    IF @CodigoEmpleadoAccionNormalizado IS NULL
        THROW 50131, 'CodigoEmpleadoAccion es obligatorio.', 1;

    IF @TicketPromedio < 0
        THROW 50132, 'TicketPromedio debe ser mayor o igual a cero.', 1;

    SELECT @TicketActual = TicketPromedio
    FROM dbo.ProyeccionTicketStaff
    WHERE Id = @Id;

    IF @TicketActual IS NULL
        THROW 50133, 'No se encontró el registro a actualizar.', 1;

    IF @TicketActual = @TicketPromedio
    BEGIN
        SELECT
            'NOCHANGE' AS Accion,
            @Id AS Id,
            @TicketActual AS TicketPromedio,
            @TicketActual AS UltimoValorTicketPromedio;
        RETURN;
    END;

    UPDATE dbo.ProyeccionTicketStaff
    SET
        UltimoValorTicketPromedio = TicketPromedio,
        TicketPromedio = @TicketPromedio,
        FechaModificacion = SYSDATETIME(),
        CodigoEmpleadoAccion = @CodigoEmpleadoAccionNormalizado
    WHERE Id = @Id;

    SELECT
        'UPDATE' AS Accion,
        @Id AS Id,
        @TicketPromedio AS TicketPromedio,
        @TicketActual AS UltimoValorTicketPromedio;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTicketStaff_EliminarMes
    @FechaInicio DATE,
    @FechaFin DATE
AS
BEGIN
    SET NOCOUNT ON;

    IF @FechaInicio IS NULL OR @FechaFin IS NULL
        THROW 50140, 'FechaInicio y FechaFin son obligatorias.', 1;

    IF @FechaInicio > @FechaFin
        THROW 50141, 'FechaInicio no puede ser mayor que FechaFin.', 1;

    DELETE FROM dbo.ProyeccionTicketStaff
    WHERE Fecha >= @FechaInicio
      AND Fecha <= @FechaFin;

    SELECT
        @@ROWCOUNT AS RegistrosEliminados,
        'Registros eliminados correctamente.' AS Mensaje;
END;
GO
