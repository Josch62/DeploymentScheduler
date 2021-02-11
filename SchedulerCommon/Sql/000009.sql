create table ServiceSchedules
(
Id bigint primary key identity(1,1),
ExecuteTime Datetime not null,
IsAcknowledged bit not null
)
GO