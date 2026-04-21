USE [ReportesLS];
GO

DROP PROCEDURE IF EXISTS dbo.sp_ProyeccionTransaccionesSucursal_FiltrarExistentesCarga;
DROP PROCEDURE IF EXISTS dbo.sp_ProyeccionTransaccionesSucursal_InsertarMasivo;
GO

IF TYPE_ID('dbo.ProyeccionTransaccionesSucursalCargaMensualType') IS NOT NULL
BEGIN
    DROP TYPE dbo.ProyeccionTransaccionesSucursalCargaMensualType;
END;
GO

IF OBJECT_ID('dbo.ProyeccionTransaccionesSucursal', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProyeccionTransaccionesSucursal
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProyeccionTransaccionesSucursal PRIMARY KEY,
        CodSucursal VARCHAR(20) NOT NULL,
        TransaccionProyectada DECIMAL(10,2) NOT NULL CONSTRAINT DF_ProyeccionTransaccionesSucursal_TransaccionProyectada DEFAULT (0),
        Mes INT NOT NULL,
        Ano INT NOT NULL,
        FechaAt DATETIME2(0) NOT NULL CONSTRAINT DF_ProyeccionTransaccionesSucursal_FechaAt DEFAULT (SYSDATETIME()),
        FechaModificacion DATETIME2(0) NULL,
        CodigoEmpleadoAccion VARCHAR(20) NOT NULL,
        UltimoValorTransaccionProyectada DECIMAL(10,2) NULL
    );
END;
GO

IF OBJECT_ID('dbo.ProyeccionTransaccionesSucursal', 'U') IS NOT NULL
BEGIN
    DECLARE @DefaultTransaccionSucursal SYSNAME;

    SELECT @DefaultTransaccionSucursal = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.ProyeccionTransaccionesSucursal')
      AND c.name = 'TransaccionProyectada';

    IF @DefaultTransaccionSucursal IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE dbo.ProyeccionTransaccionesSucursal DROP CONSTRAINT [' + @DefaultTransaccionSucursal + ']');
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = 'CK_ProyeccionTransaccionesSucursal_TransaccionProyectada'
          AND parent_object_id = OBJECT_ID('dbo.ProyeccionTransaccionesSucursal')
    )
    BEGIN
        ALTER TABLE dbo.ProyeccionTransaccionesSucursal
            DROP CONSTRAINT CK_ProyeccionTransaccionesSucursal_TransaccionProyectada;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = 'CK_ProyeccionTransaccionesSucursal_UltimoValorTransaccionProyectada'
          AND parent_object_id = OBJECT_ID('dbo.ProyeccionTransaccionesSucursal')
    )
    BEGIN
        ALTER TABLE dbo.ProyeccionTransaccionesSucursal
            DROP CONSTRAINT CK_ProyeccionTransaccionesSucursal_UltimoValorTransaccionProyectada;
    END;

    ALTER TABLE dbo.ProyeccionTransaccionesSucursal
        ALTER COLUMN TransaccionProyectada DECIMAL(10,2) NOT NULL;

    ALTER TABLE dbo.ProyeccionTransaccionesSucursal
        ALTER COLUMN UltimoValorTransaccionProyectada DECIMAL(10,2) NULL;

    ALTER TABLE dbo.ProyeccionTransaccionesSucursal
        ADD CONSTRAINT DF_ProyeccionTransaccionesSucursal_TransaccionProyectada
            DEFAULT (0) FOR TransaccionProyectada;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ProyeccionTransaccionesSucursal_TransaccionProyectada'
      AND parent_object_id = OBJECT_ID('dbo.ProyeccionTransaccionesSucursal')
)
BEGIN
    ALTER TABLE dbo.ProyeccionTransaccionesSucursal
        ADD CONSTRAINT CK_ProyeccionTransaccionesSucursal_TransaccionProyectada
            CHECK (TransaccionProyectada >= 0);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ProyeccionTransaccionesSucursal_UltimoValorTransaccionProyectada'
      AND parent_object_id = OBJECT_ID('dbo.ProyeccionTransaccionesSucursal')
)
BEGIN
    ALTER TABLE dbo.ProyeccionTransaccionesSucursal
        ADD CONSTRAINT CK_ProyeccionTransaccionesSucursal_UltimoValorTransaccionProyectada
            CHECK (UltimoValorTransaccionProyectada IS NULL OR UltimoValorTransaccionProyectada >= 0);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_ProyeccionTransaccionesSucursal_CodSucursal_Mes_Ano'
      AND object_id = OBJECT_ID('dbo.ProyeccionTransaccionesSucursal')
)
BEGIN
    CREATE UNIQUE INDEX UX_ProyeccionTransaccionesSucursal_CodSucursal_Mes_Ano
        ON dbo.ProyeccionTransaccionesSucursal (CodSucursal, Mes, Ano);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ProyeccionTransaccionesSucursal_Ano_Mes_CodSucursal'
      AND object_id = OBJECT_ID('dbo.ProyeccionTransaccionesSucursal')
)
BEGIN
    CREATE INDEX IX_ProyeccionTransaccionesSucursal_Ano_Mes_CodSucursal
        ON dbo.ProyeccionTransaccionesSucursal (Ano, Mes, CodSucursal);
