USE TicketPrime;
GO
CREATE TABLE Usuarios
(
    Cpf VARCHAR(11) PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL,
    SenhaHash VARCHAR(128) NOT NULL,
    TipoConta VARCHAR(20) NOT NULL DEFAULT 'usuario'
);

CREATE TABLE Eventos
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nome VARCHAR(200) NOT NULL,
    CapacidadeTotal INT NOT NULL,
    DataEvento DATETIME NOT NULL,
    PrecoPadrao DECIMAL(10,2) NOT NULL,
    CriadoPorCpf VARCHAR(11) NULL,

    FOREIGN KEY (CriadoPorCpf) REFERENCES Usuarios(Cpf)
);

CREATE TABLE Cupons
(
    Codigo VARCHAR(50) PRIMARY KEY,
    PorcentagemDesconto DECIMAL(5,2) NOT NULL,
    ValorMinimoRegra DECIMAL(10,2) NOT NULL,
    CriadoPorCpf VARCHAR(11) NULL,

    FOREIGN KEY (CriadoPorCpf) REFERENCES Usuarios(Cpf)
);

CREATE TABLE Reservas
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioCpf VARCHAR(11) NOT NULL,
    EventoId INT NOT NULL,
    CupomUtilizado VARCHAR(50) NULL,
    ValorFinalPago DECIMAL(10,2) NOT NULL,

    FOREIGN KEY (UsuarioCpf) REFERENCES Usuarios(Cpf),
    FOREIGN KEY (EventoId) REFERENCES Eventos(Id),
    FOREIGN KEY (CupomUtilizado) REFERENCES Cupons(Codigo)
);