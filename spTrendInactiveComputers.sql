begin tran

GO

exec sp_rename 'TREND_InactiveComputerCounts', 'TREND_InactiveComputerCounts_old'
exec sp_rename 'TREND_InactiveComputer_Current', 'TREND_InactiveComputer_Current_old'
exec sp_rename 'TREND_InactiveComputer_Previous', 'TREND_InactiveComputer_Previous_old'


GO

drop procedure spTrendInactiveComputers

GO

create procedure spTrendInactiveComputers
	@force as int = 0,
	@collectionguid as uniqueidentifier = '01024956-1000-4CDB-B452-7DB0CFF541B6'
as
if not exists (select 1 from sys.objects where type = 'u' and name = 'TREND_InactiveComputerCounts')
begin
	create table TREND_InactiveComputerCounts (
		[_exec_id] int not null,
		[timestamp] datetime not null,
		[collectionguid] uniqueidentifier not null,
		[Managed machines] int not null,
		[Inactive computers (7 days)] int not null,
		[New Inactive computers] int not null,
		[New Active computers] int not null,
		[Inactive computers (17 days)] int not null
	)
end

if not exists (select 1 from sys.objects where type = 'u' and name = 'TREND_InactiveComputer_Current')
begin
	CREATE TABLE [TREND_InactiveComputer_Current] (guid uniqueidentifier not null, collectionguid uniqueidentifier not null, _exec_time datetime not null)
	CREATE UNIQUE CLUSTERED INDEX [IX_TREND_InactiveComputer_Current] ON [dbo].[TREND_InactiveComputer_Current] 
		(
			[CollectionGuid] ASC, [Guid] ASC
	)
end

if not exists (select 1 from sys.objects where type = 'u' and name = 'TREND_InactiveComputer_Previous')
begin
	CREATE TABLE [TREND_InactiveComputer_Previous] (guid uniqueidentifier not null, collectionguid uniqueidentifier not null, _exec_time datetime not null)
	CREATE UNIQUE CLUSTERED INDEX [IX_TREND_InactiveComputer_Previous] ON [dbo].[TREND_InactiveComputer_Previous] 
		(
			[CollectionGuid] ASC, [Guid] ASC
	)
end

