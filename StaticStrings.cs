using System;
using System.Collections.Generic;
using System.Text;

namespace Symantec.CWoC.PatchTrending {
    class StaticStrings {
        #region // public static string GlobalComplianceHtml
        public static string LandingHtml = @"<html>
	<head>
        <style type=""text/css"">
        ul { width: 60em; }
        ul li {  float: left; width: 20em;  }
        br { clear: left; }
        div.wrapper {  margin-bottom: 1em;}
		body { font-family: Arial;};
        </style>
		<script type=""text/javascript"" src=""https://www.google.com/jsapi""></script>
        <script src=""https://ajax.googleapis.com/ajax/libs/prototype/1.7.0.0/prototype.js""></script>
		<script type=""text/javascript"" src=""javascript/global_0.js""></script>
		<script type=""text/javascript"" src=""javascript/global_1.js""></script>
        <script type=""text/javascript"" src=""javascript/pccompl.js""></script>
		<script type=""text/javascript"" src=""javascript/global.js""></script>
		<script type=""text/javascript"">
			google.load(""visualization"", ""1"", {packages:[""corechart""]});
			google.setOnLoadCallback(drawChart);

            function loadBulletin() {
				
				var bulletin = document.getElementById(""bulletin_name"").value;
				var jsUrl = ""javascript/"" + escapeBulletin(bulletin) + ""_0.js"";
				
				new Ajax.Request(jsUrl, {
                    method:'get',
					onSuccess: function(response) {
    					// Handle the response content...
						if (response.responseText.length > 0) {
							results = eval(response.responseText);
							window.location = ""getbulletin.html?"" + bulletin;
						}
					}, 
					onFailure: function() {
						alert(""Could not find any data to load for bulletin "" + document.getElementById(""bulletin_name"").value);
					}
				});	
			}
		
			function escapeBulletin(b) {
				var t = b.replace(""-"", ""_"");
                t = t.replace(""."", ""_"");
				return t.toUpperCase();
            }
        </script>
	</head>
    <body style=""width:1000"">
    <h2 style=""text-align: center; width:80%"">Global Compliance view</h2>
    <hr/>
    <table style=""width: 80%"">
        <tr>
            <td style=""text-align: center""><b>Installed versus Applicable</b></td> 
            <td style=""text-align: center""><b>Compliance status in %</b></td>
        </tr>
        <tr>
            <td><div id='global_div_1' style='width: 500px; height: 300px;'></div></td>
            <td><div id='global_div_0' style='width: 500px; height: 300px;'></div></td>
        </tr>
    </table>";

        public static string PcComplHtml = @"
    <hr/>
    <p style=""text-align: center;""><b>Compliance by computer - upper quarter</b></p>
    <div id='pccompl_div' style='width: 1000px; height: 300px;'></div>";

        public static string BulletinSearch = @"
    <hr/>
	Bulletin name: <input type=""text"" id=""bulletin_name""></input><input type=""button"" value=""View graphs"" onclick=""loadBulletin()""/>
    <hr/>";

        #endregion

        #region // public static string GlobalComplianceJavascript
        public static string GlobalComplianceJavascript = @"
        function drawChart() {
	    var options1 = { title: '', vAxis: { maxValue : 100, minValue : 0 }};
	    var options2 = { title: '', vAxis: { minValue : 0 }};

		var d_global_0 = google.visualization.arrayToDataTable(global_0);
		var g_global_0 = new google.visualization.LineChart(document.getElementById('global_div_0'));
		g_global_0.draw(d_global_0, options1);

		var d_global_1 = google.visualization.arrayToDataTable(global_1);
		var g_global_1 = new google.visualization.LineChart(document.getElementById('global_div_1'));
		g_global_1.draw(d_global_1, options2);

		var d_pccompl = new google.visualization.DataTable();
        d_pccompl.addColumn('string', 'Compliance in %');
		d_pccompl.addColumn('number');
		d_pccompl.addColumn('number');
		d_pccompl.addColumn('number');
		d_pccompl.addColumn('number');
		d_pccompl.addColumn({type:'string', role:'tooltip', 'p': {'html': true}});
		d_pccompl.addRows(pccompl);
		var g_pccompl = new google.visualization.CandlestickChart(document.getElementById('pccompl_div'));
        g_pccompl.draw(d_pccompl, { legend:'none', tooltip: {isHtml: true}} );

        }";
        #endregion

        #region // SQL query strings
        public static string sqlGetBulletinsIn = @"
               -- Get all tracked bulletins
                select bulletin
                  from TREND_WindowsCompliance_ByUpdate
                 where bulletin in ({0})
                 group by bulletin
                 order by MIN(_exec_time) desc, Bulletin desc";
        public static string sqlGetAllBulletins = @"
               -- Get all tracked bulletins
                select bulletin
                  from TREND_WindowsCompliance_ByUpdate
                 group by bulletin
                 order by MIN(_exec_time) desc, Bulletin desc";
        public static string sqlGetTop10Vulnerable = @"
                select top 10 Bulletin --, SUM(Applicable) - SUM(installed)
                  from TREND_WindowsCompliance_ByUpdate
                 where _Exec_id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByUpdate)
                 group by Bulletin
                 order by SUM(Applicable) - SUM(installed) desc";
        public static string sqlGetBottom10Compliance = @"
                select top 10 Bulletin --, CAST(SUM(installed) as float) / CAST(SUM(Applicable) as float) * 100
                  from TREND_WindowsCompliance_ByUpdate
                 where _Exec_id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByUpdate)
                 group by Bulletin
                having SUM(Applicable) - SUM(installed) > 100
                 order by CAST(SUM(installed) as float) / CAST(SUM(Applicable) as float) * 100
                ";
        public static string sqlGetUpdatesByBulletin = @"
                 select distinct([UPDATE])
                   from TREND_WindowsCompliance_ByUpdate
                  where bulletin = '{0}'
                 ";

        public static string sql_compliancebypc_count = @"
