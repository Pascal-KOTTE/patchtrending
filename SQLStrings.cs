using System;
using System.Collections.Generic;
using System.Text;

namespace Symantec.CWoC.PatchTrending {
    public static class SQLStrings {
        #region string sql_spTrendInactiveComputers = @"...
        public static string sql_drop_spTrendInactiveComputers = @"
if exists(select 1 from sys.objects where name = 'spTrendInactiveComputers' and type = 'P')
begin
    drop procedure spTrendInactiveComputers
end
";

        public static string sql_spTrendInactiveComputers = @"
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

if ((select MAX(_exec_time) from TREND_InactiveComputer_Current where _exec_time >  dateadd(hour, -23, getdate())) is null) or (@force = 1)
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

";
        #endregion

        #region string sql_spTrendPatchComplianceByComputer = @"...
        public static string sql_drop_spTrendPatchComplianceByComputer = @"
if exists(select 1 from sys.objects where name = 'spTrendPatchComplianceByComputer' and type = 'P')
begin
    drop procedure spTrendPatchComplianceByComputer
end
";

        public static string sql_spTrendPatchComplianceByComputer = @"
create procedure spTrendPatchComplianceByComputer
	@collectionguid as uniqueidentifier = '01024956-1000-4cdb-b452-7db0cff541b6',
	@force as int = 0
as

if (not exists(select 1 from sys.objects where name = 'PM_TRENDS2_TEMP' and type = 'U'))
begin
CREATE TABLE [dbo].[PM_TRENDS2_TEMP](
	[_ResourceGuid] [uniqueidentifier] NOT NULL,
	[Computer Name] [varchar](250) NOT NULL,
	[Compliance] [numeric](6, 2) NULL,
	[Applicable (Count)] [int] NULL,
	[Installed (Count)] [int] NULL,
	[Not Installed (Count)] [int] NULL,
	[Restart Pending] [varchar](3) NOT NULL,
	[_DistributionStatus] [nvarchar](16) NULL,
	[_OperatingSystem] [nvarchar](128) NULL,
	[_StartDate] [datetime] NULL,
	[_EndDate] [datetime] NULL,
) ON [PRIMARY]
end

if (not exists(select 1 from sys.objects where type = 'U' and name = 'TREND_WindowsCompliance_ByComputer'))
begin
	CREATE TABLE [dbo].[TREND_WindowsCompliance_ByComputer](
		[_Exec_id] [int] NOT NULL,
		[CollectionGuid] [uniqueidentifier] NOT NULL,
		[_Exec_time] [datetime] NOT NULL,
		[Percent] int NOT NULL,
		[Computer #] int NOT NULL,
		[% of Total] money NOT NULL,
	) ON [PRIMARY]

	CREATE UNIQUE CLUSTERED INDEX [IX_TREND_WindowsCompliance_ByComputer] ON [dbo].[TREND_WindowsCompliance_ByComputer] 
	(
		[CollectionGuid] ASC,
		[Percent] ASC,
		[_exec_id] ASC
	)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = 
OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

end

-- PART II: Get data into the trending table if no data was captured in the last 23 hours
if (select MAX(_exec_time) from TREND_WindowsCompliance_ByComputer where collectionguid = @collectionguid) <  dateadd(hour, -23, getdate()) or ((select COUNT(*) from TREND_WindowsCompliance_ByComputer where collectionguid = @collectionguid) = 0) or (@force = 1)
begin

-- Get the compliance by update to a 'temp' table
truncate table PM_TRENDS2_TEMP
insert into PM_TRENDS2_TEMP
exec spPMWindows_ComplianceByComputer
							@OperatingSystem = '%',
							@DistributionStatus = 'active',
							@FilterCollection = @collectionguid,
							@StartDate = '1990-08-21T00:00:00',
							@EndDate = '2020-12-31',
							@pCulture = 'en-gb',
							@TrusteeScope = '{2e1f478a-4986-4223-9d1e-b5920a63ab41}',
							@VendorGuid	= '00000000-0000-0000-0000-000000000000',
							@CategoryGuid = '-0000-0000-0000-000000000000'

declare @id as int
	set @id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByComputer where collectionguid = @collectionguid)

declare @total as float
	set @total = (select COUNT(*) from PM_TRENDS2_TEMP)

insert into TREND_WindowsCompliance_ByComputer
select (ISNULL(@id + 1, 1)), @CollectionGuid as CollectionGuid, GETDATE() as '_Exec_time', CAST(compliance as decimal) as 'Percentile', COUNT(*) as 'Computer #', cast((CAST(count(*) as float) / @total) * 100 as money) as '% of Total'
  from PM_TRENDS2_TEMP
 group by CAST(compliance as decimal)
 order by CAST(compliance as decimal)

end

