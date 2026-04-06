USE [ReportesLS];
GO

IF OBJECT_ID('dbo.ProyeccionTicketSucursal', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProyeccionTicketSucursal
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProyeccionTicketSucursal PRIMARY KEY,
        CodSucursal VARCHAR(20) NOT NULL,
        Fecha DATE NULL,
        Mes INT NOT NULL,
        Ano INT NOT NULL,
        TicketPromedio INT NOT NULL CONSTRAINT DF_ProyeccionTicketSucursal_TicketPromedio DEFAULT (0),
        FechaAt DATETIME2(0) NOT NULL CONSTRAINT DF_ProyeccionTicketSucursal_FechaAt DEFAULT (SYSDATETIME()),
        FechaModificacion DATETIME2(0) NULL,
        CodigoEmpleadoAccion VARCHAR(20) NOT NULL,
        UltimoValorTicketPromedio INT NULL
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ProyeccionTicketSucursal_TicketPromedio'
      AND parent_object_id = OBJECT_ID('dbo.ProyeccionTicketSucursal')
)
BEGIN
    ALTER TABLE dbo.ProyeccionTicketSucursal
        ADD CONSTRAINT CK_ProyeccionTicketSucursal_TicketPromedio
            CHECK (TicketPromedio >= 0);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_ProyeccionTicketSucursal_CodSucursal_Mes_Ano'
      AND object_id = OBJECT_ID('dbo.ProyeccionTicketSucursal')
)
BEGIN
    CREATE UNIQUE INDEX UX_ProyeccionTicketSucursal_CodSucursal_Mes_Ano
        ON dbo.ProyeccionTicketSucursal (CodSucursal, Mes, Ano);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ProyeccionTicketSucursal_Ano_Mes_CodSucursal'
      AND object_id = OBJECT_ID('dbo.ProyeccionTicketSucursal')
)
BEGIN
    CREATE INDEX IX_ProyeccionTicketSucursal_Ano_Mes_CodSucursal
        ON dbo.ProyeccionTicketSucursal (Ano, Mes, CodSucursal);
END;
GO

DROP PROCEDURE IF EXISTS dbo.sp_ProyeccionTicketSucursal_FiltrarExistentesCarga;
DROP PROCEDURE IF EXISTS dbo.sp_ProyeccionTicketSucursal_InsertarMasivo;
GO

IF TYPE_ID('dbo.ProyeccionTicketSucursalCargaMensualType') IS NOT NULL
BEGIN
    DROP TYPE dbo.ProyeccionTicketSucursalCargaMensualType;
END;
GO

