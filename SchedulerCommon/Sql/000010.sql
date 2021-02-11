create table EvaluatedApplications
(
Id bigint primary key identity(1,1),
ObjectId nchar(100) not null,
Revision nchar(4)
)
GO