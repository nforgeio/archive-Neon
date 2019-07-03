create table Products
(
	Id				int primary key identity,
	Uri				nvarchar(2048),
	Manufacturer	nvarchar(32),
	Type			nvarchar(32),
	Name			nvarchar(32),
	Retailer		nvarchar(32)
)

create table Links
(
	Id				varchar(32) primary key,
	Source			varchar(32),
	ProductId		int references Products(Id)
)

create table Hits
(
	Id				bigint primary key identity,
	TimeUtc 		datetime2 default getutcdate(),
	IP				varchar(32),
	Source			nvarchar(32),
	Manufacturer	nvarchar(32),
	ProductType		nvarchar(32),
	ProductName		nvarchar(32),
	Retailer		nvarchar(32)
)

create index Hits_TimeUtc on Hits(TimeUtc)
go
