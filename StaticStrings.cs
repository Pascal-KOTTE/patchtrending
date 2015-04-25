using System;
using System.Collections.Generic;
using System.Text;

namespace Symantec.CWoC.PatchTrending {
    class StaticStrings {
        #region Navigation bar (html)
        public static string html_navigationbar = @"
    <a class='menu' onclick='showhide(""_menu"")'><b>[Navigation]</b></a>
    <div id='_menu' class='hide'> 
        <a href='./' class='submenu'>Home</a>
        <a href='./sitemap.html' class='submenu'>Sitemap</a>
        <a href='./help.html' class='submenu'>Help</a>
    </div>
";
        public static string html_navigationbar_sitemap = @"
    <a class='menu' onclick='showhide(""_menu"")'><b>[Navigation]</b></a>
    <div id='_menu' class='hide'> 
        <a href='./' class='submenu'>Home</a>
        <a href='./help.html' class='submenu'>Help</a>
    </div>
";

        #endregion

        #region Navigation bar (css)
        public static string css_navigation = @"        body { font-family: Arial;}
        #_menu {
	        position: absolute;
	        top: 20px;
	        padding-bottom: 2px;
	        padding-top: 2px;
	        left:900px;
	        text-align: left;
        }
        .menu{
	        padding-top:0px;
	        padding-bottom: 2px;
	        color: #000000;
	        height: 20px;
	        left: 900px;
            top: 5px;
	        position: absolute;
	        font-size: 10px;
        }
        .submenu{
	        display: block;
	        height: 19px;
	        padding-top: 2px;
	        padding-right: 2px;
	        color: #333333;
        }

        .hide{
        display: none;
        }
        .show{
        display: block;
        }
";
        #endregion

        #region Landing (html, javascript)
        public static string html_Landing = @"<html>
	<head>
        <title>{CWoC} Patch Trending home</title>
        <style type='text/css'>
        ul { width: 60em; }
        ul li {  float: left; width: 20em;  }
        br { clear: left; }
        div.wrapper {  margin-bottom: 1em;}
		body { font-family: Arial;};
        </style>
        <link rel='stylesheet' type='text/css' href='menu.css'>
	</head>
    <body style='width:1000'>
    <a class='menu' onclick='showhide(""_menu"")'><b>[Navigation]</b></a>
    <div id='_menu' class='hide'> 
        <a href='./sitemap.html' class='submenu'>Sitemap</a>
        <a href='./help.html' class='submenu'>Help</a>
    </div>
    <h2 style='text-align: center;'>Global Compliance view</h2>
    <hr/>
    <table style='width: 80%'>
        <tr>
            <td style='text-align: center'><b>Installed versus Applicable</b></td> 
            <td style='text-align: center'><b>Compliance status in %</b></td>
        </tr>
        <tr>
            <td><div id='global_div_1' style='width: 500px; height: 300px;'></div></td>
            <td><div id='global_div_0' style='width: 500px; height: 300px;'></div></td>
        </tr>
    </table>";

        public static string LandingJs = @"
	<script type='text/javascript' src='https://www.google.com/jsapi'></script>
    <script src='https://ajax.googleapis.com/ajax/libs/prototype/1.7.0.0/prototype.js'></script>
	<script type='text/javascript' src='javascript/helper.js'></script>
	<script type='text/javascript' src='javascript/global_0.js'></script>
	<script type='text/javascript' src='javascript/global_1.js'></script>
    <script type='text/javascript' src='javascript/pccompl.js'></script>
    <script type='text/javascript' src='javascript/pccompl_full.js'></script>
    <script type='text/javascript' src='javascript/inactive_computers.js'></script>
    <script type='text/javascript' src='javascript/inactive_computers_pc.js'></script>
	<script type='text/javascript' src='javascript/global.js'></script>
	<script type='text/javascript'>
		google.load('visualization', '1', {packages:['corechart']});
		google.setOnLoadCallback(drawChart);

        function loadBulletin() {

			var bulletin = document.getElementById('bulletin_name').value;
			var jsUrl = 'javascript/' + escapeString(bulletin.toLowerCase()) + '_0.js';

			new Ajax.Request(jsUrl, {
                method:'get',
				onSuccess: function(response) {
					// Handle the response content...
					if (response.responseText.length > 0) {
						results = eval(response.responseText);
						window.location = 'getbulletin.html?' + bulletin;
					}
				}, 
				onFailure: function() {
					alert('Could not find any data to load for bulletin ' + document.getElementById('bulletin_name').value);
				}

			});	
		}
    </script>
    <script type='text/javascript'>

