# Requisitos do Sistema TicketPrime

## Histórias de Usuário

### US01
Como usuário, quero me cadastrar informando CPF, nome e email, para ter meus dados registrados no sistema.

### US02
Como administrador, quero cadastrar eventos, para disponibilizar eventos no sistema.

### US03
Como usuário, quero visualizar os eventos cadastrados, para escolher um evento de interesse.

### US04
Como administrador, quero cadastrar cupons de desconto, para disponibilizar promoções no sistema.

## Critérios de Aceitação (BDD)

### Cadastro de Usuário

Dado que um usuário deseja se cadastrar no sistema  
Quando ele envia um CPF que já está cadastrado  
Então a API deve retornar erro 400 informando que o CPF já existe.