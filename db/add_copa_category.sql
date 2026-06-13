-- Torna "copa" uma categoria de evento de primeira classe
-- Execute apos add_event_category.sql em ambientes ja existentes

BEGIN;

ALTER TABLE Eventos DROP CONSTRAINT IF EXISTS ck_eventos_categoria;

ALTER TABLE Eventos
    ADD CONSTRAINT ck_eventos_categoria
    CHECK (Categoria IN ('musicais', 'cinema', 'eventos-diversos', 'copa', 'viagens'));

CREATE INDEX IF NOT EXISTS IX_Eventos_Categoria_DataEvento
    ON Eventos (Categoria, DataEvento DESC);

COMMIT;