        function numberWithCommas(x) {
            return x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ',');
        }

		var msg_a = analyse_compliance();
		var msg_b = analyse_vulnerability();
		var msg_c = analyse_pccompl();

		var box = document.getElementById('daily_summary');
		box.innerHTML += '<hr/><h3>Daily summary</h3>';
		box.innerHTML += '<p>' + msg_a + '</p>';
		box.innerHTML += '<p>' + msg_b + '</p>';
		box.innerHTML += '<p>' + msg_c + '</p>';

		function analyse_compliance () {
			var length = global_0.length;
			var low_point = ['', 100];
			var high_point = ['', 0];
			var first = global_0[1];
			var last = global_0[length -1];

			for (var i = 1; i < length; i++) {
				current = global_0[i];
				prev = global_0[i - 1];

				// Track high and low point
				if (current[1] > high_point[1]) {
					high_point = current;
				}
				if (current[1] < low_point[1]) {
					low_point = current;
				}
			}
			var delta = last[1] - first[1];
			var append = '';
			if (delta > 0) {
				append = '+';
			}
			var hist_high = '';
			var hist_low = '';

			if (high_point[1] == last[1])
				hist_high = 'We are at a historical high from the ' + (length -1) + ' records available. ';
			else
				hist_high = 'The historical high value of ' + Math.round(high_point[1]*100)/100 + '% was recorded on ' + high_point[0] + '. ';

			if 	(low_point[1] == last[1])
				hist_low = 'We are at a historical low from the ' + (length -1) + ' records available. ';
			else
				hist_low = 'The historical low value of ' + Math.round(low_point[1]*100)/100 + '% was recorded on ' + low_point[0] + '. ';

			var msg = 'Compliance is at <b>' + Math.round(last[1]*100)/100 
					+ '%</b> (' + last[0] + '), '
					+ 'from ' + Math.round(first[1]*100)/100 
					+ '% (' + append + '' + Math.round(delta*100)/100
					+ '%) on the ' + first[0] + '. ' + hist_high + hist_low
			;

			return msg;
		}

		function analyse_vulnerability () {
			var length = global_1.length;
			var low_point = ['', 0, 0, 999999999];
			var high_point = ['', 0, 0, 0];
			var first = global_1[1];
			var last = global_1[length -1];

			for (var i = 1; i < length; i++) {
				current = global_1[i];
				prev = global_1[i - 1];

				// Track high and low point
				if (current[3] > high_point[3]) {
					high_point = current;
				}
				if (current[3] < low_point[3]) {
					low_point = current;
				}
			}

			var delta = last[3] - first[3];
			var append = '';
			if (delta > 0) {
				append = '+';
			}

			var hist_high = '';
			var hist_low = '';

			if (high_point[1] == last[1])
				hist_high = 'We are at a historical high from the ' + (length -1) + ' records available. ';
			else
				hist_high = 'The historical high value of ' + numberWithCommas(high_point[3]) + ' vulnerable updates was recorded on ' + high_point[0] + '. ';

			if 	(low_point[1] == last[1])
				hist_low = 'We are at a historical low from the ' + (length -1) + ' records available. ';
			else
				hist_low = 'The historical low value of ' + numberWithCommas(low_point[3]) + ' vulnerable updates was recorded on ' + low_point[0] + '. ';

			var msg = 'We currently have <b>' + numberWithCommas(last[3])
					+ ' vulnerable updates</b> (' + last[0] + '), '
					+ 'from ' + numberWithCommas(first[3])
					+ ' vulnerable updates (' + append + '' + numberWithCommas(delta)
					+ ') on the ' + first[0] + '. ' + hist_high + hist_low
			;

			return msg;
		}

		function analyse_pccompl () {
			var length = pccompl_full.length;
            var compl_all = 0;
			var compl_top = 0;
			var compl_mid = 0;

			if (length == 0)
				return '';

			for (var i = 1; i < length; i++) {
				if (parseInt(pccompl_full[i]) > 74) {
					var s = pccompl_full[i][5];
					var j = s.indexOf('(') + 1;
					var k = s.indexOf('% of');

					compl_all += parseFloat(s.substring(j, k))
				}
				if (parseInt(pccompl_full[i]) > 89) {
					var s = pccompl_full[i][5];
					var j = s.indexOf('(') + 1;
					var k = s.indexOf('% of');

					compl_mid += parseFloat(s.substring(j, k))
				}
				if (parseInt(pccompl_full[i]) > 94) {
					var s = pccompl_full[i][5];
					var j = s.indexOf('(') + 1;
					var k = s.indexOf('% of');

					compl_top += parseFloat(s.substring(j, k))
				}
			}

			var msg = '<b>' + compl_top.toFixed(2) + '%</b> of computers are at <i>95%</i> compliance or above';
			msg += ', <b>' + compl_mid.toFixed(2) + '%</b> of computers are at <i>90%</i> compliance or above';
			msg += ' and <b>' + compl_all.toFixed(2) + '%</b> of computers are at <i>75%</i> compliance or above</i>.';

			return msg;
		}
		</script>
";

        public static string html_PcCompl_div = @"
    <hr/>
    <p style='text-align: center;'><b>Compliance by computer - upper quarter</b></p>
    <div id='pccompl_div' style='width: 1000px; height: 300px;'></div>";

        public static string html_PcComplAndInactive_div = @"
    <hr/>
    <table style='width: 80%'>
        <tr>
            <td style='text-align: center'><b>Compliance by Computer (85%+)</b></td> 
            <td style='text-align: center'><b>Inactive computers (in % of managed)</b></td>
        </tr>
        <tr>
            <td><div id='pccompl_div' style='width: 500px; height: 300px;'></div></td>
            <td><div id='inactivepc_div' style='width: 500px; height: 300px;'></div></td>
        </tr>
    </table>
";

        public static string html_PcInactive_div = @"
    <hr/>
    <p style='text-align: center;'><b>Inactive computers (in % of managed)</b></p>
    <div id='inactivepc_div' style='width: 1000px; height: 300px;'></div>";

        public static string html_DailySummary_div = @"
	<div id='daily_summary'></div>
";

        public static string html_BulletinSearch = @"
    <hr/>
	Bulletin name: <input type='text' id='bulletin_name'></input><input type='button' value='View graphs' onclick='loadBulletin()'/>
    <hr/>";

        #endregion

        #region Global Compliance Javascript
        public static string js_GlobalCompliance = @"
        function drawChart() {
	        var options1 = { title: '', vAxis: { maxValue : 100, minValue : 0 }};
	        var options2 = { title: '', vAxis: { minValue : 0 }};

			global_0 = formatDateString(global_0, 0);
		    var d_global_0 = google.visualization.arrayToDataTable(global_0);
		    var g_global_0 = new google.visualization.LineChart(document.getElementById('global_div_0'));
		    g_global_0.draw(d_global_0, options1);

			global_1 = formatDateString(global_1, 0);
		    var d_global_1 = google.visualization.arrayToDataTable(global_1);
		    var g_global_1 = new google.visualization.LineChart(document.getElementById('global_div_1'));
		    g_global_1.draw(d_global_1, options2);

			if (typeof(pccompl) != 'undefined' && pccompl != null) {
//            if (pccompl.length > 0) {
		        var d_pccompl = new google.visualization.DataTable();
                d_pccompl.addColumn('number', 'Compliance in %');
		        d_pccompl.addColumn('number');
		        d_pccompl.addColumn('number');
		        d_pccompl.addColumn('number');
		        d_pccompl.addColumn('number');
		        d_pccompl.addColumn({type:'string', role:'tooltip', 'p': {'html': true}});
		        d_pccompl.addRows(pccompl);

                var g_pccompl = new google.visualization.CandlestickChart(document.getElementById('pccompl_div'));
                g_pccompl.draw(d_pccompl, { legend:'none', tooltip: {isHtml: true}} );
            }

			if (typeof(inactive_computers_pc) != 'undefined' && inactive_computers_pc != null) {
//            if (inactive_computers_pc.length > 0) {
                var d_inactive = google.visualization.arrayToDataTable(formatDateString(inactive_computers_pc, 0));
                var g_inactive = new google.visualization.LineChart(document.getElementById('inactivepc_div'));
                g_inactive.draw(d_inactive, {colors: ['orange', 'red', 'royalblue', 'forestgreen']});	
            }
        }";
        #endregion

        #region Get Bulletin (html + js page)
        public static string html_GetBulletin_page = @"<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Strict//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd'>
<html xmlns='http://www.w3.org/1999/xhtml'>
<head>
	<title>Bulletin detailed view</title>
	<script type='text/javascript' src='https://www.google.com/jsapi'></script>
	<script type='text/javascript' src='javascript/helper.js'></script>
    <link rel='stylesheet' type='text/css' href='menu.css'>
	<script type='text/javascript'>google.load('visualization', '1.1', {packages: ['corechart', 'controls']});</script>
