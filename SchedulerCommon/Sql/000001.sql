create table Schedules
(
Id bigint primary key identity(1,1),
EnfType nchar(3) not null,
IsEnforced bit not null,
EnforceTime Datetime not null,
ObjectId nchar(100) not null,
Revision nchar(4),
Action nchar(1),
HasRaisedConfirm bit not null DEFAULT 0
)
GO
create table SupData(
Id bigint primary key identity(1,1),
Type nchar(3) not null,
Data ntext
)
GO