END;
GO

CREATE TYPE dbo.ProyeccionTransaccionesSucursalCargaMensualType AS TABLE
(
    CodSucursal VARCHAR(20) NOT NULL,
    TransaccionProyectada DECIMAL(10,2) NOT NULL
);
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTransaccionesSucursal_DescargarPlantillaBase
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
        CAST(NULL AS DECIMAL(10,2)) AS TransaccionProyectada
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

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTransaccionesSucursal_FiltrarExistentesCarga
    @Items dbo.ProyeccionTransaccionesSucursalCargaMensualType READONLY
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
    INNER JOIN dbo.ProyeccionTransaccionesSucursal pts
        ON UPPER(pts.CodSucursal) = n.CodSucursal
       AND pts.Mes = @MesActual
       AND pts.Ano = @AnoActual
    ORDER BY
        n.CodSucursal;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTransaccionesSucursal_InsertarMasivo
    @CodigoEmpleadoAccion VARCHAR(20),
    @Items dbo.ProyeccionTransaccionesSucursalCargaMensualType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CodigoEmpleadoAccionNormalizado VARCHAR(20) = NULLIF(LTRIM(RTRIM(@CodigoEmpleadoAccion)), '');
    DECLARE @MesActual INT = MONTH(GETDATE());
    DECLARE @AnoActual INT = YEAR(GETDATE());
    DECLARE @SucursalesInvalidas NVARCHAR(MAX);
    DECLARE @MensajeSucursalesInvalidas NVARCHAR(2048);

    IF @CodigoEmpleadoAccionNormalizado IS NULL
        THROW 50300, 'CodigoEmpleadoAccion es obligatorio.', 1;

    IF NOT EXISTS (SELECT 1 FROM @Items)
        THROW 50301, 'No se recibieron filas para insertar.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @Items i
        WHERE i.TransaccionProyectada < 0
    )
        THROW 50302, 'TransaccionProyectada debe ser mayor o igual a cero en todas las filas.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @Items i
        WHERE NULLIF(LTRIM(RTRIM(i.CodSucursal)), '') IS NULL
    )
        THROW 50303, 'CodSucursal es obligatorio en todas las filas.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM
        (
            SELECT
                UPPER(LTRIM(RTRIM(i.CodSucursal))) AS CodSucursal,
                COUNT(1) AS Total
            FROM @Items i
            GROUP BY UPPER(LTRIM(RTRIM(i.CodSucursal)))
        ) duplicados
        WHERE duplicados.Total > 1
    )
        THROW 50304, 'No se permiten CodSucursal duplicados en la misma carga.', 1;

    SELECT
        @SucursalesInvalidas = STRING_AGG(cargas.CodSucursal, ', ')
    FROM
    (
        SELECT DISTINCT UPPER(LTRIM(RTRIM(i.CodSucursal))) AS CodSucursal
        FROM @Items i
    ) cargas
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Stores s
        WHERE UPPER(LTRIM(RTRIM(s.[No]))) = cargas.CodSucursal
          AND
            (
                s.[No] LIKE 'SK%'
                OR s.[No] LIKE 'SR%'
            )
    );

    IF @SucursalesInvalidas IS NOT NULL
    BEGIN
        SET @MensajeSucursalesInvalidas = CONCAT('Las siguientes sucursales no existen o no están permitidas: ', @SucursalesInvalidas);
        THROW 50305, @MensajeSucursalesInvalidas, 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM
        (
            SELECT DISTINCT UPPER(LTRIM(RTRIM(i.CodSucursal))) AS CodSucursal
            FROM @Items i
        ) cargas
        INNER JOIN dbo.ProyeccionTransaccionesSucursal pts
            ON UPPER(pts.CodSucursal) = cargas.CodSucursal
           AND pts.Mes = @MesActual
           AND pts.Ano = @AnoActual
    )
        THROW 50306, 'Ya existen registros guardados para una o más sucursales del mes actual.', 1;

    INSERT INTO dbo.ProyeccionTransaccionesSucursal
    (
        CodSucursal,
        TransaccionProyectada,
        Mes,
        Ano,
        FechaAt,
        FechaModificacion,
        CodigoEmpleadoAccion,
        UltimoValorTransaccionProyectada
    )
    SELECT
        UPPER(LTRIM(RTRIM(i.CodSucursal))),
        i.TransaccionProyectada,
        @MesActual,
        @AnoActual,
        SYSDATETIME(),
        NULL,
        @CodigoEmpleadoAccionNormalizado,
        NULL
    FROM @Items i;

    SELECT
        @@ROWCOUNT AS RegistrosInsertados,
        'Carga masiva procesada correctamente.' AS Mensaje;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTransaccionesSucursal_Consultar
    @Mes INT,
    @Ano INT,
    @CodSucursales NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Mes < 1 OR @Mes > 12
        THROW 50320, 'Mes es obligatorio y debe estar entre 1 y 12.', 1;

    IF @Ano < 2000
        THROW 50321, 'Año es obligatorio.', 1;

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
        pts.TransaccionProyectada
    FROM dbo.ProyeccionTransaccionesSucursal pts
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

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTransaccionesSucursal_Actualizar
    @Id INT,
    @TransaccionProyectada DECIMAL(10,2),
    @CodigoEmpleadoAccion VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CodigoEmpleadoAccionNormalizado VARCHAR(20) = NULLIF(LTRIM(RTRIM(@CodigoEmpleadoAccion)), '');
    DECLARE @TransaccionActual DECIMAL(10,2) = NULL;

    IF @Id <= 0
        THROW 50330, 'Id es obligatorio.', 1;

    IF @CodigoEmpleadoAccionNormalizado IS NULL
        THROW 50331, 'CodigoEmpleadoAccion es obligatorio.', 1;

    IF @TransaccionProyectada < 0
        THROW 50332, 'TransaccionProyectada debe ser mayor o igual a cero.', 1;

    SELECT @TransaccionActual = TransaccionProyectada
    FROM dbo.ProyeccionTransaccionesSucursal
    WHERE Id = @Id;

    IF @TransaccionActual IS NULL
        THROW 50333, 'No se encontró el registro a actualizar.', 1;

    IF @TransaccionActual = @TransaccionProyectada
    BEGIN
        SELECT
            'NOCHANGE' AS Accion,
            @Id AS Id,
            @TransaccionActual AS TransaccionProyectada,
            @TransaccionActual AS UltimoValorTransaccionProyectada;
        RETURN;
    END;

    UPDATE dbo.ProyeccionTransaccionesSucursal
    SET
        UltimoValorTransaccionProyectada = TransaccionProyectada,
        TransaccionProyectada = @TransaccionProyectada,
        FechaModificacion = SYSDATETIME(),
        CodigoEmpleadoAccion = @CodigoEmpleadoAccionNormalizado
    WHERE Id = @Id;

    SELECT
        'UPDATE' AS Accion,
        @Id AS Id,
        @TransaccionProyectada AS TransaccionProyectada,
        @TransaccionActual AS UltimoValorTransaccionProyectada;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTransaccionesSucursal_EliminarMes
    @Mes INT,
    @Ano INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Mes < 1 OR @Mes > 12
        THROW 50340, 'Mes es obligatorio y debe estar entre 1 y 12.', 1;

    IF @Ano < 2000
        THROW 50341, 'Año es obligatorio.', 1;

    DELETE FROM dbo.ProyeccionTransaccionesSucursal
    WHERE Mes = @Mes
      AND Ano = @Ano;

    SELECT
        @@ROWCOUNT AS RegistrosEliminados,
        'Registros eliminados correctamente.' AS Mensaje;
END;
GO
