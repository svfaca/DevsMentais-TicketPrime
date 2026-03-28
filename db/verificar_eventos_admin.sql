-- ═══════════════════════════════════════════════════════════════════
-- VERIFICAR EVENTOS E ADMINS ATIVOS
-- ═══════════════════════════════════════════════════════════════════

-- 1. EVENTOS SEM ADMIN VINCULADO (CriadoPorCpf = NULL)
SELECT COUNT(*) as "Eventos sem admin vinculado"
FROM Eventos
WHERE CriadoPorCpf IS NULL;

-- 2. LISTA DE EVENTOS ÓRFÃOS
SELECT id, nome, dataevento, CriadoPorCpf
FROM Eventos
WHERE CriadoPorCpf IS NULL
ORDER BY dataevento DESC;

-- 3. EVENTOS COM ADMIN DELETADO (CriadoPorCpf não existe em Usuarios)
SELECT e.id, e.nome, e.dataevento, e.CriadoPorCpf
FROM Eventos e
LEFT JOIN Usuarios u ON e.CriadoPorCpf = u.Cpf
WHERE e.CriadoPorCpf IS NOT NULL AND u.Cpf IS NULL
ORDER BY e.dataevento DESC;

-- 4. EVENTOS COM USUÁRIO NÃO-ADMIN (TipoConta != 'adm')
SELECT e.id, e.nome, e.dataevento, e.CriadoPorCpf, u.TipoConta
FROM Eventos e
INNER JOIN Usuarios u ON e.CriadoPorCpf = u.Cpf
WHERE u.TipoConta != 'adm'
ORDER BY e.dataevento DESC;

-- 5. ADMINS ATIVOS E SEUS EVENTOS
SELECT u.Cpf, u.Nome, u.TipoConta, COUNT(e.id) as "Total de eventos"
FROM Usuarios u
LEFT JOIN Eventos e ON u.Cpf = e.CriadoPorCpf
WHERE u.TipoConta = 'adm'
GROUP BY u.Cpf, u.Nome, u.TipoConta
ORDER BY COUNT(e.id) DESC;

-- 6. RESUMO GERAL
SELECT 
    (SELECT COUNT(*) FROM Usuarios WHERE TipoConta = 'adm') as "Total de admins",
    (SELECT COUNT(*) FROM Eventos) as "Total de eventos",
    (SELECT COUNT(*) FROM Eventos WHERE CriadoPorCpf IS NULL) as "Eventos órfãos",
    (SELECT COUNT(*) FROM Eventos WHERE CriadoPorCpf IS NOT NULL) as "Eventos com admin"