if ((select MAX(_exec_time) from TREND_InactiveComputer_Current where collectionguid = @Collectionguid and _exec_time >  dateadd(hour, -23, getdate())) is null) or (@force = 1)
begin
	-- STAGE 1: If we have current data, save it in the _previous table
	if (select count (*) from TREND_InactiveComputer_Current) > 0
		begin
			delete from TREND_InactiveComputer_Previous where CollectionGuid = '{0}'
			insert TREND_InactiveComputer_Previous (guid, collectionguid, _exec_time)
			select * from TREND_InactiveComputer_Current where CollectionGuid = @CollectionGuid

		-- STAGE 2: Insert current data in the current table
		delete from TREND_InactiveComputer_Current where CollectionGuid = @CollectionGuid
		insert TREND_InactiveComputer_Current (guid, CollectionGuid, _exec_time)
		SELECT DISTINCT(c.ResourceGuid) as 'Guid', @collectionguid, getdate()
		  FROM CollectionMembership c
		 INNER JOIN (
						select [ResourceGuid]
						  from dbo.ResourceUpdateSummary
						 where InventoryClassGuid = '9E6F402A-6A45-4CBA-9299-C2323F73A506' 		
						 group by [ResourceGuid]
						having max([ModifiedDate]) < GETDATE() - 7
			 ) as dt 
			ON c.ResourceGuid = dt.ResourceGuid	
		 WHERE c.CollectionGuid = @CollectionGuid
		 
		 --STAGE 3: Extract the add/drop counts and insert data in the trending table
		 declare @added as int, @removed as int
		 -- Added in c
			 set @added = (
					select count(*)
					  from TREND_InactiveComputer_Current c
					  full join TREND_InactiveComputer_Previous p
						on p.guid = c.guid
					 where p.guid is null
					   and p.collectionguid = @CollectionGuid
			)
			    
			-- Removed in c
			 set @removed = (
					select count(*)
					  from TREND_InactiveComputer_Current c
					  full join TREND_InactiveComputer_Previous p
						on p.guid = c.guid
					 where c.guid is null
					   and p.CollectionGuid = @CollectionGuid
			)

		declare @managed as int, @inactive_1 as int, @inactive_2 as int
		set @managed = (select count(distinct(ResourceGuid)) from CollectionMembership where CollectionGuid = @CollectionGuid)
		set @inactive_1 = (
			select count(distinct(c.ResourceGuid))
			  from CollectionMembership c
			 INNER JOIN
				(
				select [ResourceGuid]
				  from dbo.ResourceUpdateSummary
				 where InventoryClassGuid = '9E6F402A-6A45-4CBA-9299-C2323F73A506' 		
				 group by [ResourceGuid]
				having max([ModifiedDate]) < GETDATE() - 7
				 ) as dt 
				ON c.ResourceGuid = dt.ResourceGuid	
			 where c.CollectionGuid = @collectionguid
		)
		set @inactive_2 = (
			select count(distinct(c.ResourceGuid))
			  from CollectionMembership c
			 INNER JOIN
				(
				select [ResourceGuid]
				  from dbo.ResourceUpdateSummary
				 where InventoryClassGuid = '9E6F402A-6A45-4CBA-9299-C2323F73A506' 		
				 group by [ResourceGuid]
				having max([ModifiedDate]) < GETDATE() - 17
				 ) as dt 
				ON c.ResourceGuid = dt.ResourceGuid	
			 where c.CollectionGuid = @collectionguid
		)
		declare @execid as int
			set @execid = (select isnull(max(_exec_id), 0) from TREND_InactiveComputerCounts where collectionguid = @collectionguid) + 1

		insert TREND_InactiveComputerCounts (_exec_id, timestamp, collectionguid, [Managed machines], [inactive computers (7 days)], [New Inactive Computers], [New Active Computers], [Inactive Computers (17 days)])
		values (@execid, getdate(), @collectionguid, @managed, @inactive_1, @added, @removed, @inactive_2)
	end
	else
	begin
		  delete from TREND_InactiveComputer_Current where collectionguid = @collectionguid
  		  insert TREND_InactiveComputer_Current (guid, collectionguid, _exec_time)
		  select distinct(c.ResourceGuid) as 'Guid', @collectionguid, getdate()
			  from CollectionMembership c
			 INNER JOIN
				(
				select [ResourceGuid]
				  from dbo.ResourceUpdateSummary
				 where InventoryClassGuid = '9E6F402A-6A45-4CBA-9299-C2323F73A506' 		
				 group by [ResourceGuid]
				having max([ModifiedDate]) < GETDATE() - 17
				 ) as dt 
				ON c.ResourceGuid = dt.ResourceGuid	
			 where c.CollectionGuid = @collectionguid
	end
end

select * from TREND_InactiveComputerCounts where CollectionGuid = @collectionguid  order by _exec_id desc

GO

exec spTrendInactiveComputers @CollectionGuid='6410074B-FFFF-FFFF-FFFF-0C8803328385'

GO

insert TREND_InactiveComputer_current (guid, collectionguid, _exec_time) select guid, '311E8DAE-2294-4FF2-B9EF-B3D6A84183CB' as CollectionGuid, [_exec_time] from TREND_InactiveComputer_current_old
insert TREND_InactiveComputer_previous (guid, collectionguid, _exec_time) select guid, '311E8DAE-2294-4FF2-B9EF-B3D6A84183CB' as CollectionGuid, [_exec_time] from TREND_InactiveComputer_previous_old

insert TREND_InactiveComputerCounts ([_exec_id], [timestamp], [collectionguid], [Managed machines], [Inactive computers (7 days)], [New Inactive computers], [New Active computers], [Inactive computers (17 days)])
select [_exec_id], [timestamp], '311E8DAE-2294-4FF2-B9EF-B3D6A84183CB' as CollectionGuid, [Managed machines], [Inactive computers (7 days)], [New Inactive computers], [New Active computers], [Inactive computers (17 days)] from TREND_InactiveComputerCounts_old

GO

commit tran