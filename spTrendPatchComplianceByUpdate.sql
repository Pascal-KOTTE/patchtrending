create procedure spTrendPatchComplianceByUpdate
	@collectionguid as uniqueidentifier = '01024956-1000-4cdb-b452-7db0cff541b6'
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
if (select MAX(_exec_time) from TREND_WindowsCompliance_ByUpdate) <  dateadd(hour, -23, getdate()) or (select COUNT(*) from TREND_WindowsCompliance_ByUpdate) = 0
begin

-- Get the compliance by update to a "temp" table
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
	set @id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByUpdate)
		insert into TREND_WindowsCompliance_ByUpdate
		select (ISNULL(@id + 1, 1)), GETDATE() as '_Exec_time', Bulletin, [UPDATE], Severity, [Installed (Count)] as 'Installed', [Applicable (Count)] as 'Applicable', _DistributionStatus as 'DistributionStatus'
		  from PM_TRENDS_TEMP
end

-- Return the latest results
select *, applicable - installed as 'Vulnerable',  cast(cast(installed as float) / cast(applicable as float) * 100 as money) as 'Compliance %'
  from TREND_WindowsCompliance_ByUpdate
 where _exec_id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByUpdate)
--   and cast(cast(installed as float) / cast(applicable as float) * 100 as money) < %ComplianceThreshold%
--   and applicable > %ApplicableThreshold%

union

select max(_exec_id), max(_exec_time), Bulletin, '-- ALL --' as [update], '' as severity, sum(installed) as 'Installed', sum(applicable) as 'Applicable', '' as DistributionStatus,  sum(applicable) - sum(installed) as 'Vulnerable',  cast(cast(sum(installed) as float) / cast(sum(applicable) as float) * 100 as money) as 'Compliance %'
  from TREND_WindowsCompliance_ByUpdate
 where _exec_id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByUpdate)
 group by Bulletin
--having sum(applicable) >%ApplicableThreshold%
--   and cast(cast(sum(installed) as float) / cast(sum(applicable) as float) * 100 as money) < %ComplianceThreshold%
 order by Bulletin,[update]
