create table AutoUpdateSchedule
(
Id bigint primary key identity(1,1),
IsEnforcing bit not null,
Schedules ntext
)
GO
INSERT INTO AutoUpdateSchedule (IsEnforcing) VALUES ('0')
GO