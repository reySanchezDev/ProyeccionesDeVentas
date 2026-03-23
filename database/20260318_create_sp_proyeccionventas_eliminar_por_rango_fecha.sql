CREATE OR ALTER PROCEDURE dbo.sp_ProyeccionVentas_EliminarPorRangoFecha
    @FechaInicio DATE,
    @FechaFin DATE
AS
BEGIN
    SET NOCOUNT ON;

    IF @FechaInicio IS NULL OR @FechaFin IS NULL
        THROW 50001, 'FechaInicio y FechaFin son obligatorias.', 1;

    IF @FechaInicio > @FechaFin
        THROW 50002, 'FechaInicio no puede ser mayor que FechaFin.', 1;

    DELETE FROM dbo.ProyeccionVentas
    WHERE Fecha >= @FechaInicio
      AND Fecha <= @FechaFin;

    SELECT @@ROWCOUNT AS RegistrosEliminados;
END;
