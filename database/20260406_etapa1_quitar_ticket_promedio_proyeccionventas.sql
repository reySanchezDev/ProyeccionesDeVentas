USE [ReportesLS];
GO

IF COL_LENGTH('dbo.ProyeccionVentas', 'TicketPromedio') IS NOT NULL
BEGIN
    DECLARE @Sql NVARCHAR(MAX) = N'';

    SELECT @Sql = STRING_AGG(
        N'ALTER TABLE dbo.ProyeccionVentas DROP CONSTRAINT [' + dc.name + N'];',
        CHAR(10)
    )
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.ProyeccionVentas')
      AND c.name = 'TicketPromedio';

    IF NULLIF(@Sql, N'') IS NOT NULL
    BEGIN
        EXEC sp_executesql @Sql;
    END;

    SET @Sql = N'';

    SELECT @Sql = STRING_AGG(
        N'DROP INDEX [' + i.name + N'] ON dbo.ProyeccionVentas;',
        CHAR(10)
    )
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic
        ON ic.object_id = i.object_id
       AND ic.index_id = i.index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id
       AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID('dbo.ProyeccionVentas')
      AND i.is_primary_key = 0
      AND i.is_unique_constraint = 0
      AND c.name = 'TicketPromedio';

    IF NULLIF(@Sql, N'') IS NOT NULL
    BEGIN
        EXEC sp_executesql @Sql;
    END;

    ALTER TABLE dbo.ProyeccionVentas
        DROP COLUMN TicketPromedio;
END;
GO
