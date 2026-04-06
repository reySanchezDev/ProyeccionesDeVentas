USE [ReportesLS];
GO

CREATE OR ALTER PROCEDURE dbo.sp_catologoPuesto
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT
        LTRIM(RTRIM(e.Puesto)) AS Puesto
    FROM CDC.dbo.Empleados e
    WHERE e.Puesto IN ('ASISTENTE DE KIOSCO', 'BARISTA CAJERO', 'LIDER DE KIOSCO', 'MESERO')
      AND NULLIF(LTRIM(RTRIM(e.Puesto)), '') IS NOT NULL
    ORDER BY Puesto;
END;
GO

IF COL_LENGTH('dbo.ProyeccionTicketStaff', 'Mes') IS NULL
BEGIN
    ALTER TABLE dbo.ProyeccionTicketStaff
        ADD Mes INT NULL;
END;
GO

IF COL_LENGTH('dbo.ProyeccionTicketStaff', 'Ano') IS NULL
BEGIN
    ALTER TABLE dbo.ProyeccionTicketStaff
        ADD Ano INT NULL;
END;
GO

IF OBJECT_ID('dbo.ProyeccionTicketStaff', 'U') IS NOT NULL
BEGIN
    UPDATE dbo.ProyeccionTicketStaff
    SET
        Mes = ISNULL(Mes, MONTH(ISNULL(Fecha, CAST(FechaAt AS DATE)))),
        Ano = ISNULL(Ano, YEAR(ISNULL(Fecha, CAST(FechaAt AS DATE))))
    WHERE Mes IS NULL
       OR Ano IS NULL;
END;
GO

IF OBJECT_ID('dbo.ProyeccionTicketStaff', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ProyeccionTicketStaff', 'Mes') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ProyeccionTicketStaff
        ALTER COLUMN Mes INT NOT NULL;
END;
GO

IF OBJECT_ID('dbo.ProyeccionTicketStaff', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ProyeccionTicketStaff', 'Ano') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ProyeccionTicketStaff
        ALTER COLUMN Ano INT NOT NULL;
END;
GO

IF OBJECT_ID('dbo.ProyeccionTicketStaff', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ProyeccionTicketStaff', 'Fecha') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ProyeccionTicketStaff
        ALTER COLUMN Fecha DATE NULL;
END;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_ProyeccionTicketStaff_NumeroEmpleado_Fecha'
      AND object_id = OBJECT_ID('dbo.ProyeccionTicketStaff')
)
BEGIN
    DROP INDEX UX_ProyeccionTicketStaff_NumeroEmpleado_Fecha
        ON dbo.ProyeccionTicketStaff;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_ProyeccionTicketStaff_NumeroEmpleado_Mes_Ano'
      AND object_id = OBJECT_ID('dbo.ProyeccionTicketStaff')
)
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM dbo.ProyeccionTicketStaff
        GROUP BY NumeroEmpleado, Mes, Ano
        HAVING COUNT(1) > 1
    )
    BEGIN
        PRINT 'No se creó UX_ProyeccionTicketStaff_NumeroEmpleado_Mes_Ano porque existen duplicados históricos por NumeroEmpleado, Mes y Ano.';
    END
    ELSE
    BEGIN
        CREATE UNIQUE INDEX UX_ProyeccionTicketStaff_NumeroEmpleado_Mes_Ano
            ON dbo.ProyeccionTicketStaff (NumeroEmpleado, Mes, Ano);
    END
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ProyeccionTicketStaff_Ano_Mes_NumeroEmpleado'
      AND object_id = OBJECT_ID('dbo.ProyeccionTicketStaff')
)
BEGIN
    CREATE INDEX IX_ProyeccionTicketStaff_Ano_Mes_NumeroEmpleado
        ON dbo.ProyeccionTicketStaff (Ano, Mes, NumeroEmpleado);
END;
GO

DROP PROCEDURE IF EXISTS dbo.sp_ProyeccionTicketStaff_FiltrarExistentesCarga;
DROP PROCEDURE IF EXISTS dbo.sp_ProyeccionTicketStaff_InsertarMasivo;
GO

IF TYPE_ID('dbo.ProyeccionTicketStaffCargaMensualType') IS NOT NULL
BEGIN
    DROP TYPE dbo.ProyeccionTicketStaffCargaMensualType;
END;
GO

