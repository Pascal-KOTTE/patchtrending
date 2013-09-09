using System;
using System.Collections.Generic;
using System.Text;

namespace Symantec.CWoC.PatchTrending {
    class FormattedStrings {
        public static string sql_get_bulletin_data = @"
                         select Convert(Datetime, max(_Exec_time), 101) as 'Date', SUM(installed) as 'Installed', SUM(Applicable) as 'Applicable'
                           from TREND_WindowsCompliance_ByUpdate
                          where Bulletin = '{0}'
                          group by _Exec_id order by date";

        public static string sql_get_update_data = @"
                         select Convert(Datetime, _Exec_time, 101) as 'Date', installed as 'Installed', Applicable as 'Applicable'
                           from TREND_WindowsCompliance_ByUpdate
                          where [update] = '{0}' and bulletin = '{1}' ";
    }
}