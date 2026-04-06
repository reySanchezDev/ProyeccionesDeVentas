USE [ReportesLS];
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTicketSucursal_Consultar
    @Mes INT,
    @Ano INT,
    @CodSucursales NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Mes < 1 OR @Mes > 12
        THROW 50220, 'Mes es obligatorio y debe estar entre 1 y 12.', 1;

    IF @Ano < 2000
        THROW 50221, 'Año es obligatorio.', 1;

    ;WITH SucursalesSeleccionadas AS
    (
        SELECT DISTINCT UPPER(LTRIM(RTRIM(value))) AS CodSucursal
        FROM STRING_SPLIT(ISNULL(@CodSucursales, ''), ',')
        WHERE NULLIF(LTRIM(RTRIM(value)), '') IS NOT NULL
    )
    SELECT
        pts.Id,
        DATEFROMPARTS(pts.Ano, pts.Mes, 1) AS Fecha,
        pts.Mes,
        pts.Ano,
        pts.CodSucursal,
        CASE
            WHEN s.[No] IS NULL THEN CONCAT('Sucursal no existe en catálogo. Revisar código: ', pts.CodSucursal)
            ELSE LTRIM(RTRIM(ISNULL(s.StoreNo, '')))
        END AS NombreSucursal,
        CAST(CASE WHEN s.[No] IS NULL THEN 0 ELSE 1 END AS BIT) AS ExisteEnCatalogo,
        pts.TicketPromedio
    FROM dbo.ProyeccionTicketSucursal pts
    LEFT JOIN dbo.Stores s
        ON UPPER(LTRIM(RTRIM(s.[No]))) = UPPER(LTRIM(RTRIM(pts.CodSucursal)))
    WHERE pts.Mes = @Mes
      AND pts.Ano = @Ano
      AND
        (
            NOT EXISTS (SELECT 1 FROM SucursalesSeleccionadas)
            OR UPPER(LTRIM(RTRIM(pts.CodSucursal))) IN (SELECT CodSucursal FROM SucursalesSeleccionadas)
        )
    ORDER BY
        UPPER(LTRIM(RTRIM(pts.CodSucursal)));
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTicketSucursal_Actualizar
    @Id INT,
    @TicketPromedio INT,
    @CodigoEmpleadoAccion VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CodigoEmpleadoAccionNormalizado VARCHAR(20) = NULLIF(LTRIM(RTRIM(@CodigoEmpleadoAccion)), '');
    DECLARE @TicketActual INT = NULL;

    IF @Id <= 0
        THROW 50230, 'Id es obligatorio.', 1;

    IF @CodigoEmpleadoAccionNormalizado IS NULL
        THROW 50231, 'CodigoEmpleadoAccion es obligatorio.', 1;

    IF @TicketPromedio < 0
        THROW 50232, 'TicketPromedio debe ser mayor o igual a cero.', 1;

    SELECT @TicketActual = TicketPromedio
    FROM dbo.ProyeccionTicketSucursal
    WHERE Id = @Id;

    IF @TicketActual IS NULL
        THROW 50233, 'No se encontró el registro a actualizar.', 1;

    IF @TicketActual = @TicketPromedio
    BEGIN
        SELECT
            'NOCHANGE' AS Accion,
            @Id AS Id,
            @TicketActual AS TicketPromedio,
            @TicketActual AS UltimoValorTicketPromedio;
        RETURN;
    END;

    UPDATE dbo.ProyeccionTicketSucursal
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

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTicketSucursal_EliminarMes
    @Mes INT,
    @Ano INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Mes < 1 OR @Mes > 12
        THROW 50240, 'Mes es obligatorio y debe estar entre 1 y 12.', 1;

    IF @Ano < 2000
        THROW 50241, 'Año es obligatorio.', 1;

    DELETE FROM dbo.ProyeccionTicketSucursal
    WHERE Mes = @Mes
      AND Ano = @Ano;

    SELECT
        @@ROWCOUNT AS RegistrosEliminados,
        'Registros eliminados correctamente.' AS Mensaje;
END;
GO
