create table RestartSchedules
(
Id bigint primary key identity(1,1),
RestartTime Datetime not null,
DeadLine Datetime not null,
IsAcknowledged bit not null
)
GO