</head>
<body>" + html_navigationbar + @"
	<h2 id='t_012' style='width: 800px; text-align: center'></h2>
	<h3>Installed versus Applicable</h3>
    <div id='dashboard_vuln'>
        <div id='chart_vuln' style='width: 915px; height: 300px;'></div>
        <div id='control_vuln' style='width: 915px; height: 50px;'></div>
    </div>
	<h3>Compliance in %</h3>
    <div id='dashboard_compl'>
        <div id='chart_compl' style='width: 915px; height: 300px;'></div>
        <div id='control_compl' style='width: 915px; height: 50px;'></div>
    </div>
  </body>
	<script type='text/javascript'>
		var bulletin = window.location.search.substring(1).toLowerCase();

		function loadjs(filename){
			var fileref = document.createElement('script')
			fileref.setAttribute('type','text/javascript')
			fileref.setAttribute('src', filename)
			
			if (typeof fileref!='undefined')
				document.getElementsByTagName('head')[0].appendChild(fileref)
		}
			 
		loadjs('javascript/' + escapeString(bulletin) + '_0.js');
		loadjs('javascript/' + escapeString(bulletin) + '_1.js');

		function drawVisualization() {
		
			b = escapeString(bulletin);

            var data_compl = window[b + '_0'];
            var data_vuln = window[b + '_1'];

			if (typeof(data_compl) == 'undefined' || typeof(data_vuln) == 'undefined') {
				document.getElementById('dashboard_vuln').innerHTML = '<p>No trending data is available...</p>';
				return;
			}

			var dashboard_compl = new google.visualization.Dashboard(document.getElementById('dashboard_compl'));
			var dashboard_vuln = new google.visualization.Dashboard(document.getElementById('dashboard_vuln'));

			var control_compl = new google.visualization.ControlWrapper({
					'controlType': 'ChartRangeFilter',
					'containerId': 'control_compl',
					'options': {
					'filterColumnIndex': 0,
					'ui': {
						'chartType': 'LineChart',
						'chartOptions': {
							'chartArea': {'width': '90%'},
							'hAxis': {'baselineColor': 'none'}
						},
						'chartView': {
							'columns': [0, 1]
						}, 'minRangeSize': 7
						}
					}
				});

			var chart_compl = new google.visualization.ChartWrapper({
				'chartType': 'LineChart',
				'containerId': 'chart_compl',
				'options': {
				'chartArea': {'height': '80%', 'width': '90%'},
				'tooltip': {isHtml: false},
				'hAxis': {'slantedText': false},
				'vAxis': {'viewWindow': {'min': 0, 'max': 101}},
				'legend': {'position': 'none'}
				},
			});

			var d_compl = new google.visualization.arrayToDataTable(formatToDate(data_compl, 0));

			dashboard_compl.bind(control_compl, chart_compl);
			dashboard_compl.draw(d_compl);

			var dashboard_vuln = new google.visualization.Dashboard(document.getElementById('dashboard_vuln'));
			var dashboard_vuln = new google.visualization.Dashboard(document.getElementById('dashboard_vuln'));

			var control_vuln = new google.visualization.ControlWrapper({
					'controlType': 'ChartRangeFilter',
					'containerId': 'control_vuln',
					'options': {
                    'backgroundColor' : { fill:'transparent' },
					'filterColumnIndex': 0,
					'ui': {
						'chartType': 'LineChart',
						'chartOptions': {
							'chartArea': {'width': '90%'},
							'hAxis': {'baselineColor': 'none'}
						},
						'chartView': {
							'columns': [0, 3]
						}, 'minRangeSize': 7
						}
					}
				});

			var chart_vuln = new google.visualization.ChartWrapper({
				'chartType': 'LineChart',
				'containerId': 'chart_vuln',
				'options': {
                'backgroundColor' : { fill:'transparent' },
				'chartArea': {'height': '80%', 'width': '90%'},
				'tooltip': {isHtml: false},
				'hAxis': {'slantedText': false},
				'vAxis': {'viewWindow': {'min': 0}},
				'legend': {'position': 'none'}
				},
			});

			var d_vuln = new google.visualization.arrayToDataTable(formatToDate(data_vuln, 0));

			dashboard_vuln.bind(control_vuln, chart_vuln);
			dashboard_vuln.draw(d_vuln);

			}

        setTimeout(drawVisualization, 300);
        //google.setOnLoadCallback(drawVisualization);

        var head_link;
		if (bulletin == 'global'  || bulletin.search(/KB/i) > 1 || bulletin.search(/\.exe/i) > 1 || bulletin.search(/\.msi/i) > 1 || bulletin.search(/\.msp/i) > 1) {
			head_link =  bulletin.toUpperCase();
		} else {
			head_link =  '<a href=""' + bulletin + '.html"">' + bulletin.toUpperCase() + '</a>';
		}
		var t = document.getElementById('t_012').innerHTML = head_link;
    </script>
</html>
";
        #endregion

        #region Get Inactive Computers (html + js page)
        public static string html_GetInactiveComputers_page = @"<html>
  <head>
    <title>Inactive computer trending</title>
    <script type='text/javascript' src='https://www.google.com/jsapi'></script>
	<script type='text/javascript' src='javascript/helper.js'></script>
    <link rel='stylesheet' type='text/css' href='menu.css'>
	<script type='text/javascript' src='javascript/inactive_computers.js'></script>
	<script type='text/javascript' src='javascript/inactive_computers_pc.js'></script>
    <script type='text/javascript'>
      google.load('visualization', '1', {packages:['corechart']});
      google.setOnLoadCallback(switch_view);
	  
	  var data;
	  var options;

      inactive_computers = formatDateString(inactive_computers, 0);
      inactive_computers_pc = formatDateString(inactive_computers_pc, 0);

      function drawChart() {
        var chart = new google.visualization.LineChart(document.getElementById('chart_div'));
        chart.draw(data, options);	
      }
	  
	  var percent = false;
	  function switch_view() {
		if (percent) {
			percent = false;
			data = google.visualization.arrayToDataTable(formatDateString(inactive_computers_pc, 0));
	        options = {
			  title: '(in % of managed machines)', 
              backgroundColor: { fill:'transparent' },
			  colors: ['orange', 'red', 'royalblue', 'forestgreen']
			};
			drawChart();
		} else {
			percent = true;
			data = google.visualization.arrayToDataTable(formatDateString(inactive_computers, 0));
	        options = {
              backgroundColor: { fill:'transparent' },
			  title: '(in computer count)',
			  colors: ['orange', 'red', 'royalblue', 'forestgreen']
			};
			drawChart();
		}
	  }
    </script>
  </head>
  <body>" + html_navigationbar + @"
	<h2>Inactive computers over time</h2>
    <div id='chart_div' style='width: 900px; height: 500px;'></div>
	<a href='javascript:switch_view()' title='Click to switch between Computer Count view and % of Managed machines view.'>Switch View</a>
	<p><b><i>Note:</i></b> '7 days ++' reports the number of new inactive computers from the previous snapshot. '7 days --' reports the count of computers that were inactive in the previous snapshos and are not in the current snapshot (they are back to Active!).</p>
  </body>
</html>
";
        #endregion

        #region Computer Compliance (html + js page)
        public static string html_ComputerCompliance_page = @"<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Strict//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd'>
