USE [ReportesLS];
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTicketStaff_Consultar
    @Mes INT,
    @Ano INT,
    @NumeroEmpleado VARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Mes < 1 OR @Mes > 12
        THROW 50120, 'Mes es obligatorio y debe estar entre 1 y 12.', 1;

    IF @Ano < 2000
        THROW 50121, 'Año es obligatorio.', 1;

    DECLARE @NumeroEmpleadoNormalizado VARCHAR(20) = NULLIF(LTRIM(RTRIM(@NumeroEmpleado)), '');

    SELECT
        pts.Id,
        DATEFROMPARTS(pts.Ano, pts.Mes, 1) AS Fecha,
        pts.Mes,
        pts.Ano,
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
    WHERE pts.Mes = @Mes
      AND pts.Ano = @Ano
      AND
        (
            @NumeroEmpleadoNormalizado IS NULL
            OR pts.NumeroEmpleado LIKE '%' + @NumeroEmpleadoNormalizado + '%'
        )
    ORDER BY
        pts.NumeroEmpleado;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTicketStaff_EliminarMes
    @Mes INT,
    @Ano INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Mes < 1 OR @Mes > 12
        THROW 50140, 'Mes es obligatorio y debe estar entre 1 y 12.', 1;

    IF @Ano < 2000
        THROW 50141, 'Año es obligatorio.', 1;

    DELETE FROM dbo.ProyeccionTicketStaff
    WHERE Mes = @Mes
      AND Ano = @Ano;

    SELECT
        @@ROWCOUNT AS RegistrosEliminados,
        'Registros eliminados correctamente.' AS Mensaje;
END;
GO
