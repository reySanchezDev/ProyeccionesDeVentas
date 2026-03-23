USE [ReportesLS];
GO

IF OBJECT_ID('dbo.ProyeccionTicketStaff', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ProyeccionTicketStaff', 'Fecha') IS NULL
    BEGIN
        ALTER TABLE dbo.ProyeccionTicketStaff
            ADD Fecha DATE NULL;
    END;
END;
GO

IF OBJECT_ID('dbo.ProyeccionTicketStaff', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ProyeccionTicketStaff', 'Fecha') IS NOT NULL
BEGIN
    UPDATE dbo.ProyeccionTicketStaff
    SET Fecha = CAST(ISNULL(FechaAt, SYSDATETIME()) AS DATE)
    WHERE Fecha IS NULL;
END;
GO

IF OBJECT_ID('dbo.ProyeccionTicketStaff', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ProyeccionTicketStaff', 'Fecha') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ProyeccionTicketStaff
        ALTER COLUMN Fecha DATE NOT NULL;
END;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_ProyeccionTicketStaff_NumeroEmpleado'
      AND object_id = OBJECT_ID('dbo.ProyeccionTicketStaff')
)
BEGIN
    DROP INDEX UX_ProyeccionTicketStaff_NumeroEmpleado
        ON dbo.ProyeccionTicketStaff;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_ProyeccionTicketStaff_NumeroEmpleado_Fecha'
      AND object_id = OBJECT_ID('dbo.ProyeccionTicketStaff')
)
BEGIN
    CREATE UNIQUE INDEX UX_ProyeccionTicketStaff_NumeroEmpleado_Fecha
        ON dbo.ProyeccionTicketStaff (NumeroEmpleado, Fecha);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ProyeccionTicketStaff_Fecha_NumeroEmpleado'
      AND object_id = OBJECT_ID('dbo.ProyeccionTicketStaff')
)
BEGIN
    CREATE INDEX IX_ProyeccionTicketStaff_Fecha_NumeroEmpleado
        ON dbo.ProyeccionTicketStaff (Fecha, NumeroEmpleado);
END;
GO
