-- Uso:
-- psql "$DATABASE_URL" -v admin_cpf="'90000000002'" -f db/corrigir_eventos_sem_criador.sql

BEGIN;

WITH admin_valido AS (
    SELECT Cpf
    FROM Usuarios
    WHERE Cpf = :admin_cpf
      AND TipoConta = 'adm'
)
UPDATE Eventos e
SET CriadoPorCpf = admin_valido.Cpf
FROM admin_valido
WHERE e.CriadoPorCpf IS NULL;

ALTER TABLE Eventos
ALTER COLUMN CriadoPorCpf SET NOT NULL;

COMMIT;