declare @id as int
	set @id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByComputer)

if (@id > 1)
begin
	select t1.[Percent], t3.[min], t2.[Computer #], t1.[Computer #], t3.[max], t2.[% of Total]

--	, t1.[% of Total], t2.[% of Total]
	  from TREND_WindowsCompliance_ByComputer t1
	  join TREND_WindowsCompliance_ByComputer t2
		on t1.[Percent] = t2.[Percent]
	  join (
				select[Percent], MIN(t3.[Computer #]) as min, MAX(t3.[computer #]) as max
				  from TREND_WindowsCompliance_ByComputer t3
				group by [Percent]
			) t3
	    on t1.[Percent] = t3.[percent]	    
	 where t1._Exec_id = @id
	   and t2._Exec_id = @id - 1
--	   and t1.[Percent] > 74
end
";

        public static string sql_compliancebypc_percent = @"declare @id as int
	set @id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByComputer)

if (@id > 1)
begin
	select t1.[Percent], t3.[min], t2.[% of Total], t1.[% of Total], t3.[max], t2.[% of Total]

--	, t1.[% of Total], t2.[% of Total]
	  from TREND_WindowsCompliance_ByComputer t1
	  join TREND_WindowsCompliance_ByComputer t2
		on t1.[Percent] = t2.[Percent]
	  join (
				select[Percent], MIN(t3.[% of Total]) as min, MAX(t3.[% of Total]) as max
				  from TREND_WindowsCompliance_ByComputer t3
				group by [Percent]
			) t3
	    on t1.[Percent] = t3.[percent]	    
	 where t1._Exec_id = @id
	   and t2._Exec_id = @id - 1
--	   and t1.[Percent] > 74
end
";
        public static string sql_compliancebypc_bottom75percent = @"
/* BOTTOM 75% SUMMARY */
declare @id as int
	set @id = (select MAX(_exec_id) from TREND_WindowsCompliance_ByComputer)


select SUM([computer #]), SUM([% of total])
  from TREND_WindowsCompliance_ByComputer t3
 where t3._Exec_id = @id
   and t3.[Percent] < 75
 group by [_exec_id]
";


#endregion

        #region // string getbulletinhtml
        public static string GetBulletinHtml = @" <html>
	<head>
		<script type=""text/javascript"" src=""https://www.google.com/jsapi""></script>
	<script type=""text/javascript"">
		var bulletin = window.location.search.substring(1).toUpperCase();

		function loadjs(filename){
			var fileref = document.createElement('script')
			fileref.setAttribute(""type"",""text/javascript"")
			fileref.setAttribute(""src"", filename)
			
			if (typeof fileref!=""undefined"")
				document.getElementsByTagName(""head"")[0].appendChild(fileref)
		}
		
		function escapeBulletin(b) {
			var t = b.replace(""-"", ""_"");
			return t.replace(""."", ""_"");
		}
		 
		function drawChart() {
			var options1 = { title: '', vAxis: { maxValue : 100, minValue : 0 }};
			var options2 = { title: '', vAxis: { minValue : 0 }};

			b = escapeBulletin(bulletin);

			var d_0 = google.visualization.arrayToDataTable(window[b + ""_0""]);
			var g_0 = new google.visualization.LineChart(document.getElementById('div_0'));
			g_0.draw(d_0, options1);

 			var d_1 = google.visualization.arrayToDataTable(window[b + ""_1""]);
			var g_1 = new google.visualization.LineChart(document.getElementById('div_1'));
			g_1.draw(d_1, options2);
		}

		loadjs(""javascript/"" + escapeBulletin(bulletin) + ""_0.js"");
		loadjs(""javascript/"" + escapeBulletin(bulletin) + ""_1.js"");

		google.load(""visualization"", ""1"", {packages:[""corechart""]});
		google.setOnLoadCallback(drawChart);

	</script>
	</head>
    <body>
    <h1 id=""t_012"" style=""width: 800px; text-align: center""></h1>
	<h2>Installed versus Applicable</h2>
	<div id='div_1' style='width: 800px; height: 300px;'></div>
	<h2>Compliance status in %</h2>
	<div id='div_0' style='width: 800px; height: 300px;'></div>
    <script type=""text/javascript"">
	    var head_link = ""<a href=\"""" + bulletin + "".html\"">"" + bulletin + ""</a>"";
	    var t = document.getElementById(""t_012"").innerHTML = head_link;
    </script>
</body>
</html>";
        #endregion

    }
}