 select *
   from TREND_WindowsCompliance_ByComputer
  where _exec_id = (select max(_exec_id) from TREND_WindowsCompliance_ByComputer where collectionguid = @collectionguid)
    and collectionguid = @collectionguid
";
#endregion

        #region string sql_spTrendPatchComplianceByUpdate = @"...
        public static string sql_drop_spTrendPatchComplianceByUpdate = @"
if exists(select 1 from sys.objects where name = 'spTrendPatchComplianceByUpdate' and type = 'P')
begin
    drop procedure spTrendPatchComplianceByUpdate
end
";
        public static string sql_spTrendPatchComplianceByUpdate = @"
create procedure spTrendPatchComplianceByUpdate

	@collectionguid as uniqueidentifier = '01024956-1000-4cdb-b452-7db0cff541b6',
	@force as int = 0
as
-- #########################################################################################################
-- PART I: Make sure underlying infrastructure exists and is ready to use
if (exists(select 1 from sys.objects where name = 'PM_TRENDS_TEMP' and type = 'U'))
begin
	truncate table PM_TRENDS_TEMP
end
else
begin
CREATE TABLE [dbo].[PM_TRENDS_TEMP](
	[_SWUGuid] [uniqueidentifier] NOT NULL,
	[Bulletin] [varchar](250) NOT NULL,
	[Update] [varchar](250) NOT NULL,
	[Severity] [varchar](250) NOT NULL,
	[Custom Severity] [nvarchar](100) NULL,
	[Release Date] [datetime] NOT NULL,
	[Compliance] [numeric](6, 2) NULL,
	[Applicable (Count)] [int] NULL,
	[Installed (Count)] [int] NULL,
	[Not Installed (Count)] [int] NULL,
	[_SWBGuid] [uniqueidentifier] NOT NULL,
	[_ScopeCollection] [uniqueidentifier] NULL,
	[_Collection] [uniqueidentifier] NULL,
	[_StartDate] [datetime] NULL,
	[_EndDate] [datetime] NULL,
	[_DistributionStatus] [nvarchar](16) NULL,
	[_OperatingSystem] [nvarchar](128) NULL,
	[_VendorGuid] [uniqueidentifier] NULL,
	[_CategoryGuid] [uniqueidentifier] NULL
) ON [PRIMARY]
end

if (not exists(select 1 from sys.objects where type = 'U' and name = 'TREND_WindowsCompliance_ByUpdate'))
begin
	CREATE TABLE [dbo].[TREND_WindowsCompliance_ByUpdate](
		[_Exec_id] [int] NOT NULL,
		[CollectionGuid] [uniqueidentifier] NOT NULL,
		[_Exec_time] [datetime] NOT NULL,
		[Bulletin] [varchar](250) NOT NULL,
		[UPDATE] [varchar](250) NOT NULL,
		[Severity] [varchar](250) NOT NULL,
		[Installed] [int] NULL,
		[Applicable] [int] NULL,
		[DistributionStatus] [nvarchar](16) NULL
	) ON [PRIMARY]

	CREATE UNIQUE CLUSTERED INDEX [IX_TREND_WindowsCompliance_ByUpdate] ON [dbo].[TREND_WindowsCompliance_ByUpdate] 
	(
		[CollectionGuid] asc,
		[Bulletin] ASC,
		[Update] ASC,
		[_exec_id] ASC
	)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = 
OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

