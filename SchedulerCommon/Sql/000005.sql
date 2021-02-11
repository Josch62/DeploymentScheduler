create table UpdatesInstallStatus
(
Id bigint primary key identity(1,1),
IsInstalling bit not null
)
GO
INSERT INTO UpdatesInstallStatus (IsInstalling) VALUES ('0')
GO