<html xmlns='http://www.w3.org/1999/xhtml'>
<head>
	<title>Compliance by Computer</title>
	<script type='text/javascript' src='javascript/helper.js'></script>
    <link rel='stylesheet' type='text/css' href='menu.css'>
	<script type='text/javascript' src='http://www.google.com/jsapi'></script>
	<script type='text/javascript' src='javascript/pccompl_full.js'></script>
	<script type='text/javascript'>google.load('visualization', '1.1', {packages: ['corechart', 'controls']});</script>
</head>
<body>" + html_navigationbar + @"
	<h2>Compliance by Computer</h2> 
    <div id='dashboard'>
        <div id='chart' style='width: 915px; height: 400px;'></div>
        <div id='control' style='width: 915px; height: 50px;'></div>
    </div>
  </body>
	<script type='text/javascript'>
		function drawVisualization() {
			var dashboard = new google.visualization.Dashboard(document.getElementById('dashboard'));

			var control = new google.visualization.ControlWrapper({
					'controlType': 'ChartRangeFilter',
					'containerId': 'control',
					'options': {
                    'backgroundColor' : { fill:'transparent' },
					'filterColumnIndex': 0,
					'ui': {
						'chartType': 'LineChart',
						'chartOptions': {
							'chartArea': {'width': '90%'},
							'hAxis': {'baselineColor': 'none'}
						},
						'chartView': {
							'columns': [0, 3]
						},
						'minRangeSize': 10
						}
					},
					'state': {'range': {'start': 80, 'end': 100}}
				});

			var chart = new google.visualization.ChartWrapper({
				'chartType': 'CandlestickChart',
				'containerId': 'chart',
				'options': {
                'backgroundColor' : { fill:'transparent' },
				'chartArea': {'height': '80%', 'width': '90%'},
				'tooltip': {isHtml: true},
				'hAxis': {'slantedText': false},
				'vAxis': {'viewWindow': {'min': 0}},
				'legend': {'position': 'none'}
				},
			});

			var d_pccompl = new google.visualization.DataTable();
			d_pccompl.addColumn('number', 'Compliance in %');
			d_pccompl.addColumn('number');
			d_pccompl.addColumn('number');
			d_pccompl.addColumn('number');
			d_pccompl.addColumn('number');
			d_pccompl.addColumn({type:'string', role:'tooltip', 'p': {'html': true}});
			d_pccompl.addRows(pccompl_full);

			dashboard.bind(control, chart);
			dashboard.draw(d_pccompl);
		}

		google.setOnLoadCallback(drawVisualization);
	</script>
</html>
";
        #endregion

        #region Javascript Helper functions
        public static string js_Helper = @"menu_status = new Array();

function showhide(elem_id){
    var switch_id = document.getElementById(elem_id);

	if (menu_status[elem_id] != 'show') {
		switch_id.className = 'show';
		menu_status[elem_id] = 'show';
	} else {
		switch_id.className = 'hide';
		menu_status[elem_id] = 'hide';
	}
}

function loadJs(filename){
	var fileref = document.createElement('script')
	fileref.setAttribute('type','text/javascript')
	fileref.setAttribute('src', filename)
	
	if (typeof fileref!='undefined')
		document.getElementsByTagName('head')[0].appendChild(fileref)
}

function escapeString(b) {
	var t = b.replace(/-/g, '_');
	t = b.replace(/\(/g, '_');
	t = b.replace(/\)/g, '_');
	return t.replace(/\./g, '_');
}

function formatToDate(table, column) {
	for (var i = 1; i < table.length; i++) {
		var d_base = table[i][column].split('T');
		var d = d_base[0].split('-');
		table[i][column] = new Date(d[0], d[1]-1, d[2]);
	}
	return table;
}

function formatDateString(table, column) {
	for (var i = 1; i < table.length; i++) {
		table[i][column] = table[i][column].replace('T', ', ');
		table[i][column] = table[i][column].replace(/\.[0-9][0-9][0-9]/g, ' ');
		table[i][column] = table[i][column].replace(/201[0-9]-01-/g, 'Jan ');
		table[i][column] = table[i][column].replace(/201[0-9]-02-/g, 'Feb ');
		table[i][column] = table[i][column].replace(/201[0-9]-03-/g, 'Mar ');
		table[i][column] = table[i][column].replace(/201[0-9]-04-/g, 'Apr ');
		table[i][column] = table[i][column].replace(/201[0-9]-05-/g, 'May ');
		table[i][column] = table[i][column].replace(/201[0-9]-06-/g, 'Jun ');
		table[i][column] = table[i][column].replace(/201[0-9]-07-/g, 'Jul ');
		table[i][column] = table[i][column].replace(/201[0-9]-08-/g, 'Aug ');
		table[i][column] = table[i][column].replace(/201[0-9]-09-/g, 'Sep ');
		table[i][column] = table[i][column].replace(/201[0-9]-10-/g, 'Oct ');
		table[i][column] = table[i][column].replace(/201[0-9]-11-/g, 'Nov ');
		table[i][column] = table[i][column].replace(/201[0-9]-12-/g, 'Dec ');
	}
	return table;
}
";
        #endregion

        #region Update page javascript (header link generaiton)
        public static string js_UpdatePageHeader = @"    <script type='text/javascript'>
		var bulletin = location.pathname.substring(location.pathname.lastIndexOf('/') + 1, location.pathname.lastIndexOf('.'));
		var head_link = '<a href=""getbulletin.html?' + bulletin + '"">' + bulletin.toUpperCase() + '</a>';
		document.getElementById('uHeader').innerHTML = head_link;
    </script>";
        #endregion

        #region Help file (html)
        public static string html_help = @"<html>
<head>
    <title>{CWoC} Patch trending help page</title>
	<link rel='stylesheet' type='text/css' href='menu.css'>
	<script type='text/javascript' src='javascript/helper.js'></script>
</head>
<body style='width: 900px;'>
<a class='menu' onclick='showhide(""_menu"")'><b>[Navigation]</b></a>
<div id='_menu' class='hide'> 
	<a href='./' class='submenu'>Home</a>
	<a href='./sitemap.html' class='submenu'>Sitemap</a>
</div>
<h2 id='top'>{CWoC} Patch Trending Help center</h2>
<h3>Content:</h3>
<ul>
    <li><a href='#intro'>Introduction</a></li>
    <li><a href='#landing'>The site Landing page</a>
		<ul>
			<li><a href='#globalsection'>Global compliance section</a></li>
			<li><a href='#dailysummary'>Daily summary</a></li>
			<li><a href='#quickaccess'>Quick access tool</a></li>
			<li><a href='#customsection'>Custom compliance section</a></li>
		</ul
	</li>
    <li><a href='#instvsappl'>Installed versus Applicable view</a></li>
    <li><a href='#complpercent'>Compliance in % view</a></li>
    <li><a href='#pccompl'>Compliance by Computer view</a></li>
    <li><a href='#inactive'>Inactive Computers view</a></li>
    <li><a href='#layouttxt'>Customisable site-layout</a></li>
	<li><a href='#sitemap'>Sitemap and site navigation</a></li>
	<li><a href='#googlecharts'>Google Charts API</a>
		<ul>
			<li><a href='#linecharts'>Line charts</a></li>
			<li><a href='#candlesticks'>Candle stick charts</a></li>
			<li><a href='#rangefilters'>Range filters</a></li>
		</ul>
	</li>
	<li><a href='#changelog'>Change log</a></li>
    <li><a href='#references'>External references</a></li>
