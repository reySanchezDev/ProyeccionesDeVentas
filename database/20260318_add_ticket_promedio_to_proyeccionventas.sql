IF COL_LENGTH('dbo.ProyeccionVentas', 'TicketPromedio') IS NULL
BEGIN
    ALTER TABLE dbo.ProyeccionVentas
    ADD TicketPromedio INT NULL;
END;
