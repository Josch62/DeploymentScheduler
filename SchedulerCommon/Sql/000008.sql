create table DeadlineBackup
(
Id bigint primary key identity(1,1),
ScopeId nchar(100) not null,
Revision nchar(4) not null,
Deadline Datetime not null
)
GO
create table Assignments
(
Id bigint primary key identity(1,1),
ScopeId nchar(100) not null,
Revision nchar(4) not null,
Purpose int not null
)
GO