</ul>
<h3 id='intro'>Introduction</h3>
<p>This site is generate from the {CWoC} site builder executable <a href='#ref1' id='_ref1'>[1]</a>. It contains pages to quickly access compliance data on currently active software bulletin and updates from all vendors. If you have gotten this far you probably have met all the dependencies but here is a summary just in case:</p>
<ul>
    <li>Patch trending report or stored procedure (required) <a href='#ref2' id='_ref2'>[2]</a><a href='#ref3' id='_ref3'>[3]</a></li>
    <li>Compliance by computer trending report or stored procedure (optional) <a href='#ref4' id='_ref4'>[4]</a><a href='#ref3' id='_ref3'>[3]</a></li>
    <li>Inactive computers trending report or stored procedure (optional) <a href='#ref5' id='_ref5'>[5]</a><a href='#ref3' id='_ref3'>[3]</a></li>
</ul>
<p>This help file should help clarify what the charts are showing and how the data is gathered or handled.</p>
<h4><a href='#top'>Top</a></h4>
<h3 id='landing'>The site Landing page</h3>
<p>The landing page is divided in 4 sections: the global compliance information, the daily summary, the bulletin quick access tool and the custom compliance views.</p>
<ul>
	<li id='globalsection'><p>The global compliance section is divided into 2, 3 or 4 charts depending on how many optionally modules were installed. Up top and side by side we find the mandatory <a href='#instvsappl'>Installed versus Applicable</a> and <a href='#complpercent'>Compliance in %</a> <a href='#linecharts'>line charts</a>. Then, if not additional modules are installed we jump to daily summary section. If only one module (<a href='#ref4' id='_ref4'>[4]</a> or <a href='#ref5' id='_ref5'>[5]</a>) it will then take the next sub-section in full. Finally if both optional modules are installed the <a href='#pccompl'>Compliance by Computer</a> graph will be displayed on the left, with the <a href='#inactive'>Inactive Computers</a> graph on the right.</p></li>
	<li id='dailysummary'><p>The daily summary section is the result of an anlysis from the global compliance data. It compares current compliance levels with historical high and low values, count of vulnerabilities against historical high and low values and provides a summary of the compliance by computers with 3 tranches: % of computers at 95% and above; % of computers at 90% and above; % of computers at 75% and above. We can easily derive the opposite values to flag the most vulnerable computers (computers with a low compliance level).</p>
	<li id='quickaccess'>The bulletin quick access tool allows you to jump to any bulletin view by bulletin name. The input is case insensitive and will redirect your browser to the page if data is found. If not a pop-up message will inform you that we could not find any compliance data for the specified bulletin. <b><i>Note!</i></b> This tool can allow you to view a little more than bulletin charts: try the keyword global and you should be able to view the global compliance graph in a larger page than the landing. You could also input an update name there and it would redirect you, but the lenghty names are much to prone to error for this feature to be widely advertised. However you'll see that we leverage it in the <a href='#sitemap'>sitemap</a>.</li>
	<li id='customsection'><p>The custom compliance view lists the default troubleshooting pages generated by the tool and any pages as defined in the <a href='#layouttxt'>customisable site-layout</a> text file used to generate the site. Currently the following pages are added to this section whether a custom site-layout is provided or not:
		<ul>
			<li><i>top10-vulnerable:</i> this page contains the summary charts for the 10 bulletin that are reporting the most vulnerabilities in descending order</li>
			<li><i>top25-vulnerable-upd:</i> this page contains the summary charts for the 25 updates that are reporting the most vulnerabilities in descending order</li>
			<li><i>top10-movers-up:</i> this page contains the summary charts for the 10 bulletins (or less) that have reduced their vulnerabilities the most since the previous record set.</li>
			<li><i>top10-movers-down:</i> this page contains the summary charts for the 10 bulletins (or less) that have increased in count of vulnerabilities the most since the previous record set.</li>
			<li><i>bottom-10-compliance:</i> this page contains the summary charts for the 10 bulletins that have the lowest compliance in %.</li>
			<li><i>bottom-25-compliance-upd:</i> this page contains the summary charts for the 25 updates that have the lowest compliance in %.</li>
			<li><i>compliance-by-computer:</i> this page contains a single <a href='#candlesticks'>candle stick chart </a> with a <a href='#rangefilters'>range-filter</a> that allows you to drill into the full comlpiance by computer range (from 0 to 100%).</i></li>
			<li><i>inactive-computers:</i> this page contains a single line chart displaying the count of inactive computers. </li>
		</ul>
	</p></li>
</ul>
<h4><a href='#top'>Top</a></h4>
<h3 id='instvsappl'>Installed versus Applicable view</h3>
<p>This view provides quite an insightful line chart (if not the most), has it displays 3 related datasets (per update or per bulletin depending on the entry you are checking) into a single timeline:
	<ul>
		<li><i>Applicable</i>: the count of software updates that are reported to be applicable by the computers running the Windows System Assessment Scan</li>
		<li><i>Installed</i>: the count of software updates that are reported to be installed by the computers running the Windows System Assessment Scan</li>
		<li><i>Vulnerable</i>: the difference between the count of applicable updates and the count of installed updates. This is a computed value from the 2 agent reported values above.</li>
	</ul>
