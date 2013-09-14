create procedure spTrendInactiveComputers
	@force as int = 0
as
if not exists (select 1 from sys.objects where type = 'u' and name = 'TREND_ActiveComputerCounts')
begin
	create table TREND_ActiveComputerCounts (
		[_exec_id] int not null,
		[timestamp] datetime not null,
		[Managed machines] int not null,
		[Inactive computers (7 days)] int not null,
		[New Inactive computers] int not null,
		[New Active computers] int not null,
		[Inactive computers (17 days)] int not null
	)
end

if not exists (select 1 from sys.objects where type = 'u' and name = 'TREND_InactiveComputer_Current')
begin
	CREATE TABLE [TREND_InactiveComputer_Current] (guid uniqueidentifier not null, _exec_time datetime not null)
	CREATE UNIQUE CLUSTERED INDEX [IX_TREND_InactiveComputer_Current] ON [dbo].[TREND_InactiveComputer_Current] 
		(
			[Guid] ASC
	)
end

if not exists (select 1 from sys.objects where type = 'u' and name = 'TREND_InactiveComputer_Previous')
begin
	CREATE TABLE [TREND_InactiveComputer_Previous] (guid uniqueidentifier not null, _exec_time datetime not null)
	CREATE UNIQUE CLUSTERED INDEX [IX_TREND_InactiveComputer_Previous] ON [dbo].[TREND_InactiveComputer_Previous] 
		(
			[Guid] ASC
	)
end

if ((select MAX(_exec_time) from TREND_InactiveComputer_Current where _exec_time >  dateadd(hour, -23, getdate())) is null) or (@force = 1)
begin
	-- STAGE 1: If we have current data, save it in the _previous table
	if (select count (*) from TREND_InactiveComputer_Current) > 0
		begin
			truncate table TREND_InactiveComputer_Previous
			insert TREND_InactiveComputer_Previous (guid, _exec_time)
			select * from TREND_InactiveComputer_Current

		-- STAGE 2: Insert current data in the current table
		truncate table TREND_InactiveComputer_Current
		insert TREND_InactiveComputer_Current (guid, _exec_time)
		select distinct(c.Guid) as 'Guid', getdate()
		  from RM_ResourceComputer c
		 INNER JOIN
			(
			select [ResourceGuid]
			  from dbo.ResourceUpdateSummary
			 where InventoryClassGuid = '9E6F402A-6A45-4CBA-9299-C2323F73A506' 		
			 group by [ResourceGuid]
			having max([ModifiedDate]) < GETDATE() - 7
			 ) as dt 
			ON c.Guid = dt.ResourceGuid	
		 where c.IsManaged = 1
		 
		 --STAGE 3: Extract the add/drop counts and insert data in the trending table
		 declare @added as int, @removed as int
		 -- Added in c
			 set @added = (
					select count(*)
					  from TREND_InactiveComputer_Current c
					  full join TREND_InactiveComputer_Previous p
						on p.guid = c.guid
					 where p.guid is null
			)
			    
			-- Removed in c
			 set @removed = (
					select count(*)
					  from TREND_InactiveComputer_Current c
					  full join TREND_InactiveComputer_Previous p
						on p.guid = c.guid
					 where c.guid is null
			)

		declare @managed as int, @inactive_1 as int, @inactive_2 as int
		set @managed = (select count(distinct(Guid)) from RM_ResourceComputer where IsManaged = 1)
		set @inactive_1 = (
			select count(distinct(c.Guid))
			  from RM_ResourceComputer c
			 INNER JOIN
				(
				select [ResourceGuid]
				  from dbo.ResourceUpdateSummary
				 where InventoryClassGuid = '9E6F402A-6A45-4CBA-9299-C2323F73A506' 		
				 group by [ResourceGuid]
				having max([ModifiedDate]) < GETDATE() - 7
				 ) as dt 
				ON c.Guid = dt.ResourceGuid	
			 where c.IsManaged = 1
		)
		set @inactive_2 = (
			select count(distinct(c.Guid))
			  from RM_ResourceComputer c
			 INNER JOIN
				(
				select [ResourceGuid]
				  from dbo.ResourceUpdateSummary
				 where InventoryClassGuid = '9E6F402A-6A45-4CBA-9299-C2323F73A506' 		
				 group by [ResourceGuid]
				having max([ModifiedDate]) < GETDATE() - 17
				 ) as dt 
				ON c.Guid = dt.ResourceGuid	
			 where c.IsManaged = 1
		)
		declare @execid as int
			set @execid = (select isnull(max(_exec_id), 0) from TREND_ActiveComputerCounts) + 1

		insert TREND_ActiveComputerCounts (_exec_id, timestamp, [Managed machines], [inactive computers (7 days)], [New Inactive Computers], [New Active Computers], [Inactive Computers (17 days)])
		values (@execid, getdate(), @managed, @inactive_1, @added, @removed, @inactive_2)
	end
	else
	begin
		truncate table TREND_InactiveComputer_Current
		insert TREND_InactiveComputer_Current (guid, _exec_time)
		select distinct(c.Guid) as 'Guid', getdate()
		  from RM_ResourceComputer c
		 INNER JOIN
			(
			select [ResourceGuid]
			  from dbo.ResourceUpdateSummary
			 where InventoryClassGuid = '9E6F402A-6A45-4CBA-9299-C2323F73A506' 		
			 group by [ResourceGuid]
			having max([ModifiedDate]) < GETDATE() - 7
			 ) as dt 
			ON c.Guid = dt.ResourceGuid	
		 where c.IsManaged = 1
	end
end

select * from TREND_ActiveComputerCounts order by _exec_id desc
