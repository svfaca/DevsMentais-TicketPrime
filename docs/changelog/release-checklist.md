# Release Checklist Final - AV2 DoD (Definition of Done)

Este checklist confirma o cumprimento integral dos requisitos operacionais, de negócio e de segurança exigidos para a entrega da AV2 da disciplina.

## 📋 Itens de Avaliação Entregues

- [x] **Item 1: Decisão Arquitetural (ADR)**  
  Arquivo `adr.md` criado em `/docs` contendo as seções exatas: `## Contexto`, `## Decisão` e `## Consequências`.

- [x] **Item 2: Trade-offs no ADR**  
  Seção `## Consequências` do ADR detalhando explicitamente os `Prós:` e os `Contras:` da decisão técnica de validação em aplicação.

- [x] **Item 3: Matriz de Riscos**  
  Matriz de riscos criada no arquivo `/docs/operacao.md` contendo as colunas exatas: `Risco`, `Probabilidade`, `Impacto` e `Ação`.

- [x] **Item 4: Gatilhos de Risco**  
  A Tabela de Riscos em `/docs/operacao.md` inclui a coluna extra `Gatilho` preenchida com o evento desencadeador claro para cada mitigação.

- [x] **Item 5: Métricas Operacionais**  
  Definição de métrica no arquivo `/docs/operacao.md` estruturada com as tags exatas: `Fórmula:`, `Fonte de Dados:` e `Frequência:`.

- [x] **Item 6: Ação da Métrica**  
  Campo exato `Ação se Violado:` incluído nas métricas operacionais especificando as ações mitigadoras de escalada técnica.

- [x] **Item 7: Objetivo de Serviço (SLO)**  
  Promessa do sistema definida sob o termo exato `SLO:` acompanhada da porcentagem de disponibilidade (`99.9%`) e da janela de tempo (`7 dias`).

- [x] **Item 8: Error Budget Policy**  
  Documento detalhando as obrigações da equipe técnica de congelamento de deploys quando o orçamento falhar, sob a tag exata `Error Budget Policy:`.

- [x] **Item 9: Segurança de Código (SSDF)**  
  Garantia de que nenhuma string de conexão ou parâmetros confidenciais (`Password=`, `User Id=`) foram escritos ou expostos nos arquivos `.cs`.

- [x] **Item 10: Checklist Final (DoD)**  
  Este arquivo (`release_checklist_final.md`) entregue na raiz do projeto com todas as caixas marcadas como concluídas (`[x]`).

---

## 🛠️ Endpoints API Implementados em C# & Dapper

- [x] **GET `/api/reservas/{cpf}`**:
  Traz as reservas do cliente mapeado por CPF. A consulta utiliza a biblioteca Dapper com um `INNER JOIN` associativo de banco para retornar o Nome do Evento (`NomeEvento`) e não apenas o identificador.

- [x] **POST `/api/reservas`**:
  Realiza a reserva de ingresso com suporte a cupom e assento. O motor valida as 4 regras de negócio e bloqueia com `Status Code 400 Bad Request` se falhar:
  - [x] *Regra R1 (Integridade)*: Rejeição caso o `UsuarioCpf` ou o `EventoId` não existam em suas respectivas tabelas.
  - [x] *Regra R2 (Limite por CPF)*: Restrição de no máximo 2 reservas ativas por CPF para o mesmo `EventoId`.
  - [x] *Regra R3 (Controle de Estoque)*: Verificação se o total de reservas ativas atinge a capacidade total máxima do evento.
  - [x] *Regra R4 (Motor de Cupons)*: Verificação e aplicação de cupom apenas se o preço padrão do evento for maior ou igual ao `ValorMinimoRegra` do cupom.
