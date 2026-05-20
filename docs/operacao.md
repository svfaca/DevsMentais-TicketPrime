# Blindagem Operacional do TicketPrime

Este documento estabelece as diretrizes de confiabilidade, monitoramento de saúde operacional e mitigação de riscos para o motor de vendas e reservas da plataforma.

## Matriz de Riscos

A tabela abaixo mapeia os principais riscos operacionais da API de vendas e o plano de ação correspondente.

| Risco | Probabilidade | Impacto | Gatilho | Ação |
| :--- | :--- | :--- | :--- | :--- |
| **Sobrevenda de Ingressos (Overbooking)** | Média | Crítico | Dois ou mais inserts de reservas ocorrem concorrentemente ultrapassando a `CapacidadeTotal` do evento. | Ativar isolamento transacional de banco de dados (`SELECT FOR UPDATE` ou nível de transação `SERIALIZABLE` no Dapper) e alertar no monitor SRE. |
| **Exaustão de Conexões no Banco de Dados** | Média | Alto | Latência de processamento das regras (R1 a R4) excede 2000ms devido a conexões travadas na fila. | Escalar verticalmente os pools de conexões no NeonDB/PostgreSQL e aplicar timeouts agressivos nas conexões da API C#. |
| **Abuso de Cupons (Ataque de Força Bruta)** | Baixa | Médio | Taxa de falha na validação do cupom (R4) ultrapassa 30% em menos de 5 minutos para um mesmo IP ou CPF. | Bloquear temporariamente o processamento de novos cupons para o IP de origem e disparar rate limiting na rota `POST /api/reservas`. |

---

## Métricas Operacionais

Para monitorar e blindar a API de reservas, definimos a seguinte métrica de confiabilidade de transações:

*   **Fórmula:** `(Quantidade de reservas processadas com sucesso (status HTTP 201 ou status HTTP 400 devidamente tratados) / Quantidade total de requisições enviadas ao endpoint POST /api/reservas) * 100`
*   **Fonte de Dados:** Logs estruturados do Application Insights (Azure APM) filtrados por endpoint e código de status HTTP.
*   **Frequência:** Coleta e consolidação a cada 1 minuto.
*   **Ação se Violado:** Disparar alerta crítico no PagerDuty para o engenheiro de plantão (SRE), desviar tráfego secundário para uma página estática de fila de espera e congelar deploys em andamento no ambiente de produção.

---

## Objetivo de Serviço (SLO)

Para garantir a melhor experiência na compra de ingressos, nos comprometemos com o seguinte objetivo operacional:

*   **SLO:** `99.9%` de requisições bem-sucedidas no motor de reservas (`POST /api/reservas`) com tempo de resposta (p95) abaixo de `500ms`, calculado em uma janela de tempo de `7 dias`.

---

## Error Budget Policy:

O Orçamento de Erro (Error Budget) representa a margem de falha tolerada para o nosso SLO (ou seja, `0.1%` de falhas permitidas na janela de 7 dias). Quando esse orçamento é totalmente esgotado, as seguintes ações tornam-se **obrigatórias** para o time técnico:

1.  **Congelamento de Features**: O time de desenvolvimento é obrigado a congelar imediatamente a esteira de implantação de novas funcionalidades no ambiente de produção.
2.  **Foco em Confiabilidade**: 100% da capacidade produtiva da equipe deve ser redirecionada para a resolução de débitos técnicos, otimização de consultas SQL e correção das falhas de infraestrutura que causaram o estouro do orçamento.
3.  **Análise de Causa Raiz (Post-Mortem)**: Conduzir um post-mortem detalhado sem culpas (Blameless Post-Mortem) em até 24 horas após o incidente para identificar e blindar o sistema contra reincidências.
4.  **Liberação da Esteira**: O desenvolvimento e deploy de novos recursos comerciais só podem ser retomados quando o indicador de SLO retornar ao patamar de conformidade seguro (acima da meta de `99.9%`).
