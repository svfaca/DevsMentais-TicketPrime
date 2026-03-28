-- ═══════════════════════════════════════════════════════════════════
-- INSERIR EVENTOS COM IMAGENS
-- Execute este script no banco Neon PostgreSQL
-- ═══════════════════════════════════════════════════════════════════

TRUNCATE TABLE eventos CASCADE;

INSERT INTO eventos (nome, capacidadetotal, dataevento, precopadrao, criadoporcpf)
VALUES 
    ('Luan Santana - Para Sempre', 5000, '2026-04-15 20:00:00', 150.00, '90000000002'),
    ('Matue & Brandão - Encontro de Gerações', 8000, '2026-05-10 20:00:00', 180.00, '90000000002'),
    ('Leo Santana - Festa do Povo', 10000, '2026-07-12 20:00:00', 80.00, '90000000002'),
    ('Bienal Internacional do Livro - São Paulo 2026', 15000, '2026-09-02 10:00:00', 45.00, '90000000002'),
    ('Futurecom 2026 - Festival de Tecnologia e Inovação', 6000, '2026-09-15 09:00:00', 120.00, '90000000002'),
    ('CCXP 2026 - Comic Con Experience', 20000, '2026-10-05 10:00:00', 95.00, '90000000002'),
    ('Kanye West - O Retorno', 12000, '2026-10-28 20:00:00', 350.00, '90000000002'),
    ('Swing Gala Festival 2026', 3000, '2026-11-12 20:00:00', 180.00, '90000000002');

-- Verificar inserção
SELECT id, nome, capacidadetotal, dataevento, precopadrao FROM eventos ORDER BY dataevento;
