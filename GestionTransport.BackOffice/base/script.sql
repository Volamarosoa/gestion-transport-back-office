
CREATE DATABASE GestionTransport;
GO

USE GestionTransport;
GO

CREATE TABLE Departement (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Nom NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255),
    Actif BIT DEFAULT 1,
    DateInsertion DATETIME DEFAULT GETDATE(),
    DateDesactivation DATETIME NULL
);

CREATE TABLE Employe (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Nom NVARCHAR(100) NOT NULL,
    Prenom NVARCHAR(100) NOT NULL,
    Matricule NVARCHAR(50),
    Telephone NVARCHAR(15),
    idDepartement INT FOREIGN KEY REFERENCES Departement(Id),
    Actif BIT DEFAULT 1,
    DateInsertion DATETIME DEFAULT GETDATE(),
    DateDesactivation DATETIME NULL
);

CREATE TABLE AdresseEmploye (
    Id INT PRIMARY KEY IDENTITY(1,1),
    IdEmploye INT FOREIGN KEY REFERENCES Employe(Id),
    Adresse NVARCHAR(255),
    Latitude DECIMAL(18,9),
    Longitude DECIMAL(18,9),
    EstPrincipale BIT DEFAULT 0,
    Actif BIT DEFAULT 1,
    DateInsertion DATETIME DEFAULT GETDATE(),
    DateDesactivation DATETIME NULL
);

CREATE TABLE SITE (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Nom NVARCHAR(100), -- Siège, Usine, Agence, ...
    Adresse NVARCHAR(255),
    Latitude DECIMAL(18,9),
    Longitude DECIMAL(18,9),
    Actif BIT DEFAULT 1, 
    DateInsertion DATETIME DEFAULT GETDATE(),
    DateDesactivation DATETIME NULL
);

CREATE TABLE Vehicule (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Matricule NVARCHAR(50),
    NombrePlaces INT,
    Actif BIT DEFAULT 1,
    DateInsertion DATETIME DEFAULT GETDATE(),
    DateDesactivation DATETIME NULL
);

CREATE TABLE TypeTransport (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Libelle NVARCHAR(100), -- Aller, Retour
    Actif BIT DEFAULT 1
);

CREATE TABLE HeureTransport (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Heure TIME,
    Libelle NVARCHAR(100), -- Matin, Soir, Nuit
    Actif BIT DEFAULT 1,
    DateInsertion DATETIME DEFAULT GETDATE(),
    DateDesactivation DATETIME NULL
);

CREATE TABLE DateTransport (
    Id INT PRIMARY KEY IDENTITY(1,1),
    DateJour DATE NOT NULL,
    Actif BIT DEFAULT 1
);

CREATE TABLE TypeAffectation (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Libelle NVARCHAR(50), -- Automatique, Manuel
    Actif BIT DEFAULT 1
);

CREATE TABLE Affectation (
    Id INT PRIMARY KEY IDENTITY(1,1),
    IdDate INT FOREIGN KEY REFERENCES DateTransport(Id),
    IdEmploye INT FOREIGN KEY REFERENCES Employe(Id),
    IdAdresse INT FOREIGN KEY REFERENCES AdresseEmploye(Id),
    IdTypeTransport INT FOREIGN KEY REFERENCES TypeTransport(Id),
    IdSite INT FOREIGN KEY REFERENCES SITE(Id),
    IdVehicule INT FOREIGN KEY REFERENCES Vehicule(Id),
    IdHeureTransport INT FOREIGN KEY REFERENCES HeureTransport(Id),
    
    EstValidee BIT NULL, -- 1 = validée, 0 = refusée, NULL = en attente
    Commentaire NVARCHAR(255),
    
    DateCreation DATETIME DEFAULT GETDATE(),
    DateValidation DATETIME NULL,
    IdType INT FOREIGN KEY REFERENCES TypeAffectation(Id) 
);

CREATE TABLE HistoriqueAffectation (
    IdHistorique INT PRIMARY KEY IDENTITY(1,1),
    IdAffectation INT FOREIGN KEY REFERENCES Affectation(Id),
    
    IdDate INT,
    IdEmploye INT,
    IdAdresse INT,
    IdTypeTransport INT,
    IdSite INT,
    IdVehicule INT,
    IdHeureTransport INT,
    
    EstValidee BIT,
    Commentaire NVARCHAR(255),
    
    DateCreation DATETIME,
    DateValidation DATETIME,
    IdType INT,
    
    DateModification DATETIME DEFAULT GETDATE()
);

-- Pour rechercher rapidement les affectations d'un employé à une date
CREATE INDEX IX_Affectation_DateEmploye ON Affectation(IdDate, IdEmploye);

-- Pour éviter les doublons de matricule
CREATE UNIQUE INDEX IX_Employe_Matricule ON Employe(Matricule) WHERE Matricule IS NOT NULL;