	CREATE NONCLUSTERED INDEX [IX_TREND_WindowsCompliance_ByUpdate_OrderbyUpdate] ON [dbo].[TREND_WindowsCompliance_ByUpdate] 
	(
		[UPDATE] ASC
	)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

end

-- PART II: Get data into the trending table if no data was captured in the last 24 hours
if (select MAX(_exec_time) from TREND_WindowsCompliance_ByUpdate where CollectionGuid = @CollectionGuid) <  dateadd(hour, -23, getdate()) or ((select COUNT(*) from TREND_WindowsCompliance_ByUpdate where CollectionGuid = @CollectionGuid) = 0) or (@force = 1)
begin

-- Get the compliance by update to a 'temp' table
insert into PM_TRENDS_TEMP
  exec spPMWindows_ComplianceByUpdate
			@OperatingSystem = '%',
			@DistributionStatus = 'Active',
			@FilterCollection = @collectionguid,
			@StartDate = '1900-06-29T00:00:00',
			@EndDate = '2020-06-29T00:00:00',
			@pCulture = 'en-GB',
			@ScopeCollectionGuid = '91c68fcb-1822-e793-b59c-2684e99a64cd',
			@TrusteeScope = '{2e1f478a-4986-4223-9d1e-b5920a63ab41}',
			@VendorGuid = '00000000-0000-0000-0000-000000000000',
			@CategoryGuid = '00000000-0000-0000-0000-000000000000',
			@DisplayMode = 'all' 

declare @id as int
	set @id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByUpdate where CollectionGuid = @CollectionGuid)
		insert into TREND_WindowsCompliance_ByUpdate
		select (ISNULL(@id + 1, 1)), @collectionguid, GETDATE() as '_Exec_time', Bulletin, [UPDATE], Severity, [Installed (Count)] as 'Installed', [Applicable (Count)] as 'Applicable', _DistributionStatus as 'DistributionStatus'
		  from PM_TRENDS_TEMP
end

-- Return the latest results
select *, applicable - installed as 'Vulnerable',  cast(cast(installed as float) / cast(applicable as float) * 100 as money) as 'Compliance %'
  from TREND_WindowsCompliance_ByUpdate
 where _exec_id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByUpdate where CollectionGuid = @CollectionGuid)
   and CollectionGuid = @CollectionGuid
--   and cast(cast(installed as float) / cast(applicable as float) * 100 as money) < %ComplianceThreshold%
--   and applicable > %ApplicableThreshold%

union

select max(_exec_id), @CollectionGuid, max(_exec_time), Bulletin, '-- ALL --' as [update], '' as severity, sum(installed) as 'Installed', sum(applicable) as 'Applicable', '' as DistributionStatus,  sum(applicable) - sum(installed) as 'Vulnerable',  cast(cast(sum(installed) as float) / cast(sum(applicable) as float) * 100 as money) as 'Compliance %'
  from TREND_WindowsCompliance_ByUpdate
 where _exec_id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByUpdate where CollectionGuid = @CollectionGuid)
   and CollectionGuid = @CollectionGuid
 group by Bulletin
--having sum(applicable) >%ApplicableThreshold%
--   and cast(cast(sum(installed) as float) / cast(sum(applicable) as float) * 100 as money) < %ComplianceThreshold%
 order by Bulletin,[update]