The data series start either at the first collection point (for updates / bulletins that pre-existed the Patch Trending report executions) or at the day when computers first send compliance data. For the later you will normally see a step increase in applicable and vulnerable count prior to the updates being available and rolled out on the client. Then the install count will go up and the vulnerable will go down in parallel, whilst the overall applicable count normally rises steadily to due inactive computers coming back to operation based on their user schedules.
</p>
<h4><a href='#top'>Top</a></h4>
<h3 id='complpercent'>Compliance in % view</h3>
<p>This view displayed the compliance in % over time. The data is loaded from a javascript file and calculate when the site builder is executed by dividing the installed count by applicable count and multiplying the result by 100. This is a key metric for Patch Management administrator or Security specialists, but it is very insightful when you can see it side by side with the Installed versus Applicable charts (as the later provided data on the impacted population that is missing from the compliance in % chart alone).</p>
<h4><a href='#top'>Top</a></h4>
<h3 id='pccompl'>Compliance by Computer</h3>
<p>The compliance by computer gives us another angle at the same problem: making sure that we have as many vulnerabilities patched on as many computers as possible in a given environment.</p>
<p>The data collected here is a summary of the information provided by running the Patch Management Compliance by Computer report: instead of tracking down the compliance by computer (this would be far too much data, albeit it would be very interesting at least for troubleshooting) we track the count and percent of computers in all percentage points over time.</p><p>In this manner we can see that at the end of a patch cycle (just before the Patch Tuesday PMImport task runs) most of the computer estate is reporting compliance between 96 and 100%.</p><p>Then after the Windows Software Assessment Scan package is refreshed and running on the computers we the compliance by computer spreads to the left (96% and below depending on the environment), before compressing again to the right as we are coming closer to the end of the patch cycle.</p>
<h4><a href='#top'>Top</a></h4>
<h3 id='inactive'>Inactive computers</h3>
<p>Quickly deploying patches to large count of computers world wide (say 10,000+) is not an easy task, especially when the target is to have 95% of the computers compliant at 95% or above (aka the 95-95 rule), and when this target is not met we need to be able to explain why this is so.</p>
<p>So we need to know how many computers are inactive (off, or not reporting data) over time in order to factor those off-time in with the compliance data: given we have new updates to install every 4 to 5 weeks, having computers off during that time is going to make it harder to meet the 95-95 goal.</p>
<p>Here is the summary of the metrics gathered:</p>
<table align='center' border='1' cellpadding='1' cellspacing='1' style='width: 800px;'>
	<thead>
		<tr>
			<th scope='col' style='width: 200px;'>Metric name</th>
			<th scope='col' style='width: 672px;'>Description</th>
		</tr>
	</thead>
	<tbody>
		<tr>
			<td style='width: 200px;'>Managed computers</td>
			<td style='width: 672px;'>Count of managed computers in the SMP database.</td>
		</tr>
		<tr>
			<td style='width: 200px;'>7 days</td>
			<td style='width: 672px;'>Count of computers&nbsp;inactive for more than 7 days</td>
		</tr>
		<tr>
			<td style='width: 200px;'>17 days</td>
			<td style='width: 672px;'>Count of computers&nbsp;inactive for more than 17 days</td>
		</tr>
		<tr>
			<td style='width: 200px;'>7 days ++</td>
			<td style='width: 672px;'>Count of computers added to the &quot;7 days&quot; count. These are computers that were not inactive in the previous record (t -1)</td>
		</tr>
		<tr>
			<td style='width: 200px;'>7 days --</td>
			<td style='width: 672px;'>Count of computers removed from the &quot;7 days&quot; count. These are computers that were inactive at (t -1) and that are not currently inactive i.e. they are back to Active!</td>
		</tr>
	</tbody>
</table>
<h4><a href='#top'>Top</a></h4>
<h3 id='layouttxt'>Customisable site-layout</h3>
<p>The site builder will create pages per bulletins listing all the updates active within the bulletins, however these are not necessarily grouped in a way that makes sense to the user. Given Microsoft release software bulletins every month we decided to provided a mechanism to associate pages with bulletins: the site-layout.txt</p>
<p>This optional component allows you to define group of bulletins and to have them automatically added to the landing page. But rather than doing much explaining about the file, let's give a quick sample that should be self-explanatory:</p>
<blockquote><pre>
microsoft-2013-september, ms13-066, ms13-067, ms13-068, ms13-069, ms13-070, ms13-071, ms13-072, ms13-073, ms13-074, ms13-075, ms13-076, ms13-077, ms13-078, ms13-079
microsoft-2013-august, ms13-059, ms13-060, ms13-061, ms13-062, ms13-063, ms13-064, ms13-065, ms13-066
microsoft-2013-july, ms13-052, ms13-053, ms13-054, ms13-055, ms13-056, ms13-057, ms13-058
microsoft-2013-june, ms13-047, ms13-048, ms13-049, ms13-050, ms13-051
microsoft-2013-may, ms13-037, ms13-038, ms13-039, ms13-040, ms13-041, ms13-042, ms13-043, ms13-044, ms13-045
adobe-2013, APSB13-01,APSB13-02,APSB13-04,APSB13-05,APSB13-06,APSB13-08,APSB13-09,APSB13-11,APSB13-12,APSB13-14,APSB13-15,APSB13-16,APSB13-17,APSB13-18
adobe-2012, APSB12-01,APSB12-02,APSB12-03,APSB12-05,APSB12-07,APSB12-08,APSB12-09,APSB12-13,APSB12-14,APSB12-16,APSB12-17,APSB12-18,APSB12-19,APSB12-22,APSB12-23,APSB12-24,APSB12-27
</pre></blockquote>
The file is read line by line and each line is a list of comma separated values. The first value will be the page name, and any subsequent value will be treated as a bulletin name. When a bulletin on the site-layout.txt file is not found or doesn't contain any data the file is not displayed in the landing page. In this manner we can add every month all of the Microsoft bulletins release into a convenient page, without having to worry whether the bulletin is active or applicable to the environment.
<h4><a href='#top'>Top</a></h4>
<h3 id='sitemap'>Sitemap and site navigation</h3>
<p>In order to make the site easily discoverable and to avoid too many back-cliks for the user we have built a sitemap page and added a navigation menu in most of the pages (in effect all but the landing and sitemap pages).</p><p>The sitemap itself replaces a search function, as it contains an exhaustive list of bulletins and updates, plus links to the troubleshooting pages (top10-... etc). So if you want to find a specific update or KB you can open the site map and search for any text fragment using the browser built-in capabilities.</p><p>The navigation bar is displayed on the top right section of pages and is actionable via click events (click to unfold, click again to fold back). It contains 3 links: one to the landing page, one to the sitemap and one to this help center.
<h4><a href='#top'>Top</a></h4>
<h3 id='googlecharts'>Google Charts API</h3>
<p>The site builds up on the public Google charts API. In fact the SiteBuilder was crafted in just a few days thanks to the easy to use API. We use the following elements to display data in a user friendly and easy to understand / interact with manner:</p>
<ul>
	<li id='linecharts'><p>Line charts: these are the base and are seen throught the site. All compliance by bulletin and updates use this chart type. We can draw multiple series together to see how the patching process performs over time.</p></li>
	<li id='candlesticks'><p>Candle stick charts: these are used for the compliance by computer only. They were the most practical option to show the current data for each percentage point and to show historical data all in one go: in one entry we can see the historical high and low values (the single vertical line) and the previous values compared to the current value (the boxes, with a blue color filled box meaning that the current value is on the upper section and a white colour filled box indicating that the current value is the bottom section).</a></li>
	<li id='rangefilters'><p>Range filters: range filters are used to easily zoom in or out of a chart section. This was first implemented in the Compliance by computer page and was generalised to the bulletin / update view page.</p></li>
