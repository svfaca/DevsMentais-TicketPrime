-- Adicionar coluna Status/Ativa na tabela Usuarios (se PostgreSQL)
ALTER TABLE Usuarios ADD COLUMN IF NOT EXISTS Ativa BOOLEAN DEFAULT TRUE;

-- Para SQL Server, use:
-- ALTER TABLE Usuarios ADD Ativa BIT DEFAULT 1;

-- Garantir que o admin teste está ativo
UPDATE Usuarios SET Ativa = TRUE WHERE Cpf = '90000000002';

-- Verificar
SELECT Cpf, Nome, TipoConta, Ativa FROM Usuarios WHERE TipoConta = 'adm';