CREATE TYPE dbo.ProyeccionTicketStaffCargaMensualType AS TABLE
(
    NumeroEmpleado VARCHAR(20) NOT NULL,
    TicketPromedio INT NOT NULL
);
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTicketStaff_DescargarStaffBase
    @Puestos NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH PuestosPermitidos AS
    (
        SELECT 'ASISTENTE DE KIOSCO' AS Puesto
        UNION ALL SELECT 'BARISTA CAJERO'
        UNION ALL SELECT 'LIDER DE KIOSCO'
        UNION ALL SELECT 'MESERO'
    ),
    PuestosSeleccionados AS
    (
        SELECT DISTINCT LTRIM(RTRIM(value)) AS Puesto
        FROM STRING_SPLIT(ISNULL(@Puestos, ''), ',')
        WHERE NULLIF(LTRIM(RTRIM(value)), '') IS NOT NULL
    )
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
        )) AS NombreStaff,
        LTRIM(RTRIM(ISNULL(e.Puesto, ''))) AS Puesto,
        LTRIM(RTRIM(ISNULL(e.Ubicacion, ''))) AS Ubicacion,
        CAST(NULL AS INT) AS TicketPromedio
    FROM CDC.dbo.Empleados e
    WHERE LTRIM(RTRIM(ISNULL(e.Puesto, ''))) IN (SELECT Puesto FROM PuestosPermitidos)
      AND
      (
          NOT EXISTS (SELECT 1 FROM PuestosSeleccionados)
          OR LTRIM(RTRIM(ISNULL(e.Puesto, ''))) IN (SELECT Puesto FROM PuestosSeleccionados)
      )
    ORDER BY
        LTRIM(RTRIM(ISNULL(e.Puesto, ''))),
        LTRIM(RTRIM(ISNULL(e.Ubicacion, ''))),
        LTRIM(RTRIM(ISNULL(e.NombreEmpleado, ''))),
        LTRIM(RTRIM(ISNULL(e.ApellidosEmpleado, ''))),
        LTRIM(RTRIM(ISNULL(e.NumeroEmpleado, '')));
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionTicketStaff_FiltrarExistentesCarga
    @Items dbo.ProyeccionTicketStaffCargaMensualType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MesActual INT = MONTH(GETDATE());
    DECLARE @AnoActual INT = YEAR(GETDATE());

    ;WITH Normalizados AS
    (
        SELECT DISTINCT
            LTRIM(RTRIM(i.NumeroEmpleado)) AS NumeroEmpleado
        FROM @Items i
        WHERE NULLIF(LTRIM(RTRIM(i.NumeroEmpleado)), '') IS NOT NULL
    )
    SELECT
        @MesActual AS Mes,
        @AnoActual AS Ano,
        n.NumeroEmpleado
    FROM Normalizados n
    INNER JOIN dbo.ProyeccionTicketStaff pts
        ON pts.NumeroEmpleado = n.NumeroEmpleado
       AND pts.Mes = @MesActual
       AND pts.Ano = @AnoActual
    ORDER BY
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
    DECLARE @MesActual INT = MONTH(GETDATE());
    DECLARE @AnoActual INT = YEAR(GETDATE());

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
        WHERE NULLIF(LTRIM(RTRIM(i.NumeroEmpleado)), '') IS NULL
    )
        THROW 50104, 'NumeroEmpleado es obligatorio en todas las filas.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM
        (
            SELECT
                LTRIM(RTRIM(i.NumeroEmpleado)) AS NumeroEmpleado,
                COUNT(1) AS N
            FROM @Items i
            GROUP BY LTRIM(RTRIM(i.NumeroEmpleado))
        ) d
        WHERE d.N > 1
    )
        THROW 50105, 'El archivo contiene filas duplicadas para NumeroEmpleado.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM @Items i
        INNER JOIN dbo.ProyeccionTicketStaff pts
            ON pts.NumeroEmpleado = LTRIM(RTRIM(i.NumeroEmpleado))
           AND pts.Mes = @MesActual
           AND pts.Ano = @AnoActual
    )
        THROW 50106, 'Ya existen registros guardados para uno o más empleados en el mes actual.', 1;

    INSERT INTO dbo.ProyeccionTicketStaff
    (
        NumeroEmpleado,
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
        LTRIM(RTRIM(i.NumeroEmpleado)) AS NumeroEmpleado,
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
    DECLARE @PeriodoInicio INT = (YEAR(@FechaInicio) * 100) + MONTH(@FechaInicio);
    DECLARE @PeriodoFin INT = (YEAR(@FechaFin) * 100) + MONTH(@FechaFin);

    SELECT
        pts.Id,
        ISNULL(pts.Fecha, DATEFROMPARTS(pts.Ano, pts.Mes, 1)) AS Fecha,
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
    WHERE
        (
            (
                pts.Fecha IS NOT NULL
                AND pts.Fecha >= @FechaInicio
                AND pts.Fecha <= @FechaFin
            )
            OR
            (
                pts.Fecha IS NULL
                AND ((pts.Ano * 100) + pts.Mes) BETWEEN @PeriodoInicio AND @PeriodoFin
            )
        )
      AND
        (
            @NumeroEmpleadoNormalizado IS NULL
            OR pts.NumeroEmpleado LIKE '%' + @NumeroEmpleadoNormalizado + '%'
        )
    ORDER BY
        ISNULL(pts.Fecha, DATEFROMPARTS(pts.Ano, pts.Mes, 1)),
        pts.NumeroEmpleado;
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

    DECLARE @PeriodoInicio INT = (YEAR(@FechaInicio) * 100) + MONTH(@FechaInicio);
    DECLARE @PeriodoFin INT = (YEAR(@FechaFin) * 100) + MONTH(@FechaFin);

    DELETE FROM dbo.ProyeccionTicketStaff
    WHERE
        (
            (
                Fecha IS NOT NULL
                AND Fecha >= @FechaInicio
                AND Fecha <= @FechaFin
            )
            OR
            (
                Fecha IS NULL
                AND ((Ano * 100) + Mes) BETWEEN @PeriodoInicio AND @PeriodoFin
            )
        );

    SELECT
        @@ROWCOUNT AS RegistrosEliminados,
        'Registros eliminados correctamente.' AS Mensaje;
END;
GO
