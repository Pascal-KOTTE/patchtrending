exec sp_rename @objname='TREND_InactiveComputerCounts', @newname='TREND_InactiveComputerCounts_old'
exec spTrendInactiveComputers @CollectionGuid='6410074B-FFFF-FFFF-FFFF-0C8803328385'
insert TREND_InactiveComputerCounts ([_exec_id], [timestamp], [collectionguid], [Managed machines], [Inactive computers (7 days)], [New Inactive computers], [New Active computers], [Inactive computers (17 days)]) select [_exec_id], [timestamp], '311E8DAE-2294-4FF2-B9EF-B3D6A84183CB' as CollectionGuid, [Managed machines], [Inactive computers (7 days)], [New Inactive computers], [New Active computers], [Inactive computers (17 days)] from TREND_InactiveComputerCounts_old

exec sp_rename @objname='TREND_InactiveComputer_Current', @newname='TREND_InactiveComputer_Current_old'
exec spTrendInactiveComputers @CollectionGuid='6410074B-FFFF-FFFF-FFFF-0C8803328385'
insert TREND_InactiveComputer_current (guid, collectionguid, _exec_time) select guid, '311E8DAE-2294-4FF2-B9EF-B3D6A84183CB' as CollectionGuid, [_exectime] from TREND_InactiveComputer_Current_old

exec sp_rename @objname='TREND_InactiveComputer_Previous', @newname='TREND_InactiveComputer_Previous_old'
exec spTrendInactiveComputers @CollectionGuid='6410074B-FFFF-FFFF-FFFF-0C8803328385'
insert TREND_InactiveComputer_previous (guid, collectionguid, _exec_time) select guid, '311E8DAE-2294-4FF2-B9EF-B3D6A84183CB' as CollectionGuid, [_exectime] from TREND_InactiveComputer_Previous_old

exec sp_rename @objname='TREND_WindowsCompliance_ByComputer', @newname='TREND_WindowsCompliance_ByComputer_old'
exec spTrendPatchComplianceByComputer @CollectionGuid='6410074B-FFFF-FFFF-FFFF-0C8803328385'
insert TREND_WindowsCompliance_ByComputer ([_Exec_id], [CollectionGuid], [_Exec_time], [Percent], [Computer #], [% of Total]) select [_Exec_id], '311E8DAE-2294-4FF2-B9EF-B3D6A84183CB' as 'CollectionGuid', [_Exec_time], [Percent], [Computer #], [% of Total] from TREND_WindowsCompliance_ByComputer_old

exec sp_rename @objname='TREND_WindowsCompliance_ByUpdate', @newname='TREND_WindowsCompliance_ByUpdate_old'
exec spTrendPatchComplianceByUpdate @CollectionGuid = '6410074B-FFFF-FFFF-FFFF-0C8803328385'
insert TREND_WindowsCompliance_ByUpdate ([_Exec_id], [CollectionGuid], [_Exec_time], [Bulletin], [UPDATE], [Severity], [Installed], [Applicable], [DistributionStatus]) select [_Exec_id], '311E8DAE-2294-4FF2-B9EF-B3D6A84183CB' as 'CollectionGuid', [_Exec_time], [Bulletin], [UPDATE], [Severity], [Installed], [Applicable], [DistributionStatus] from TREND_WindowsCompliance_ByUpdate_old
