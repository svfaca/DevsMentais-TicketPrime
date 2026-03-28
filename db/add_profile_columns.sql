-- Adicionar colunas Telefone e FotoPerfil na tabela Usuarios (PostgreSQL)
ALTER TABLE Usuarios ADD COLUMN IF NOT EXISTS Telefone TEXT;
ALTER TABLE Usuarios ADD COLUMN IF NOT EXISTS FotoPerfil TEXT;

-- Para SQL Server, use:
-- ALTER TABLE Usuarios ADD Telefone VARCHAR(20);
-- ALTER TABLE Usuarios ADD FotoPerfil TEXT;

SELECT * FROM Usuarios LIMIT 5;
