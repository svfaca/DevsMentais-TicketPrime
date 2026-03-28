-- Script para popular os eventos com as imagens
-- PostgreSQL

-- Verificar coluna existe
ALTER TABLE Eventos ADD COLUMN IF NOT EXISTS ImagemUrl TEXT;

-- Atualizar eventos com suas imagens (por nome exato)
UPDATE Eventos SET ImagemUrl = '/imagens/luan.jpg' 
WHERE Nome ILIKE '%Luan Santana%' OR Nome ILIKE '%Para Sempre%';

UPDATE Eventos SET ImagemUrl = '/imagens/matueebrandao.jpg'
WHERE Nome ILIKE '%Matue%' OR Nome ILIKE '%Brandão%' OR Nome ILIKE '%Encontro de Gerações%';

UPDATE Eventos SET ImagemUrl = '/imagens/leo.jpg'
WHERE Nome ILIKE '%Leo Santana%' OR Nome ILIKE '%Festa do Povo%';

UPDATE Eventos SET ImagemUrl = '/imagens/bienal.jpg'
WHERE Nome ILIKE '%Bienal%' OR Nome ILIKE '%Livro%';

UPDATE Eventos SET ImagemUrl = '/imagens/futurecom.png'
WHERE Nome ILIKE '%Futurecom%' OR Nome ILIKE '%Tecnologia%';

UPDATE Eventos SET ImagemUrl = '/imagens/ccxp.jpg'
WHERE Nome ILIKE '%CCXP%' OR Nome ILIKE '%Comic Con%';

UPDATE Eventos SET ImagemUrl = '/imagens/kanye.jpg'
WHERE Nome ILIKE '%Kanye%' OR Nome ILIKE '%Retorno%';

UPDATE Eventos SET ImagemUrl = '/imagens/SWING.png'
WHERE Nome ILIKE '%SWING%' OR Nome ILIKE '%Swing%';

-- Verificar resultado
SELECT Id, Nome, ImagemUrl FROM Eventos ORDER BY DataEvento DESC;
