create table ContentStatus
(
Id bigint primary key identity(1,1),
ObjectId nchar(100) not null,
Revision nchar(4),
ContentLocation nchar(100) NULL,
IsDownloaded bit not null,
InstallTime Datetime NULL
)
GO