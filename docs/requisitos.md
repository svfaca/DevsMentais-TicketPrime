# Requisitos do Sistema TicketPrime

## Histórias de Usuário

### US01 - Cadastro de Usuário ✅
Como usuário, Quero me cadastrar informando CPF, nome, email e senha, Para ter meus dados registrados no sistema.

**Status:** Implementado
- Endpoint: `POST /api/auth/register`
- Banco de dados: PostgreSQL

### US02 - Cadastro de Eventos ✅
Como administrador, Quero cadastrar eventos informando nome, capacidade total, data do evento, preço padrão e categoria, Para disponibilizar eventos no sistema.

**Status:** Implementado
- Endpoint: `POST /api/eventos` (requer autenticação admin)
- Banco de dados: Armazenado em PostgreSQL

### US03 - Visualização de Eventos ✅
Como usuário, Quero visualizar os eventos cadastrados, Para escolher um evento de interesse.

**Status:** Implementado
- Endpoints: `GET /api/eventos/publico` (sem autenticação), `GET /api/eventos` (com autenticação)
- Frontend com carousel dinâmico em events-hero.js

### US04 - Cadastro de Cupons ✅
Como administrador, Quero cadastrar cupons de desconto informando código, porcentagem de desconto e valor mínimo de regra, Para disponibilizar promoções para os eventos.

**Status:** Implementado
- Endpoint: `POST /api/cupons` (requer autenticação admin)
- Endpoint: `GET /api/cupons` (listar cupons)

### US05 - Reserva de Ingressos ✅
Como usuário, Quero fazer reserva de ingressos para um evento, Para garantir minha presença.

**Status:** Implementado
- Endpoint: `POST /api/reservas` (cria reserva com assento)
- Endpoint: `GET /api/reservas` (lista reservas do usuário)
- Endpoint: `DELETE /api/reservas/{id}` (cancela reserva)
- Endpoint: `GET /api/eventos/{id}/assentos-ocupados` (verifica disponibilidade)

### US08 - Tipos de Ingresso por Evento ✅
Como administrador, Quero configurar múltiplos tipos de ingresso em cada evento, Para vender categorias de acesso com preços e estoques distintos.

**Status:** Implementado
- Endpoint: `POST /api/eventos/{eventoId}/tipos-ingresso` (cria tipo)
- Endpoint: `GET /api/eventos/{eventoId}/tipos-ingresso` (lista tipos ativos, público)
- Endpoint: `GET /api/tipos-ingresso` (lista tipos dos eventos do admin)
- Endpoint: `DELETE /api/tipos-ingresso/{id}` (desativa tipo)

### US09 - Detalhes de Viagem por Evento ✅
Como administrador, Quero informar origem, destino, companhia e horários de partida/chegada em eventos da categoria viagens, Para vender ingressos de viagem com dados completos.

**Status:** Implementado
- Endpoint: `GET /api/eventos/{eventoId}/viagem` (consulta detalhes de viagem)
- Endpoint: `PUT /api/eventos/{eventoId}/viagem` (cria/atualiza detalhes)
- Endpoint: `DELETE /api/eventos/{eventoId}/viagem` (remove detalhes)
- Regra: somente eventos com categoria `viagens` aceitam detalhes de viagem

### US06 - Authentificação de Admin ✅
Como administrador, Quero fazer login no sistema, Para acessar funcionalidades administrativas.

**Status:** Implementado
- Endpoint: `POST /api/auth/login` (login com CPF e senha)
- Endpoint: `POST /api/auth/bootstrap-admin` (bootstrap do primeiro admin)
- Endpoint: `POST /api/auth/register-admin` (registrar novo admin por admin existente)

### US07 - Perfil do Usuário ✅
Como usuário autenticado, Quero visualizar meu perfil, Para gerenciar meus dados.

**Status:** Implementado
- Endpoint: `GET /api/me` (retorna dados do usuário autenticado)

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

---

### Criação de reserva com assento válido

Dado que um usuário autenticado deseja fazer uma reserva  
Quando ele envia um evento_id e assento_numero válidos  
Então o sistema deve criar a reserva e retornar sucesso (201).

---

### Criação de reserva com assento duplicado

Dado que um assento já foi reservado em um evento  
Quando outro usuário tenta reservar o mesmo assento  
Então o sistema deve retornar erro 400 (Assento indisponível).

---

### Cancelamento de reserva

Dado que um usuário tem uma reserva ativa  
Quando ele requisita o cancelamento da reserva  
Então o sistema deve deletar a reserva e liberar o assento.

---

### Login com credenciais válidas

Dado que um usuário está registrado no sistema  
Quando ele fornece CPF e senha corretos  
Então o sistema deve retornar um token JWT.

---

### Login com credenciais inválidas

Dado que um usuário tenta fazer login  
Quando ele fornece CPF ou senha incorretos  
Então o sistema deve retornar erro 401 (Não autorizado).