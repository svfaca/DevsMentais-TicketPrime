-- Adiciona categoria em Eventos e prepara filtro por tipo de nicho
-- Categorias permitidas: musicais, cinema, eventos-diversos, copa, viagens

BEGIN;

ALTER TABLE Eventos ADD COLUMN IF NOT EXISTS Categoria TEXT;

UPDATE Eventos
SET Categoria = 'musicais'
WHERE Categoria IS NULL OR btrim(Categoria) = '';

ALTER TABLE Eventos ALTER COLUMN Categoria SET DEFAULT 'musicais';
ALTER TABLE Eventos ALTER COLUMN Categoria SET NOT NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'ck_eventos_categoria'
    ) THEN
        ALTER TABLE Eventos
        ADD CONSTRAINT ck_eventos_categoria
        CHECK (Categoria IN ('musicais', 'cinema', 'eventos-diversos', 'copa', 'viagens'));
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS IX_Eventos_Categoria_DataEvento
    ON Eventos (Categoria, DataEvento DESC);

COMMIT;
