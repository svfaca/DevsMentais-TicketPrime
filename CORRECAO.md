# Correção AV2 — DevsMentais-TicketPrime

**Grupo:** @jvdellolio, @MiriamSsntos, @patrickmqs, @svfaca, @MatheusPrudente04

| # | Item de Avaliação | Nota | Justificativa |
|---|-------------------|:----:|---------------|
| 01 | Padrão AAA nos Testes | 0,0 | Nenhum dos 6 métodos em `BasicTests.cs` tem `// Arrange`, `// Act`, `// Assert`; apenas comentários genéricos (`// Testa validação básica de CPF`) |
| 02 | Nomenclatura e Independência | 0,0 | Nomes como `DeveValidarCpfComOnzeDigitos` não seguem o padrão `Metodo_Cenario_ResultadoEsperado` exigido |
| 03 | Padrões Arquiteturais | 0,0 | Arquivo `/docs/analise_arquitetura.md` não existe |
| 04 | Violações Arquiteturais | 0,0 | Arquivo não existe |
| 05 | ADR | 0,5 | `docs/adr.md` existe com `## Contexto`, `## Decisão`, `## Consequências`, `Prós:` e `Contras:`; caminho flexibilizado (fora de `/docs/adrs/`) |
| 06 | Dívida Técnica | 0,0 | Arquivo `/docs/registro_divida_tecnica.md` não existe |
| 07 | Priorização Dívida | 0,0 | Arquivo não existe |
| 08 | Classificação Manutenção | 0,0 | Arquivo `/docs/fluxo_manutencao.md` não existe |
| 09 | Pipeline de Liberação | 0,0 | Arquivo não existe |
| 10 | Plano de Iteração | 0,0 | Arquivo `/docs/plano_iteracao.md` não existe |
| 11 | Quadro Kanban e WIP | 0,0 | Arquivo não existe |
| 12 | Matriz de Riscos | 0,0 | Apenas 3 riscos em `/docs/operacao.md` (mínimo 5); coluna `Estratégia` ausente (tabela usa `Ação` em vez de `Estratégia` + `Ação Planejada`) |
| 13 | Gatilhos de Risco | 0,5 | Coluna `Gatilho` presente com 3 riscos, todos com textos ≥20 caracteres; itens são independentes (binários) |
| 14 | Métrica DORA | 0,0 | Métrica em `operacao.md` incompleta: faltam campos `Nome`, `O que Mede`, `Limites` |
| 15 | Métrica de Qualidade | 0,0 | Apenas uma métrica descrita; segunda métrica com 7 campos não existe |
| 16 | SLO | 0,0 | SLO mencionado (99.9%, 7 dias) mas sem estrutura de ficha com `SLI`, `Fórmula de Coleta`, `Fonte`, `Janela de Medição`, `Alvo` |
| 17 | Error Budget Policy | 0,0 | Descreve ações pós-estouro (congelamento, post-mortem) mas sem os 3 níveis graduados exigidos |
| 18 | Segurança SSDF | 0,5 | Nenhuma credencial hardcoded nos arquivos `.cs`; connection strings via `builder.Configuration` e variáveis de ambiente (`DATABASE_URL`) |
| 19 | Threat Model e Gates | 0,0 | Arquivo `/docs/seguranca_ciclo.md` não existe |
| 20 | Topologia Times e DoD | 0,0 | Arquivos `/docs/topologia_times.md` e `release_checklist_final.md` não existem |

**Nota Final: 1,5 / 10,0**

---

**Observações:**
- O código da aplicação (Backend/Program.cs) está bem estruturado, com múltiplos endpoints, regras de negócio (R1-R4), JOINs, validações e queries parametrizadas com Dapper — o trabalho de desenvolvimento foi bem executado.
- Porém, a AV2 avalia exclusivamente documentação de qualidade, arquitetura e operação (itens 03 a 20) + testes (01-02). O grupo não produziu a maioria dos documentos exigidos.
- Apenas 3 dos 19 arquivos de documentação exigidos existem: `adr.md` (aceito fora da pasta `adrs/`), `operacao.md` (parcial) e `requisitos.md`.
- Itens pontuados: 05 (ADR, caminho flexibilizado), 13 (Gatilhos de Risco com ≥20 caracteres), 18 (Segurança SSDF, zero credenciais hardcoded).
- Os testes são extremamente simples (validam constantes literais, sem interagir com serviços/banco) e não seguem o padrão AAA.
- README não contém matrículas dos integrantes, apenas @users do GitHub.