";
#endregion

        #region SQL query strings
        public static string sql_get_bulletin_data = @"
                         select Convert(varchar(255), max(_Exec_time), 127) as 'Date', SUM(installed) as 'Installed', SUM(Applicable) as 'Applicable'
                           from TREND_WindowsCompliance_ByUpdate
                          where Bulletin = '{0}' and CollectionGuid = '{1}'
                          group by _Exec_id order by date
		";
        public static string sql_get_update_data = @"
                         select Convert(varchar(255), _Exec_time, 127) as 'Date', installed as 'Installed', Applicable as 'Applicable'
                           from TREND_WindowsCompliance_ByUpdate
                          where [update] = '{0}' and bulletin = '{1}' and CollectionGuid = '{2}'
		";
        public static string sql_get_bulletins_in = @"
               -- Get all tracked bulletins
                select bulletin
                  from TREND_WindowsCompliance_ByUpdate
                 where bulletin in ({0})
				   and collectionguid = '{1}'
                 group by bulletin
                having MAX(_exec_id) = (select MAX(_exec_id) from TREND_WindowsCompliance_ByUpdate)
                 order by MIN(_exec_time) desc, Bulletin desc
		";
        public static string sql_get_all_bulletins = @"
               -- Get all tracked bulletins
                select bulletin
                  from TREND_WindowsCompliance_ByUpdate
				 where collectionguid = '{0}'
                 group by bulletin
                having MAX(_exec_id) = (select MAX(_exec_id) from TREND_WindowsCompliance_ByUpdate)
                 order by MIN(_exec_time) desc, Bulletin desc
		";
        public static string sql_get_global_compliance_data = @"
                         select Convert(varchar, max(_Exec_time), 127) as 'Date', SUM(installed) as 'Installed', SUM(Applicable) as 'Applicable'
                           from TREND_WindowsCompliance_ByUpdate
						  where collectionguid = '{0}'
                          group by _Exec_id order by date
		";
        public static string sql_get_topn_vulnerable = @"
                select top 10 Bulletin --, SUM(Applicable) - SUM(installed)
                  from TREND_WindowsCompliance_ByUpdate
                 where _Exec_id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByUpdate where collectionguid = '{0}')
				   and collectionguid = '{0}'
                 group by Bulletin
                 order by SUM(Applicable) - SUM(installed) desc
		";
        public static string sql_get_topn_vulnerable_upd = @"
                select top 25 [Update]
                  from TREND_WindowsCompliance_ByUpdate
                 where [_Exec_id] = (select MAX(_exec_id) from TREND_WindowsCompliance_ByUpdate where collectionguid = '{0}')
				   and collectionguid = '{0}'
                 group by [Update], [Bulletin]
                 order by SUM(Applicable) - SUM(installed) desc
		";
        public static string sql_get_bottomn_compliance = @"
                select top 10 Bulletin --, CAST(SUM(installed) as float) / CAST(SUM(Applicable) as float) * 100
                  from TREND_WindowsCompliance_ByUpdate
                 where _Exec_id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByUpdate where collectionguid = '{0}')
				   and collectionguid = '{0}'
                 group by Bulletin
                having SUM(Applicable) - SUM(installed) > 100
                 order by CAST(SUM(installed) as float) / CAST(SUM(Applicable) as float) * 100
		";
        public static string sql_get_bottomn_compliance_upd = @"
                select top 25 [Update] --, CAST(SUM(installed) as float) / CAST(SUM(Applicable) as float) * 100
                  from TREND_WindowsCompliance_ByUpdate
                 where _Exec_id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByUpdate where collectionguid = '{0}')
				   and collectionguid = '{0}'
                 group by Bulletin, [Update]
                having SUM(Applicable) - SUM(installed) > 100
                 order by CAST(SUM(installed) as float) / CAST(SUM(Applicable) as float) * 100
		";
        public static string sql_get_topn_movers_up = @"
                -- Return the 10 bulletins for which more computers are secured
                select top 10 t1.Bulletin, t1._Exec_id, (sum(t2.Applicable) - SUM(t2.installed)) - (sum(t1.Applicable) - SUM(t1.installed)) as 'Delta'
                  from TREND_WindowsCompliance_ByUpdate t1
                  join TREND_WindowsCompliance_ByUpdate t2
                    on t1._Exec_id -1 = t2._Exec_id and t1.Bulletin = t2.Bulletin and t1.[UPDATE] = t2.[update] and t1.collectionguid = t2.collectionguid
                 where t1._Exec_id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByUpdate where collectionguid = '{0}')
                   and t1.collectionguid = '{0}'
                 group by t1.Bulletin, t1._Exec_id
                having (sum(t2.Applicable) - SUM(t2.installed)) - (sum(t1.Applicable) - SUM(t1.installed)) > 0
                 order by (sum(t2.Applicable) - SUM(t2.installed)) - (sum(t1.Applicable) - SUM(t1.installed)) desc
        ";
        public static string sql_get_topn_movers_down = @"
                -- Return the 10 bulletins for which more computers are vulnerable
                select top 10 t1.Bulletin, t1._Exec_id, (sum(t2.Applicable) - SUM(t2.installed)) - (sum(t1.Applicable) - SUM(t1.installed)) as 'Delta'
                  from TREND_WindowsCompliance_ByUpdate t1
                  join TREND_WindowsCompliance_ByUpdate t2
                    on t1._Exec_id -1 = t2._Exec_id
                   and t1.Bulletin = t2.Bulletin 
                   and t1.[UPDATE] = t2.[update] 
                   and t1.collectionguid = t2.collectionguid
                 where t1._Exec_id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByUpdate where collectionguid = '{0}')
				   and t1.collectionguid = '{0}'
                 group by t1.Bulletin, t1._Exec_id
                having (sum(t2.Applicable) - SUM(t2.installed)) - (sum(t1.Applicable) - SUM(t1.installed)) < 0
                 order by (sum(t2.Applicable) - SUM(t2.installed)) - (sum(t1.Applicable) - SUM(t1.installed))
        ";
        public static string sql_get_updates_bybulletin = @"
                 select distinct([UPDATE])
                   from TREND_WindowsCompliance_ByUpdate
                  where bulletin = '{0}'
				    and collectionguid = '{1}'
				  group by [update]
				 having MAX(_exec_id) = (select MAX(_exec_id) from TREND_WindowsCompliance_ByUpdate where CollectionGuid = '{1}')
        ";
		public static string sql_get_compliance_bypccount = @"
