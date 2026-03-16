# Requisitos do Sistema TicketPrime

## Histórias de Usuário

### US01
Como usuário, Quero me cadastrar informando CPF, nome e email, Para ter meus dados registrados no sistema.

### US02
Como administrador, Quero cadastrar eventos informando nome, capacidade total, data do evento e preço padrão, Para disponibilizar eventos no sistema.

### US03
Como usuário, Quero visualizar os eventos cadastrados, Para escolher um evento de interesse.

### US04
Como administrador, Quero cadastrar cupons de desconto informando código, porcentagem de desconto e valor mínimo de regra, Para disponibilizar promoções para os eventos.

---

## Critérios de Aceitação (BDD)

### Cadastro de usuário

Dado que um usuário deseja se cadastrar no sistema  
Quando ele envia um CPF que já está cadastrado  
Então a API deve retornar erro 400 informando que o CPF já existe.

---

### Cadastro de evento

Dado que um administrador deseja cadastrar um evento  
Quando ele envia nome, capacidade total, data do evento e preço padrão válidos  
Então o sistema deve cadastrar o evento no banco de dados.

---

### Listagem de eventos

Dado que existem eventos cadastrados no sistema  
Quando um usuário solicita a lista de eventos  
Então o sistema deve retornar todos os eventos disponíveis.