</ul>
<h4><a href='#top'>Top</a></h4>
<h3 id='changelog'>Changelog</h3>
<h4>Release 15</h4>
<p>This release contains a major codefix, a minor codefix and two important new features and a minor CLI change:</p>
<ul>
<li>Code fix (1): Modified the getbulletin.html page to ensure it loads charts properly under various Internet Explorer versions (tested on Version 8, 9 and 10)</li>
<li>Code fix (2): Modified getbulletin.html to verify whether trending data exists or not for the requested entry. If not the message 'No data is available...' is displayed.</li>
<li><p>Feature (1): Added command line option /write-all to prevent the following static pages from being over-written with each site builder invocation (i.e. they will only be overwritten if you invoke 'sitebuilder.exe /write-all'):</p>
<ul>
<li>inactive-computers.html</li>
<li>compliance-by-computer.html</li>
<li>getbulletin.html</li>
<li>webpart-fullview.html</li>
<li>menu.css</li>
<li>help.html</li>
<li>javascript/helper.js</li>
</ul>
<p>You will notice that this feature include the menu.css. This will allow you to customise the look and feel of the site without loosing your work in between all execution. The same is true for the html pages, as you can now customise them further without the risk of loosing them.</p>
</li>
<li>Feature (2): Added a new html page name 'webpart-fullview.html'. This page is a copy of getbulletin.html without the site navigation. It is designed to be used inside the SMP console right-click actions inside a virtual window.</li>
<li>CLI change: Added a standard message to display all valid option when invoking the executable with the help paremeter (/? or --help)</li>
</ul>
<h4>Release 14</h4>
<p>Adding the stored procedure code inside the site builder to simplify the installation process. The command line invocation is simple: 'sitebuilder.exe /install'.</p>
<p><b><i>Note!</i></b> This will reset the stored procedures to default if they were customized.</p>
<h4>Release 13</h4>
<p>Added some information in the help section. Also generalized the menu to all pages and changed some of the page linking. One important feature is that the site layout file is now optional, as the site navigation does not depend on customised pages. Also fixed a few problems and improve code ordering.</p>
<h4>Release 12</h4>
<p>Version 12 is here with massive amount of changes. A full release note article will be published soon, but here&#39;s a short list of additions / improvements: all dates are not ISO based and displayed on the graphs using the MMM dd (for example 2013-07-14 is displayed Jul 14). We have a new site layout that lists all Microsoft bulletins by month, all the way to January 2009, we now have a site map, headers (linked or not) on all stub pages, a navigation tool, a help centre (empty for now), we filter out superseded / inactive updates / bulletins from the site, we added a Compliance by Computer page that use a range selector and we have used the same range selector in the bulletin / update page (getbulletin.html).</p>
<h4>Release 11</h4>
<p>Added Inactive computer trending pages. One page is added to the custom compliance view, and if the data exist a graph is added to the landing page, beside the compliance by computer graph if you have this trending enable, or on its own (see the 2 screen shots added above).</p>
<h4>Release 10</h4>
<p>Added two troubleshooting pages to list the top 10 bulletins with most changes up (net increase in installed updates) &nbsp;and down (net increase in vulnerable count). Also took some times to re-order the html pages generated. In this manner the browser will display the html content before it tries to build up the graphs in javascript. Finally I added page title to all generated html pages for additional clarity on the site.</p>
<h4>Release 9</h4>
<p>Fixed the landing page search function. It will now only redirect to the getbulletin.html page if we can find data for the user input (bulletin name).</p>
<h4>Release 8c</h4>
<p>Added compliance by computer graphics. This is a single graphs that shows on the landing page if you have enabled Compliance by Computer trending reports (awaiting release here on Connect). The graphs is of Candlestick type and shows data as illustrated above. With enough trending done you will see single line going thru the boxes. This is because we display the historical low, historical high and changes since the previous data capture.</p>
<p>Note that you can use this version without having the Compliance by Computer report running, as this is an optional add-on.</p>
<h4>Release 6c</h4>
<p>Fixed a problem with Internet Explorer support. The pages now render properly for IE 8.0 and above. It may work with IE 7 but was not tested yet.</p>
<h4>Release 6b</h4>
<p>Switched the compliance data to be computed from the installed versus applicable datasets, thus reducing the amount of SQL queries executed by half.</p>
<h4>Release 6</h4>
<p>Introduced vulnerable count on the Installed vers Applicable graphs. This gives us 3 lines (curves) that are easy to comprehend as you can see from the sample above.</p>
<h4>Release 5b</h4>
<p>Corrected some performance issues from the previous build and added instrumentation. The site builder now logs entry in the Altiris Logs and will indicate the count of html and js pages generated as well as the count of SQL queries it ran. During the performance issue troubleshooting we considered using a single Databasecontext entry but this was a wrong lead. The problem was database performance as the use of code based stop watch indicated. This was fixed by a non-clustered index on the table to keep track of data by updates.</p>
<h4>Release 5</h4>
<p>Refactored the graph per update generation. Added the link to the bulletin update page on the bulletin view and on the various aggregate pages.</p>
<h4>Release 4</h4>
<p>Introduced the Updates per bulletin pages. This pages are crafted for all the bulletins found in the trending table, and each page is named after the bulletin (escaped by replacing dot and hyphens with underscore.</p>
<h4>Release 3</h4>
<p>Introduced the global compliance graphs on the landing page. This makes the first look at the site very powerful, as we get compliance levels for the entire estate.</p>
<p>There were no prior release (or production use) of the tool.</p><h4><a href='#top'>Top</a></h4>
<h3 id='references'>External references</h3>
<a id='ref1' href='#_ref1'>[1]</a> <a target='_blank' href='http://www.symantec.com/connect/downloads/cwoc-patch-trending-sitebuilder'>{CWoc} Patch Trending SiteBuilder</a><br/>
<a id='ref2' href='#_ref2'>[2]</a> <a target='_blank' href='http://www.symantec.com/connect/articles/cwoc-patch-trending-adding-patch-compliance-trending-capacity-smp-simple-running-report-dai'>{CWoC} Patch Trending: Adding Patch Compliance Trending Capacity to SMP is as Simple as Running a Report Daily</a><br/>
<a id='ref3' href='#_ref3'>[3]</a> <a target='_blank' href='http://www.symantec.com/connect/downloads/cwoc-patch-trending-stored-procedures'>{CWoC} Patch Trending Stored Procedures</a><br/>
<a id='ref4' href='#_ref4'>[4]</a> <a target='_blank' href='http://www.symantec.com/connect/articles/cwoc-patch-trending-adding-compliance-computer-module'>{CWoC} Patch Trending: Adding a Compliance by Computer module</a><br/>
<a id='ref5' href='#_ref5'>[5]</a> <a target='_blank' href='http://www.symantec.com/connect/articles/cwoc-patch-trending-inactive-computer-trending-report'>{CWoC} Patch Trending: Inactive Computer Trending Report</a><br/>

<h4><a href='#top'>Top</a></h4>
<p>For additional documentation please refer to <a href='http://www.symantec.com/connect/search/apachesolr_search/cwoc%20patch%20trending'>Symantec Connect</a>. Support for this CWoC project is also available thru Symantec Connect.</p>
</body>
<!-- MODEL HTML
Reference pointer: <a href='#ref6' id='_ref6'>[6]</a>
Reference entry: <a id='ref6' href='#_ref6'>[6]</a> <a target='_blank' href='#externalref'>TTT</a><br/>

Content entry:
	<li><a href='#'></a></li>
Subsection:
	<h3 id=''></h3>
	<p></p>
	<h4><a href='#top'>Top</a></h4>
Note: <b><i>Notes!</i></b> 
-->
</html>";
        #endregion

        #region CLI Help
        public static string CLIHelp = @"
Welcome to the Patch Trending SiteBuilder. Here are the currently supported
command line arguments:

    /install

        This command line installs the pre-requisite stored procedures to the
        Symantec CMDB and terminates.

    /write-all

        This command line will prevent static html and css  files from being 
        written to disk. This allows you to customise the site look and feel
        to better suit your needs.

    /?

        This command line prints out this help message and terminates.
        ";
        #endregion

        #region Default Pages strings
        public static string[,] DefaultPages = new string[6,3] {
                {"top10-vulnerable", SQLStrings.sql_get_topn_vulnerable, "Generating Top 10 bulletins by vulnerable computers page..."},
                {"top25-vulnerable-upd", SQLStrings.sql_get_topn_vulnerable_upd, "Generating Top 25 update by vulnerable computers page..." },
                {"top10-movers-up", SQLStrings.sql_get_topn_movers_up, "Generating Top 10 movers (++) page..." },
                {"top10-movers-down", SQLStrings.sql_get_topn_movers_down, "Generating Top 10 movers (--) page..." },
                {"bottom-10-compliance", SQLStrings.sql_get_bottomn_compliance, "Generating Bottom 10 bulletins by compliance..." },
                {"bottom-25-compliance-upd", SQLStrings.sql_get_bottomn_compliance_upd, "Generating Bottom 25 updates by compliance..." },
        };
        #endregion

        #region webpart-fullview (html)
        public static string html_webpart_fullview = @"<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Strict//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd'>
<html xmlns='http://www.w3.org/1999/xhtml'>
<head>
	<title>Bulletin detailed view</title>
	<script type='text/javascript' src='http://www.google.com/jsapi'></script>
	<script type='text/javascript' src='javascript/helper.js'></script>
    <link rel='stylesheet' type='text/css' href='menu.css'>
	<script type='text/javascript'>google.load('visualization', '1.1', {packages: ['corechart', 'controls']});</script>
</head>
<body>
	<h4 id='t_012' style='text-align: center'></h4>
    <div id='dashboard_vuln'>
        <div id='chart_vuln' style='height: 200px;'></div>
        <div id='control_vuln' style='height: 50px;'></div>
    </div>
    <div id='dashboard_compl'>
        <div id='chart_compl' style='height: 200px;'></div>
        <div id='control_compl' style='height: 50px;'></div>
    </div>
  </body>
	<script type='text/javascript'>
		var bulletin = window.location.search.substring(1).toLowerCase();

		function loadjs(filename){
			var fileref = document.createElement('script')
			fileref.setAttribute('type','text/javascript')
			fileref.setAttribute('src', filename)
			
			if (typeof fileref!='undefined')
				document.getElementsByTagName('head')[0].appendChild(fileref)
		}
			 
		loadjs('javascript/' + escapeString(bulletin) + '_0.js');
		loadjs('javascript/' + escapeString(bulletin) + '_1.js');

		function drawVisualization() {
		
			b = escapeString(bulletin);
			
			
			var data_compl = window[b + '_0'];
			var data_vuln = window[b + '_1'];

			if (typeof(data_compl) == 'undefined' || typeof(data_vuln) == 'undefined') {
				document.getElementById('dashboard_vuln').innerHTML = '<p>No trending data is available...</p>';
				return;
			}
			
			var dashboard_compl = new google.visualization.Dashboard(document.getElementById('dashboard_compl'));
			var dashboard_vuln = new google.visualization.Dashboard(document.getElementById('dashboard_vuln'));

			var control_compl = new google.visualization.ControlWrapper({
					'controlType': 'ChartRangeFilter',
					'containerId': 'control_compl',
					'options': {
					'filterColumnIndex': 0,
					'ui': {
						'chartType': 'LineChart',
						'chartOptions': {
							'chartArea': {'width': '90%'},
							'hAxis': {'baselineColor': 'none'}
						},
						'chartView': {
							'columns': [0, 1]
						}, 'minRangeSize': 7
						}
					}
				});

			var chart_compl = new google.visualization.ChartWrapper({
				'chartType': 'LineChart',
				'containerId': 'chart_compl',
				'options': {
				'chartArea': {'height': '80%', 'width': '90%'},
				'tooltip': {isHtml: false},
				'hAxis': {'slantedText': false},
				'vAxis': {'viewWindow': {'min': 0, 'max': 101}},
				'legend': {'position': 'none'}
				},
			});

			var d_compl = new google.visualization.arrayToDataTable(formatToDate(data_compl, 0));

			dashboard_compl.bind(control_compl, chart_compl);
			dashboard_compl.draw(d_compl);

			var dashboard_vuln = new google.visualization.Dashboard(document.getElementById('dashboard_vuln'));
			var dashboard_vuln = new google.visualization.Dashboard(document.getElementById('dashboard_vuln'));

			var control_vuln = new google.visualization.ControlWrapper({
					'controlType': 'ChartRangeFilter',
					'containerId': 'control_vuln',
					'options': {
                    'backgroundColor' : { fill:'transparent' },
					'filterColumnIndex': 0,
					'ui': {
						'chartType': 'LineChart',
						'chartOptions': {
							'chartArea': {'width': '90%'},
							'hAxis': {'baselineColor': 'none'}
						},
						'chartView': {
							'columns': [0, 3]
						}, 'minRangeSize': 7
						}
					}
				});

			var chart_vuln = new google.visualization.ChartWrapper({
				'chartType': 'LineChart',
				'containerId': 'chart_vuln',
				'options': {
                'backgroundColor' : { fill:'transparent' },
				'chartArea': {'height': '80%', 'width': '90%'},
				'tooltip': {isHtml: false},
				'hAxis': {'slantedText': false},
				'vAxis': {'viewWindow': {'min': 0}},
				'legend': {'position': 'none'}
				},
			});

			var d_vuln = new google.visualization.arrayToDataTable(formatToDate(data_vuln, 0));

			dashboard_vuln.bind(control_vuln, chart_vuln);
			dashboard_vuln.draw(d_vuln);

			}

        setTimeout(drawVisualization, 150);

        var head_link =  bulletin.toUpperCase();
		document.getElementById('t_012').innerHTML = head_link;
    </script>
</html>
";
        #endregion
    }
}
