-- Adiciona suporte a exclusao logica de eventos

BEGIN;

ALTER TABLE Eventos ADD COLUMN IF NOT EXISTS Ativo BOOLEAN;

UPDATE Eventos
SET Ativo = TRUE
WHERE Ativo IS NULL;

ALTER TABLE Eventos ALTER COLUMN Ativo SET DEFAULT TRUE;
ALTER TABLE Eventos ALTER COLUMN Ativo SET NOT NULL;

CREATE INDEX IF NOT EXISTS IX_Eventos_Ativo_DataEvento
    ON Eventos (Ativo, DataEvento DESC);

COMMIT;
