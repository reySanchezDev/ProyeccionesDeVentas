USE [ReportesLS];
GO

IF OBJECT_ID('dbo.ProyeccionTicketStaff', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProyeccionTicketStaff
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProyeccionTicketStaff PRIMARY KEY,
        NumeroEmpleado VARCHAR(20) NOT NULL,
        Fecha DATE NOT NULL,
        TicketPromedio INT NOT NULL CONSTRAINT DF_ProyeccionTicketStaff_TicketPromedio DEFAULT (0),
        FechaAt DATETIME2(0) NOT NULL CONSTRAINT DF_ProyeccionTicketStaff_FechaAt DEFAULT (SYSDATETIME()),
        FechaModificacion DATETIME2(0) NULL,
        CodigoEmpleadoAccion VARCHAR(20) NOT NULL,
        UltimoValorTicketPromedio INT NULL
    );
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

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ProyeccionTicketStaff_TicketPromedio'
      AND parent_object_id = OBJECT_ID('dbo.ProyeccionTicketStaff')
)
BEGIN
    ALTER TABLE dbo.ProyeccionTicketStaff
        ADD CONSTRAINT CK_ProyeccionTicketStaff_TicketPromedio
            CHECK (TicketPromedio >= 0);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_ProyeccionTicketStaff_UltimoValorTicketPromedio'
      AND parent_object_id = OBJECT_ID('dbo.ProyeccionTicketStaff')
)
BEGIN
    ALTER TABLE dbo.ProyeccionTicketStaff
        ADD CONSTRAINT CK_ProyeccionTicketStaff_UltimoValorTicketPromedio
            CHECK (UltimoValorTicketPromedio IS NULL OR UltimoValorTicketPromedio >= 0);
END;
GO
