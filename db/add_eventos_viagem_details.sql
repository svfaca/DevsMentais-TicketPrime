-- Adiciona tabela de detalhes para eventos da categoria viagens
CREATE TABLE IF NOT EXISTS EventosViagem (
    EventoId INTEGER PRIMARY KEY REFERENCES Eventos(Id) ON DELETE CASCADE,
    Origem TEXT NOT NULL,
    Destino TEXT NOT NULL,
    Companhia TEXT NOT NULL,
    PartidaEm TIMESTAMPTZ NOT NULL,
    ChegadaEm TIMESTAMPTZ NOT NULL,
    BagagemIncluida BOOLEAN NOT NULL DEFAULT FALSE,
    Observacoes TEXT,
    CONSTRAINT ck_eventos_viagem_periodo CHECK (ChegadaEm > PartidaEm)
);