declare @id as int
	set @id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByComputer where CollectionGuid = '{0}')

if (@id > 1)
begin
	select t1.[Percent], t3.[min], t2.[Computer #], t1.[Computer #], t3.[max], t1.[% of Total]

--	, t1.[% of Total], t2.[% of Total]
	  from TREND_WindowsCompliance_ByComputer t1
	  join TREND_WindowsCompliance_ByComputer t2
		on t1.[Percent] = t2.[Percent]
	  join (
				select[Percent], MIN(t3.[Computer #]) as min, MAX(t3.[computer #]) as max
				  from TREND_WindowsCompliance_ByComputer t3
				 where CollectionGuid = '{0}'
				 group by [Percent]
			) t3
	    on t1.[Percent] = t3.[percent]
	 where t1._Exec_id = @id
	   and t2._Exec_id = @id - 1
	   and t1.CollectionGuid = '{0}'
--	   and t1.[Percent] > 74
end
";

        public static string sql_get_compliance_bypcpercent = @"declare @id as int
	set @id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByComputer where CollectionGuid = '{0}')

if (@id > 1)
begin
	select t1.[Percent], t3.[min], t2.[% of Total], t1.[% of Total], t3.[max], t1.[% of Total]
--	, t1.[% of Total], t2.[% of Total]
	  from TREND_WindowsCompliance_ByComputer t1
	  join TREND_WindowsCompliance_ByComputer t2
		on t1.[Percent] = t2.[Percent]
	  join (
				select[Percent], MIN(t3.[% of Total]) as min, MAX(t3.[% of Total]) as max
				  from TREND_WindowsCompliance_ByComputer t3
				 where CollectionGuid = '{0}'
				 group by [Percent]
			) t3
	    on t1.[Percent] = t3.[percent]
	 where t1._Exec_id = @id
	   and t2._Exec_id = @id - 1
	   and t1.CollectionGuid = '{0}'
--	   and t1.[Percent] > 74
end
";
        public static string sql_get_inactive_computer_trend = @"
select Convert(varchar, timestamp, 127), [Inactive computers (7 days)], [Inactive computers (17 days)], [New inactive computers], [New Active Computers]
  from TREND_InactiveComputerCounts
 where CollectionGuid = '{0}'
 order by _exec_id
";
        public static string sql_get_inactive_computer_percent = @"
select Convert(varchar, timestamp, 127), cast([Inactive computers (7 days)] as money) /  cast([Managed machines] as money) * 100 as '7-days inactive (% of managed)', cast([Inactive computers (17 days)] as money) /  cast([Managed machines] as money) * 100 as '17-days inactive (% of managed)', CAST([New inactive computers] as money) / CAST([Managed machines] AS money) * 100 as '++ (% of managed)', CAST([New active computers] as money) / CAST([Managed machines] as money) * 100 as '-- (% of managed)'
  from TREND_InactiveComputerCounts
 where CollectionGuid = '{0}'
 order by _exec_id
     ";
	 
		public static string sql_exec_spTrendInactiveComputers = @"
if exists (select 1 from sys.objects where type = 'p' and name = 'spTrendInactiveComputers')
exec spTrendInactiveComputers @CollectionGuid = '{0}'
		";
		public static string sql_exec_spTrendPatchComplianceByComputer = @"
if exists (select 1 from sys.objects where type = 'p' and name = 'spTrendPatchComplianceByComputer')
exec spTrendPatchComplianceByComputer @CollectionGuid = '{0}'
		";
		public static string sql_exec_spTrendPatchComplianceByUpdate = @"
if exists (select 1 from sys.objects where type = 'p' and name = 'spTrendPatchComplianceByUpdate')
exec spTrendPatchComplianceByUpdate @CollectionGuid = '{0}'
		";

        #endregion

    }
}
