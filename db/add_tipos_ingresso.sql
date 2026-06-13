-- Adiciona modelagem de tipos de ingresso por evento
-- Inclui suporte para reservas por tipo

BEGIN;

CREATE TABLE IF NOT EXISTS TiposIngresso (
    Id SERIAL PRIMARY KEY,
    EventoId INTEGER NOT NULL REFERENCES Eventos(Id) ON DELETE CASCADE,
    Nome TEXT NOT NULL,
    Preco NUMERIC(10,2) NOT NULL,
    EstoqueTotal INTEGER NOT NULL,
    Ativo BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT ck_tipos_ingresso_preco_nonnegative CHECK (Preco >= 0),
    CONSTRAINT ck_tipos_ingresso_estoque_positive CHECK (EstoqueTotal > 0)
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'uq_tipos_ingresso_evento_nome'
    ) THEN
        ALTER TABLE TiposIngresso
        ADD CONSTRAINT uq_tipos_ingresso_evento_nome UNIQUE (EventoId, Nome);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS IX_TiposIngresso_EventoId_Ativo
    ON TiposIngresso (EventoId, Ativo);

ALTER TABLE Reservas ADD COLUMN IF NOT EXISTS TipoIngressoId INTEGER;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_reservas_tipoingresso'
    ) THEN
        ALTER TABLE Reservas
        ADD CONSTRAINT fk_reservas_tipoingresso
        FOREIGN KEY (TipoIngressoId) REFERENCES TiposIngresso(Id);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS IX_Reservas_EventoId_TipoIngressoId
    ON Reservas (EventoId, TipoIngressoId);

COMMIT;
