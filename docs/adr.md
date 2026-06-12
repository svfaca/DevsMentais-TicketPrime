# Architectural Decision Record (ADR) - Validação das Regras de Negócio na Camada de Aplicação

Este documento registra a decisão arquitetural sobre onde devem residir as validações das regras de negócio complexas do motor de vendas do TicketPrime.

## Contexto

O sistema TicketPrime exige a implementação de quatro regras de negócio inegociáveis para a reserva de ingressos (POST `/api/reservas`):
1. **Regra R1 (Integridade)**: A reserva só pode ser criada se o CPF do usuário e o ID do evento existirem.
2. **Regra R2 (Limite por CPF)**: O mesmo CPF só pode realizar no máximo duas reservas para o mesmo evento.
3. **Regra R3 (Controle de Estoque)**: Bloqueio de novas compras caso a capacidade total do evento seja atingida.
4. **Regra R4 (Motor de Cupons)**: Aplicação de cupom de desconto com validação de valor mínimo.

Havia duas abordagens possíveis para essa implementação:
- Implementar as validações no banco de dados relacional através de restrições (`CONSTRAINTS`), gatilhos (`TRIGGERS`) ou procedimentos armazenados (`STORED PROCEDURES`).
- Implementar as validações diretamente na camada de aplicação (código C#) e manter o Dapper apenas para operações de leitura e escrita simples (`INSERT`/`SELECT`).

## Decisão

Optou-se por implementar todas as quatro regras de negócio (R1 a R4) **diretamente no código C# na Minimal API**, no ponto de entrada de criação de reservas, executando as consultas e validações lógicas na memória da aplicação antes de persistir o registro de forma segura.

O Dapper é mantido estritamente como um mapeador objeto-relacional (ORM) leve e de alto desempenho, sem sobrecarregar a infraestrutura do banco de dados com lógica processual complexa.

## Consequências

A escolha traz consequências diretas para a escalabilidade, manutenção e segurança da plataforma:

*   **Prós:**
    *   **Facilidade de Manutenção**: Todo o fluxo de negócios e validações fica concentrado no mesmo repositório e na mesma linguagem (C#), evitando que desenvolvedores precisem inspecionar scripts SQL separados no PostgreSQL para entender o comportamento do sistema.
    *   **Feedback Detalhado ao Usuário**: Facilita o envio de respostas HTTP com `Status Code 400 Bad Request` detalhando de forma exata e amigável qual regra falhou (ex: "O mesmo CPF nao pode ter mais de 2 reservas para o mesmo EventoId").
    *   **Testabilidade Elevada**: Permite a escrita de testes automatizados e simulação de cenários de erro e sucesso sem a necessidade de instanciar ou inicializar triggers e estados complexos de banco de dados.
    *   **Independência de Infraestrutura**: Reduz o acoplamento do TicketPrime com recursos proprietários ou dialetos do PostgreSQL, facilitando uma eventual migração de banco de dados.

*   **Contras:**
    *   **Condições de Corrida**: Validações de concorrência baseadas em software (como o controle de estoque R3) podem falhar sob concorrência maciça (ex: centenas de requests no mesmo milissegundo) caso não sejam combinadas com transações adequadas ou travas na tabela (`SELECT ... FOR UPDATE`).
    *   **Latência de Rede Intermediária**: Exige múltiplas viagens de ida e volta (Round-Trips) ao banco de dados relacional (para verificar a existência do CPF, contar reservas existentes, etc.) antes de realizar o `INSERT`, gerando um pequeno overhead no tempo total da requisição.