CREATE TYPE dbo.ProyeccionTicketSucursalCargaMensualType AS TABLE
(
    CodSucursal VARCHAR(20) NOT NULL,
    TicketPromedio INT NOT NULL
);
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTicketSucursal_DescargarPlantillaBase
    @CodSucursales NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH SucursalesSeleccionadas AS
    (
        SELECT DISTINCT LTRIM(RTRIM(value)) AS CodSucursal
        FROM STRING_SPLIT(ISNULL(@CodSucursales, ''), ',')
        WHERE NULLIF(LTRIM(RTRIM(value)), '') IS NOT NULL
    )
    SELECT
        s.[No] AS CodSucursal,
        s.[StoreNo] AS NombreSucursal,
        CAST(NULL AS INT) AS TicketPromedio
    FROM dbo.Stores s
    WHERE
        (
            s.[No] LIKE 'SK%'
            OR s.[No] LIKE 'SR%'
        )
      AND
        (
            NOT EXISTS (SELECT 1 FROM SucursalesSeleccionadas)
            OR s.[No] IN (SELECT CodSucursal FROM SucursalesSeleccionadas)
        )
    ORDER BY
        s.[No],
        s.[StoreNo];
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTicketSucursal_FiltrarExistentesCarga
    @Items dbo.ProyeccionTicketSucursalCargaMensualType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MesActual INT = MONTH(GETDATE());
    DECLARE @AnoActual INT = YEAR(GETDATE());

    ;WITH Normalizados AS
    (
        SELECT DISTINCT
            UPPER(LTRIM(RTRIM(i.CodSucursal))) AS CodSucursal
        FROM @Items i
        WHERE NULLIF(LTRIM(RTRIM(i.CodSucursal)), '') IS NOT NULL
    )
    SELECT
        @MesActual AS Mes,
        @AnoActual AS Ano,
        n.CodSucursal
    FROM Normalizados n
    INNER JOIN dbo.ProyeccionTicketSucursal pts
        ON UPPER(pts.CodSucursal) = n.CodSucursal
       AND pts.Mes = @MesActual
       AND pts.Ano = @AnoActual
    ORDER BY
        n.CodSucursal;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTicketSucursal_InsertarMasivo
    @CodigoEmpleadoAccion VARCHAR(20),
    @Items dbo.ProyeccionTicketSucursalCargaMensualType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CodigoEmpleadoAccionNormalizado VARCHAR(20) = NULLIF(LTRIM(RTRIM(@CodigoEmpleadoAccion)), '');
    DECLARE @MesActual INT = MONTH(GETDATE());
    DECLARE @AnoActual INT = YEAR(GETDATE());
    DECLARE @SucursalesInvalidas NVARCHAR(MAX);
    DECLARE @MensajeSucursalesInvalidas NVARCHAR(2048);

    IF @CodigoEmpleadoAccionNormalizado IS NULL
        THROW 50200, 'CodigoEmpleadoAccion es obligatorio.', 1;

    IF NOT EXISTS (SELECT 1 FROM @Items)
        THROW 50201, 'No se recibieron filas para insertar.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @Items i
        WHERE i.TicketPromedio < 0
    )
        THROW 50202, 'TicketPromedio debe ser mayor o igual a cero en todas las filas.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @Items i
        WHERE NULLIF(LTRIM(RTRIM(i.CodSucursal)), '') IS NULL
    )
        THROW 50203, 'CodSucursal es obligatorio en todas las filas.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM
        (
            SELECT
                UPPER(LTRIM(RTRIM(i.CodSucursal))) AS CodSucursal,
                COUNT(1) AS N
            FROM @Items i
            GROUP BY UPPER(LTRIM(RTRIM(i.CodSucursal)))
        ) d
        WHERE d.N > 1
    )
        THROW 50204, 'El archivo contiene filas duplicadas para CodSucursal.', 1;

    ;WITH SucursalesArchivo AS
    (
        SELECT DISTINCT UPPER(LTRIM(RTRIM(i.CodSucursal))) AS CodSucursal
        FROM @Items i
    ),
    SucursalesValidas AS
    (
        SELECT DISTINCT UPPER(LTRIM(RTRIM(s.[No]))) AS CodSucursal
        FROM dbo.Stores s
        WHERE s.[No] LIKE 'SK%'
           OR s.[No] LIKE 'SR%'
    )
    SELECT @SucursalesInvalidas = STRING_AGG(a.CodSucursal, ', ')
    FROM SucursalesArchivo a
    LEFT JOIN SucursalesValidas v
        ON v.CodSucursal = a.CodSucursal
    WHERE v.CodSucursal IS NULL;

    IF @SucursalesInvalidas IS NOT NULL
    BEGIN
        SET @MensajeSucursalesInvalidas = CONCAT('Se encontraron sucursales no válidas: ', @SucursalesInvalidas);
        THROW 50205, @MensajeSucursalesInvalidas, 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM @Items i
        INNER JOIN dbo.ProyeccionTicketSucursal pts
            ON UPPER(pts.CodSucursal) = UPPER(LTRIM(RTRIM(i.CodSucursal)))
           AND pts.Mes = @MesActual
           AND pts.Ano = @AnoActual
    )
        THROW 50206, 'Ya existen registros guardados para una o más sucursales en el mes actual.', 1;

    INSERT INTO dbo.ProyeccionTicketSucursal
    (
        CodSucursal,
        Fecha,
        Mes,
        Ano,
        TicketPromedio,
        FechaAt,
        FechaModificacion,
        CodigoEmpleadoAccion,
        UltimoValorTicketPromedio
    )
    SELECT
        UPPER(LTRIM(RTRIM(i.CodSucursal))) AS CodSucursal,
        NULL AS Fecha,
        @MesActual AS Mes,
        @AnoActual AS Ano,
        i.TicketPromedio,
        SYSDATETIME(),
        NULL,
        @CodigoEmpleadoAccionNormalizado,
        NULL
    FROM @Items i;

    SELECT
        COUNT(1) AS RegistrosInsertados,
        CONCAT('Datos guardados correctamente para el período ', RIGHT(CONCAT('00', @MesActual), 2), '/', @AnoActual, '.') AS Mensaje
    FROM @Items;
END;
GO
