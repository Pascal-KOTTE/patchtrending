using Altiris.NS.ContextManagement;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Sql;
using System.IO;
using System.Text;
using Symantec.CWoC.APIWrappers;

namespace Symantec.CWoC.PatchTrending {
	class PatchTrending {

		static int Main(string[] args) {

			string filename = "SiteLayout.txt";
			bool write_all = false;
			bool runforcollection = false;
			string collectionguid = "01024956-1000-4cdb-b452-7db0cff541b6";

			 Altiris.NS.Logging.EventLog.ReportInfo("PatchTrending is starting...");

			if (args.Length > 0) {
				bool build = false;
				foreach (string arg in args) {
					string _arg = arg.ToLower();
					if (_arg.ToLower() == "/install") {
						return Installer.install("ALL");
					} 
					if (_arg.ToLower() == "/write-all") {
						write_all = true;
						continue;
					}
					if (_arg.ToLower().StartsWith("/collectionguid=")) {
						collectionguid = _arg.Substring("/collectionguid=".Length);
						runforcollection = true;
					}
					if (_arg.ToLower() == "/collectdata") {
						return DataCollector.CollectData();
					} 
					if (_arg.StartsWith("/upgrade")) {
						string upgrade_guid = collectionguid;
						if (_arg.Contains("=")) {
							upgrade_guid = _arg.Substring("/upgrade=".Length);
						}
						return Upgrader.Upgrade(upgrade_guid);
					} 
					if (_arg.StartsWith("/version")) {
						Console.WriteLine("PatchTrending version 16");
						return 0;
					} 
					if (_arg.ToLower() == "/buildsite") {
						build = true;
						continue;
					} 
					if (_arg.ToLower() == "/?" || _arg.ToLower() == "/help"){
						Console.WriteLine(StaticStrings.CLIHelp);
						return 0;
					}
				}
				if (!build) {
						Console.WriteLine(StaticStrings.CLIHelp);
						return -1;
				}
			} else {
					Console.WriteLine(StaticStrings.CLIHelp);
					return 0;
			}

			int rc = 0;
			if (!Upgrader.NeedUpgrade()) {
				if (!File.Exists("SiteConfig.txt") || runforcollection) {
					Altiris.NS.Logging.EventLog.ReportInfo("SiteConfig.txt file does not exist or PatchTrending invoke with /collectionguid.");
					SiteBuilder builder = new SiteBuilder(write_all, collectionguid);
					rc = builder.Build(filename);
				} else {
					try {
						using (StreamReader reader = new StreamReader("SiteConfig.txt")) {
							string meta_index = "<h2><ul>";
							while (!reader.EndOfStream) {
								string line = reader.ReadLine();
								if (line.StartsWith("#") || line.Length == 0) {
									continue;
								}
								Altiris.NS.Logging.EventLog.ReportInfo(String.Format("Processing SiteConfig.txt line '{0}'.", line));
								string [] d = line.Split(',');
								string enabled = d[0].Trim();
								string collection_guid = d[1].Trim();
								string site_name = d[2].Trim();
								string site_description = d[3].Trim();
								string default_site = d[4].Trim();

								if (default_site == "1") {
									site_name = "."; // Write the default site to root
								}

								if (enabled == "1") {
									SiteBuilder builder = new SiteBuilder(write_all, collection_guid, site_name);
									rc = builder.Build(filename);

									// Add to meta-index file
									meta_index += String.Format("<li><a href='{0}'>{1}</a></li>", site_name, site_description);
								}
							}
							meta_index += "</ul></h2>";
							Altiris.NS.Logging.EventLog.ReportVerbose(String.Format("Saving data to file {0}:\n{1}", ("meta-index.html").ToLower(), meta_index));
							using (StreamWriter outfile = new StreamWriter("meta-index.html")) {
								outfile.Write(meta_index);
							}
						}
					} catch {
					}
				}
			} else {
				string n = "The database is not ready to run PatchTrending version 16. Please run the tool with the /upgrade command line first.";
				Console.WriteLine(n);
				Altiris.NS.Logging.EventLog.ReportInfo(n);
				return -1;
			}
			// Keep the process running for a few seconds to ensure all event logging is completed.
			System.Threading.Thread.Sleep(5000);
			return rc;
		}
	}

	class SiteBuilder {

		public string version = "version 17";
		private StringBuilder SiteMap;
		private bool WriteAll;
		private string CollectionGuid;
		private string SitePath;

		public SiteBuilder(bool write_all, string collectionguid) {
			Timer.Init();
			Counters.Init();

			WriteAll = write_all;
			CollectionGuid = collectionguid;
			SitePath = ".\\";

			Altiris.NS.Logging.EventLog.ReportInfo("SiteBuilder is starting...");
			SiteMap = new StringBuilder();

			// Make sure we have the required sub-folder for javascript files
			string js_folder = SitePath + "javascript";
			if (!Directory.Exists(js_folder)) {
				Directory.CreateDirectory(js_folder);
			}
		}

		public SiteBuilder(bool write_all, string collectionguid, string sitepath) {
			Timer.Init();
			Counters.Init();

			WriteAll = write_all;
			CollectionGuid = collectionguid;
			SitePath = sitepath + "\\";

			Altiris.NS.Logging.EventLog.ReportInfo("SiteBuilder is starting...");
			SiteMap = new StringBuilder();

			// Make sure we have the required sub-folder for javascript files
			string js_folder = SitePath + "javascript";
			if (!Directory.Exists(js_folder)) {
				Directory.CreateDirectory(js_folder);
			}
		}

		public int Build(string filename) {
			string line, pagename;
			StringBuilder filter = new StringBuilder();
			StringBuilder index = new StringBuilder();
			string[] d;


			/* Check that we can run against the database (I.e. prerequisite table does exist) */
			bool compliance_by_update = TestSql(String.Format("select top 1 1 from TREND_WindowsCompliance_ByUpdate where collectionguid = '{0}'", CollectionGuid));
			bool compliance_by_computer = TestSql(String.Format("select 1 from TREND_WindowsCompliance_ByComputer t  where collectionguid = '{0}' group by t._Exec_id having MAX(_exec_id) > 1", CollectionGuid));
			bool inactive_computer_trend = TestSql(String.Format("select top 1 1 from TREND_InactiveComputerCounts where collectionguid = '{0}'", CollectionGuid));

			if (compliance_by_update) {

				if (!File.Exists(SitePath + "menu.css") || WriteAll) {
					SaveToFile("menu.css", StaticStrings.css_navigation);
					++Counters.HtmlPages;
				}
				if (!File.Exists(SitePath + "help.html") || WriteAll) {
					SaveToFile("help.html", StaticStrings.html_help);
					++Counters.HtmlPages;
				}

				SiteMap.Append(StaticStrings.html_navigationbar_sitemap);
				SiteMap.AppendLine("<link rel='stylesheet' type='text/css' href='menu.css'>");
				SiteMap.AppendLine("<script type='text/javascript' src='javascript/helper.js'></script>");

				AddToSiteMap("Home page", "./");
				AddToSiteMap("Help center", "help.html");
				AddToSiteMap("Global compliance", "getbulletin.html?global");
				AddToSiteMap("Critical Severity updates compliance", "getbulletin.html?critical");
				AddToSiteMap("Important Severity updates compliance", "getbulletin.html?important");
				AddToSiteMap("Moderate Severity updates compliance", "getbulletin.html?moderate");
				AddToSiteMap("Unclassified Severity updates compliance", "getbulletin.html?unclassified");

				if (!File.Exists(SitePath + "javascript\\helper.js") || WriteAll) {
					SaveToFile("javascript\\helper.js", StaticStrings.js_Helper);
					++Counters.JsPages;
				}
				if (!File.Exists(SitePath + "getbulletin.html") || WriteAll) {
					SaveToFile("getbulletin.html", StaticStrings.html_GetBulletin_page);
					++Counters.HtmlPages;
				}
				if (!File.Exists(SitePath + "webpart-fullview.html") || WriteAll) {
					SaveToFile("webpart-fullview.html", StaticStrings.html_webpart_fullview);
					++Counters.HtmlPages;
				}

				// Generate default pages showing in the custom compliance view
				for (int i = 0; i < StaticStrings.DefaultPages.Length / 3; i++) {
					Altiris.NS.Logging.EventLog.ReportInfo(StaticStrings.DefaultPages[i, 2]);
					GeneratePage(StaticStrings.DefaultPages[i, 0], String.Format(StaticStrings.DefaultPages[i, 1], CollectionGuid));
					AddToIndex(ref index, StaticStrings.DefaultPages[i, 0]);
				}

				if (inactive_computer_trend) {
					 Altiris.NS.Logging.EventLog.ReportInfo("Generating Inactive-computers page...");
					GenerateInactiveComputerJs();

					if (!File.Exists(SitePath + "inactive-computers.html") || WriteAll) {
						SaveToFile("inactive-computers.html", StaticStrings.html_GetInactiveComputers_page);
						++Counters.HtmlPages;
					}
					AddToIndex(ref index, "inactive-computers");
					AddToSiteMap("inactive-computers", "inactive-computers.html");
				}

				if (compliance_by_computer) {
					 Altiris.NS.Logging.EventLog.ReportInfo("Generating Compliance-by-computer page...");

					if (!File.Exists(SitePath + "compliance-by-computer.html") || WriteAll) {
						SaveToFile("compliance-by-computer.html", StaticStrings.html_ComputerCompliance_page);
						++Counters.HtmlPages;
					}

					AddToIndex(ref index, "compliance-by-computer");
					AddToSiteMap("compliance-by-computer", "compliance-by-computer.html");
				}

				Altiris.NS.Logging.EventLog.ReportInfo("Generating site pages from the layout file...");
				if (File.Exists(filename)) {
					try {
						using (StreamReader reader = new StreamReader(filename)) {
							while (!reader.EndOfStream) {
								filter = new StringBuilder();
								line = reader.ReadLine();
								d = line.Split(',');

								pagename = d[0];
								Altiris.NS.Logging.EventLog.ReportInfo(pagename.ToUpper());
								if (d.Length > 1) {
									filter.Append("'" + d[1].Trim() + "'");
								}
								for (int i = 2; i < d.Length; i++) {
									filter.Append(", '" + d[i].Trim() + "'");
								}
								int j= GeneratePage(pagename, filter.ToString(), SQLStrings.sql_get_bulletins_in.Replace("{1}", CollectionGuid));
								if (j > 0)
									AddToIndex(ref index, pagename);
							}
						}
					} catch (Exception e){
						string msg = string.Format("Caught exception {0}\nInnerException={1}\nStackTrace={2}", e.Message, e.InnerException, e.StackTrace);
						Altiris.NS.Logging.EventLog.ReportError(msg);
					}
				}

				GenerateIndex(ref index, compliance_by_computer, inactive_computer_trend);
				GenerateGlobalPage();
				GenerateSeverityDataPages();
				Altiris.NS.Logging.EventLog.ReportInfo("Generating updates pages...");
				GenerateUpdatePages();

				SaveToFile("sitemap.html", SiteMap.ToString());
				++Counters.HtmlPages;

				Timer.Stop();
				string summary = string.Format("SiteBuilder took {0} ms to generate {1} pages ({2} html and {3} javascript) with {4} sql queries executed (returning {5} rows).", Timer.duration(), Counters.Pages, Counters.HtmlPages, Counters.JsPages, Counters.SqlQueries, Counters.SqlRows);
				Altiris.NS.Logging.EventLog.ReportInfo(summary);
			} else {
				Altiris.NS.Logging.EventLog.ReportError("We cannot execute anything as the prerequisite table TREND_WindowsCompliance_ByUpdate is missing or no data exists for the given collection guid.");
			}
			// Make sure we have an index file - no matter what
			string index_file = SitePath + "index.html";
			if(!File.Exists(index_file)) {
				SaveToFile(index_file, "<h2>No data currently exist for this collection...</h2>");
			}
			return 0;
		}

		private void AddToIndex(ref StringBuilder b, string s) {
			b.AppendFormat("<li><a href='{0}.html'>{0}</a></li>", s);
		}

		private void GenerateIndex(ref StringBuilder b, bool byComputer, bool inactive) {

			StringBuilder p = new StringBuilder();
			p.Append(StaticStrings.html_Landing);

			GeneratePcComplPages(byComputer, inactive);

			if (byComputer == true && inactive == false) {
				p.Append(StaticStrings.html_PcCompl_div);
			} else if (byComputer == false && inactive == true) {
				p.Append(StaticStrings.html_PcInactive_div);
			} else if (byComputer == true && inactive == true) {
				p.Append(StaticStrings.html_PcComplAndInactive_div);
			}

			p.Append(StaticStrings.html_DailySummary_div);
			p.Append(StaticStrings.html_BulletinSearch);

			// Add compliance by computer graphs here
			p.AppendLine("<h2 style=\"text-align: center; width:80%\">Custom compliance views</h2>");
			if (b.Length > 0) {
				p.AppendFormat("<div class=\"wrapper\"><ul>{0}</ul><br/></div>\n", b.ToString());
			}
			p.AppendFormat(FormattedStrings.html_footer, version, DateTime.Now.ToString());
			p.Append(StaticStrings.LandingJs);
			p.AppendLine("</body></html>");

			SaveToFile("index.html", p.ToString());
			SaveToFile("default.htm", p.ToString());
			Counters.HtmlPages += 2;
		}

		private void GeneratePcComplPages(bool hasData, bool smaller) {
			string data = "var pccompl = []";
			string data_full = "var pccompl_full = []";

			if (hasData) {
				int bottom = 74;
				if (smaller)
					bottom = 84;

				DataTable t = DatabaseAPI.GetTable(String.Format(SQLStrings.sql_get_compliance_bypccount, CollectionGuid));
				StringBuilder b = new StringBuilder();
				StringBuilder c = new StringBuilder();

				b.AppendLine("var pccompl = [");
				c.AppendLine("var pccompl_full = [");

				foreach (DataRow r in t.Rows) {
					if (Convert.ToInt32(r[0]) > bottom) {
						b.AppendFormat(FormattedStrings.js_CandleStickData, r[0], r[1], r[2], r[3], r[4], r[5]);
					}
					c.AppendFormat(FormattedStrings.js_CandleStickData, r[0], r[1], r[2], r[3], r[4], r[5]);
				}
				// Remove the last comma we inserted
				c.Remove(c.Length - 2, 1);
				c.AppendLine("]");

				b.Remove(b.Length - 2, 1);
				b.AppendLine("]");
				data = b.ToString();
				data_full = c.ToString();

				SaveToFile("javascript\\pccompl.js", data);
				SaveToFile("javascript\\pccompl_full.js", data_full);
				Counters.JsPages += 2;
			}
		}

		public void SaveToFile(string filepath, string data) {
			Altiris.NS.Logging.EventLog.ReportVerbose(String.Format("Saving data to file {0}:\n{1}", (SitePath + filepath).ToLower(), data));
			using (StreamWriter outfile = new StreamWriter(SitePath + filepath.ToLower())) {
				outfile.Write(data);
			}
		}

		private string GetJSString(string s) {
			s = s.Replace('-', '_');
			s = s.Replace('.', '_');
			s = s.Replace('(', '_');
			s = s.Replace(')', '_');
			s = s.ToLower();
			return s;
		}

		private bool TestSql(string sql) {
			try {
				if (DatabaseAPI.ExecuteScalar(sql) == 1) {
					return true;
				}
			} catch {
			}
			return false;
		}

		private int GeneratePage(string pagename, string sql) {
			return GeneratePage(pagename, "", sql);
		}

		private int GeneratePage(string pagename, string filter, string sql) {
			DataTable t;
			if (filter != "") {
				t = DatabaseAPI.GetTable(string.Format(sql, filter, CollectionGuid));
			} else {
				t = DatabaseAPI.GetTable(sql);
			}

			if (t.Rows.Count == 0) {
				return 0;
			}

			return GeneratePage(t, pagename, "");
		}

		private int GeneratePage(DataTable t, string pagename, string bulletin) {
			AddToSiteMap(pagename + "<ul>", pagename + ".html");

			StringBuilder drawChart = new StringBuilder();
			StringBuilder htmlDivs = new StringBuilder();
			StringBuilder jsInclude = new StringBuilder();

			StringBuilder bList = new StringBuilder();


			drawChart.AppendLine("function drawChart() {");
			drawChart.AppendLine("\tvar options1 = { backgroundColor: { fill:'transparent' },title: '', vAxis: { maxValue : 100, minValue : 0 }};\n");
			drawChart.AppendLine("\tvar options2 = { backgroundColor: { fill:'transparent' },title: '', vAxis: { minValue : 0 }};\n");

			htmlDivs.Append(StaticStrings.html_navigationbar);

			bool isBulletin = false, isUpdate = false;

			if (bulletin == "")
				isBulletin = true;

			if (pagename.EndsWith("-upd"))
				isUpdate = true;

			string entry = "", curr_bulletin = "", curr_graph = "", curr_data = "", curr_div = "";

			if (!isBulletin) {
				htmlDivs.AppendLine("<h2 style='text-align: center; width: 1000px' id='uHeader'></h2>");
				jsInclude.AppendLine(StaticStrings.js_UpdatePageHeader);
			} else {
				htmlDivs.AppendFormat("<h2 style='text-align: center; width: 1000px'>{0}</h2>", pagename);
			}

			foreach (DataRow r in t.Rows) {

				entry = r[0].ToString();
				curr_bulletin = GetJSString(entry);
				curr_data = "d_" + curr_bulletin;
				curr_graph = "g_" + curr_bulletin;
				curr_div = curr_bulletin + "_div";

				AddToSiteMap(entry.ToLower(), "getbulletin.html?" + entry.ToLower());

				jsInclude.AppendFormat("\t<script type=\"text/javascript\" src=\"javascript/{0}_0.js\"></script>\n"
					+ "\t<script type=\"text/javascript\" src=\"javascript/{0}_1.js\"></script>\n", curr_bulletin);

				drawChart.AppendFormat("\t\tvar {0}_0 = google.visualization.arrayToDataTable(formatDateString({1}_0, 0));\n"
					+ "\t\tvar {2}_0 = new google.visualization.LineChart(document.getElementById('{3}_0'));\n"
					+ "\t\t{2}_0.draw({0}_0, options1);\n"
					, curr_data, curr_bulletin, curr_graph, curr_div);

				// Generate the inst / appl graph js
				drawChart.AppendFormat("\t\tvar {0}_1 = google.visualization.arrayToDataTable(formatDateString({1}_1, 0));\n"
					+ "\t\tvar {2}_1 = new google.visualization.LineChart(document.getElementById('{3}_1'));\n"
					+ "\t\t{2}_1.draw({0}_1, options2);\n"
					, curr_data, curr_bulletin, curr_graph, curr_div);

				// Generate the header and divs
				if (isUpdate) {
					htmlDivs.AppendFormat("<h3><a href='getbulletin.html?{0}'>{1}</a></h3>\n", entry.ToLower(), entry);
				} else if (isBulletin) {
					htmlDivs.AppendFormat("<h3><a href='{0}.html'>{1}</a></h3>\n", entry.ToLower(), entry);
				} else {
					htmlDivs.AppendFormat("<h3>{0}</h3>\n", entry);
				}

				htmlDivs.AppendLine("<table width '80%'>");
				htmlDivs.AppendLine("<tr><td>Installed versus Applicable</td><td>Compliance status in %</td></tr><tr>");
				htmlDivs.AppendFormat("<td><div id='{0}_1' style='width: 500px; height: 200px;'></div></td>\n"
					+ "<td><div id='{0}_0' style='width: 500px; height: 200px;'></div></td>\n"
					, curr_div);
				htmlDivs.AppendLine("</tr></table>");

				if (isBulletin) {
					string bulletin_compliance = "";
					string bulletin_stats = "";

					GetBulletinData(ref bulletin_compliance, ref bulletin_stats, entry);

					SaveToFile("javascript\\" + curr_bulletin + "_0.js", bulletin_compliance);
					SaveToFile("javascript\\" + curr_bulletin + "_1.js", bulletin_stats);
				} else {
					string update_compliance = "";
					string update_stats = "";

					GetUpdateData(ref update_compliance, ref update_stats, entry, bulletin);

					SaveToFile("javascript\\" + curr_bulletin + "_0.js", update_compliance);
					SaveToFile("javascript\\" + curr_bulletin + "_1.js", update_stats);
				}
				Counters.JsPages += 2;
			}

			Altiris.NS.Logging.EventLog.ReportInfo(String.Format("Generating graphing javascript for {0}...", entry));

			drawChart.AppendLine("}");
			SaveToFile("javascript\\" + pagename + ".js", drawChart.ToString());
			Counters.JsPages++;

			Altiris.NS.Logging.EventLog.ReportInfo(String.Format("Generating html page for {0}...", entry));
			GenerateBulletinHtml(ref htmlDivs, ref jsInclude, pagename);
			Counters.HtmlPages++;

			SiteMap.Append("</ul>");
			return t.Rows.Count;
		}

		private void GenerateGlobalPage() {
			SaveToFile("javascript\\global.js", StaticStrings.js_GlobalCompliance);

			string globalcompliance = "";
			string globalstats = "";

			GetGlobalData(ref globalcompliance, ref globalstats);

			SaveToFile("javascript\\global_0.js", globalcompliance);
			SaveToFile("javascript\\global_1.js", globalstats);

			Counters.JsPages += 3;
			
		}

		private void GenerateSeverityDataPages() {
			string [] severities = new string [4] {"Critical", "Important", "Moderate", "Unclassified"};
			foreach (string severity in severities) {
				string globalcompliance = "";
				string globalstats = "";

				GetGlobalData_bysev(ref globalcompliance, ref globalstats, severity);

				SaveToFile("javascript\\" + severity + "_0.js", globalcompliance);
				SaveToFile("javascript\\" + severity + "_1.js", globalstats);

				Counters.JsPages += 2;
			}
		}

		private void GenerateBulletinHtml(ref StringBuilder divs, ref StringBuilder jsfiles, string pagename) {
			string html = String.Format(FormattedStrings.html_BulletinPage, pagename, divs.ToString(), jsfiles.ToString(), pagename.ToLower());
			SaveToFile(pagename + ".html", html);
		}

		private void GetUpdateData(ref string compliance, ref string stats, string update, string bulletin) {
			if (update.Length == 0 || update == string.Empty) {
				Altiris.NS.Logging.EventLog.ReportInfo(String.Format("GetUpdateData returns without generating compliance json for {0}.{1}.", bulletin, update));
				return;
			}

			string sql = String.Format(SQLStrings.sql_get_update_data, update, bulletin, CollectionGuid);
			DataTable t = DatabaseAPI.GetTable(sql);
			stats = GetJSONFromTable(t, update);

			string[,] _compliance = GetComplianceFromTable(t);
			compliance = GetComplianceJSONFromArray(_compliance, update);
			Altiris.NS.Logging.EventLog.ReportVerbose(String.Format("GetUpdateData processing with {0}.{1}.Data = \n{2}\n{3}", bulletin, update, compliance, stats));
		}

		private void GetBulletinData(ref string compliance, ref string data, string bulletin) {
			string sql = String.Format(SQLStrings.sql_get_bulletin_data, bulletin, CollectionGuid);
			DataTable t = DatabaseAPI.GetTable(sql);
			data = GetJSONFromTable(t, bulletin);

			string[,] _compliance = GetComplianceFromTable(t);
			compliance = GetComplianceJSONFromArray(_compliance, bulletin);
		}

		private void GetGlobalData(ref string compliance, ref string data) {
			DataTable t = DatabaseAPI.GetTable(String.Format(SQLStrings.sql_get_global_compliance_data, CollectionGuid));
			data = GetJSONFromTable(t, "global");

			string[,] _compliance = GetComplianceFromTable(t);
			compliance = GetComplianceJSONFromArray(_compliance, "global");
		}

		private void GetGlobalData_bysev(ref string compliance, ref string data, string severity) {
			DataTable t = DatabaseAPI.GetTable(String.Format(SQLStrings.sql_get_global_compliance_data_bysev, CollectionGuid, severity));
			data = GetJSONFromTable(t, severity);

			string[,] _compliance = GetComplianceFromTable(t);
			compliance = GetComplianceJSONFromArray(_compliance, severity);
		}

		private string GetComplianceJSONFromArray(string[,] t, string bulletin) {
			StringBuilder b = new StringBuilder();

			b.AppendLine("var " + GetJSString(bulletin) + "_0 = [");
			b.AppendLine("['Date', 'Compliance %'],");
			for (int i = 0; i < t.Length / 2; i++) {
				b.AppendLine("['" + t[i, 0] + "', " + t[i, 1].Replace(',', '.') + "],");
			}
			b.Remove(b.Length - 3, 1);
			b.AppendLine("]");

			return b.ToString();
		}

		private void GenerateInactiveComputerJs() {
			DataTable t = DatabaseAPI.GetTable(String.Format(SQLStrings.sql_get_inactive_computer_trend, CollectionGuid));
			SaveToFile("Javascript\\inactive_computers.js", GetInactiveComputer_JSONFromTable(t, "inactive_computers"));

			t = DatabaseAPI.GetTable(String.Format(SQLStrings.sql_get_inactive_computer_percent, CollectionGuid));
			SaveToFile("Javascript\\inactive_computers_pc.js", GetInactiveComputer_JSONFromTable(t, "inactive_computers_pc"));
			++Counters.JsPages;
		}

		private string[,] GetComplianceFromTable(DataTable t) {
			string[,] d = new string[t.Rows.Count, 2];
			int i = 0;

			foreach (DataRow r in t.Rows) {
				d[i, 0] = r[0].ToString();
				// Catch cases of division by zero
				if (Convert.ToInt32(r[1]) > 0) {
					Single compliance = Convert.ToSingle(r[1]) / Convert.ToSingle(r[2]) * 100;
					d[i, 1] = compliance.ToString();
				} else {
					d[i, 1] = "0";
				}
				i++;
			}

			return d;
		}

		private string GetJSONFromTable(DataTable t, string entry) {
			StringBuilder b = new StringBuilder();

			b.AppendLine("var " + GetJSString(entry) + "_1 = [");
			b.AppendLine("['Date', 'Installed', 'Applicable', 'Vulnerable'],");

			foreach (DataRow r in t.Rows) {
				int vulnerable = Convert.ToInt32(r[2]) - Convert.ToInt32(r[1]);
				b.AppendLine("['" + r[0].ToString() + "', " + r[1] + ", " + r[2] + ", " + vulnerable.ToString() + "],");
			}
			// Remove the last comma we inserted
			b.Remove(b.Length - 3, 1);
			b.AppendLine("]");

			return b.ToString();
		}

		private string GetInactiveComputer_JSONFromTable(DataTable t, string entry) {
			if (t.Rows.Count == 0) {
				string json = "var " + GetJSString(entry) + " = [];";
				return json;
			}

			StringBuilder b = new StringBuilder();

			b.AppendLine("var " + GetJSString(entry) + " = [");
			b.AppendLine("\t['Date', '7 days', '17 days', '7 days ++', '7 days --'],");

			foreach (DataRow r in t.Rows) {
				b.AppendLine("\t['" + r[0].ToString() + "', " + r[1].ToString().Replace(',', '.') + ", " + r[2].ToString().Replace(',', '.') + ", " + r[3].ToString().Replace(',', '.') + ", " + r[4].ToString().Replace(',', '.') + "],");
			}
			// Remove the last comma we inserted
			b.Remove(b.Length - 3, 1);
			b.AppendLine("];");

			return b.ToString();
		}

		private void GenerateUpdatePages() {
			 Altiris.NS.Logging.EventLog.ReportInfo("Generating update pages for bulletins now...");
			DataTable t = DatabaseAPI.GetTable(String.Format(SQLStrings.sql_get_all_bulletins, CollectionGuid));
			string bulletin;

			foreach (DataRow r in t.Rows) {
				bulletin = r[0].ToString();
				DataTable u = DatabaseAPI.GetTable(string.Format(SQLStrings.sql_get_updates_bybulletin, bulletin, CollectionGuid));
				Altiris.NS.Logging.EventLog.ReportInfo(String.Format("Generating all update graphs for bulletin " + bulletin));
				GeneratePage(u, bulletin, bulletin);
			}

		}

		private void AddToSiteMap(string page, string url) {
			SiteMap.AppendFormat("<li><a href='{1}'>{0}</a></li>\n", page, url);
		}
	}

	class Installer {
		public static int install(string spname) {
			try {
				if (spname == "spTrendPatchComplianceByUpdate" || spname == "ALL") {
					Altiris.NS.Logging.EventLog.ReportInfo("Dropping spTrendPatchComplianceByUpdate...\t");
					DatabaseAPI.ExecuteNonQuery(SQLStrings.sql_drop_spTrendPatchComplianceByUpdate);
					Altiris.NS.Logging.EventLog.ReportInfo("Installing spTrendPatchComplianceByUpdate...\t");
					DatabaseAPI.ExecuteNonQuery(SQLStrings.sql_spTrendPatchComplianceByUpdate);
				}
				if (spname == "spTrendPatchComplianceByComputer" || spname == "ALL") {
					Altiris.NS.Logging.EventLog.ReportInfo("Dropping spTrendPatchComplianceByComputer...\t");
					DatabaseAPI.ExecuteNonQuery(SQLStrings.sql_drop_spTrendPatchComplianceByComputer);
					Altiris.NS.Logging.EventLog.ReportInfo("Installing spTrendPatchComplianceByComputer...\t");
					DatabaseAPI.ExecuteNonQuery(SQLStrings.sql_spTrendPatchComplianceByComputer);
				}
				if (spname == "spTrendInactiveComputers" || spname == "ALL") {
					Altiris.NS.Logging.EventLog.ReportInfo("Dropping spTrendInactiveComputers...\t\t");
					DatabaseAPI.ExecuteNonQuery(SQLStrings.sql_drop_spTrendInactiveComputers);
					Altiris.NS.Logging.EventLog.ReportInfo("Installing spTrendInactiveComputers...\t\t");
					DatabaseAPI.ExecuteNonQuery(SQLStrings.sql_spTrendInactiveComputers);
					Altiris.NS.Logging.EventLog.ReportInfo("All Done!");
				}
			} catch (Exception e){
				Console.WriteLine(e.Message);
				Altiris.NS.Logging.EventLog.ReportError(e.Message);
				return -1;
			}
			return 0;
		}
	}

	class DataCollector {
		private static string CollectionGuid = "01024956-1000-4cdb-b452-7db0cff541b6";
		public static int CollectData() {
			// Get the site configuration file
			if (!File.Exists("SiteConfig.txt")) {
				// If the file does not exist use the default collectionguid
				CollectData(CollectionGuid);

			} else {
				try {
					using (StreamReader reader = new StreamReader("SiteConfig.txt")) {
						while (!reader.EndOfStream) {
							string line = reader.ReadLine();
							if (line.StartsWith("#") || line.Length == 0) {
								continue;
							}
							string [] d = line.Split(',');

							if (d[0] == "1") {
								CollectData(d[1].ToString().Trim());
							}

						}
					}
				} catch {

				}
			}
			return 0;
		}

		private static void CollectData(String CollectionGuid) {
			string msg = String.Format("Running data collection for Collection Guid {0}", CollectionGuid);
			Altiris.NS.Logging.EventLog.ReportInfo(msg);
			try {
				Altiris.NS.Logging.EventLog.ReportInfo(String.Format(SQLStrings.sql_exec_spTrendInactiveComputers, CollectionGuid));
				DatabaseAPI.ExecuteNonQuery(String.Format(SQLStrings.sql_exec_spTrendInactiveComputers, CollectionGuid));
				Altiris.NS.Logging.EventLog.ReportInfo("...collect Inactive Computer data done.");
			} catch (Exception e1) {
				Altiris.NS.Logging.EventLog.ReportError("Failed to collect Inactive Computer data", e1.Message);
			}
			try {
				Altiris.NS.Logging.EventLog.ReportInfo(String.Format(SQLStrings.sql_exec_spTrendPatchComplianceByComputer, CollectionGuid));
				DatabaseAPI.ExecuteNonQuery(String.Format(SQLStrings.sql_exec_spTrendPatchComplianceByComputer, CollectionGuid));
				Altiris.NS.Logging.EventLog.ReportInfo("...collect Compliance by Computer data done.");
			} catch (Exception e2) {
				Altiris.NS.Logging.EventLog.ReportError("Failed to collect Compliance by Computer data", e2.Message);
			}
			try {
				Altiris.NS.Logging.EventLog.ReportInfo(String.Format(SQLStrings.sql_exec_spTrendPatchComplianceByUpdate, CollectionGuid));
				DatabaseAPI.ExecuteNonQuery(String.Format(SQLStrings.sql_exec_spTrendPatchComplianceByUpdate, CollectionGuid));
				Altiris.NS.Logging.EventLog.ReportInfo("...collect Compliance by Update data done.");
			} catch (Exception e3) {
				Altiris.NS.Logging.EventLog.ReportError("Failed to collect Compliance by Update data", e3.Message);
			}
		}
	}

	class Upgrader {
		public static bool NeedUpgrade() {
			foreach (string table in tables) {
				if (NeedUpgrade(table))
					return true;
			}
			return false;
		}

		public static bool NeedUpgrade (string table_name){
			// Check whether we need to upgrade the DB Table
			object o = DatabaseAPI.ExecuteScalar(String.Format(sql_base, table_name));
			if (o.ToString() != "1"){
				Altiris.NS.Logging.EventLog.ReportInfo(String.Format("Table {0} needs to be upgraded.", table_name));
				return true;
			} else {
				Altiris.NS.Logging.EventLog.ReportInfo(String.Format("Table {0} does not need to be upgraded.", table_name));
				return false;
			}
		}

		public static int Upgrade (string collectionguid) {
			if (UpgradeComplianceByComputerTable(collectionguid) != 0)
				goto rollback;
			if (UpgradeComplianceByUpdateTable(collectionguid) != 0)
				goto rollback;
			if (UpgradeInactiveComputerTables(collectionguid) != 0) 
				goto rollback;

			string site_config_file = "siteconfig.txt";
			if (!File.Exists(site_config_file)) {
				string siteconfig = "1, " + collectionguid + ", UpgradedSite, Patch trending site updated automatically, 1";
				Altiris.NS.Logging.EventLog.ReportVerbose(String.Format("Saving siteconfig to file {0}:\n{1}", site_config_file, siteconfig));
				using (StreamWriter outfile = new StreamWriter(site_config_file)) {
					outfile.Write(siteconfig);
				}
			}
			return 0;

rollback:
			Altiris.NS.Logging.EventLog.ReportError("Upgrade failed...");
			return -1;
		}

		public static int UpgradeTable(string table, string install_spname, string sql_procedure, string sql_restore) {
			string sql_backup = String.Format(sql_sprename, table, table + backup_table_suffix);
			Altiris.NS.Logging.EventLog.ReportVerbose(sql_backup);
			try {
				DatabaseAPI.ExecuteNonQuery(sql_backup);
			} catch {
			}
			
			// The previous SQL may return an exception and work properly - let's handle this
			try {
				string sql = String.Format("select 1 from sys.objects where type = 'u' and name = '{0}'", table + backup_table_suffix);
				int result = (int)DatabaseAPI.ExecuteScalar(sql);
				if (result != 1)
					return -1;
			} catch (Exception e) {
				Altiris.NS.Logging.EventLog.ReportError("Failed to sp_rename stored procedure...");
				Altiris.NS.Logging.EventLog.ReportError(e.Message);
				return -1;				
			}

			Altiris.NS.Logging.EventLog.ReportVerbose(install_spname);
			int done = Installer.install(install_spname);
			
			if (done != 0)
				return -1;

			Altiris.NS.Logging.EventLog.ReportVerbose(sql_procedure);
			try {
				DatabaseAPI.ExecuteNonQuery(sql_procedure);
			} catch (Exception e){
				Altiris.NS.Logging.EventLog.ReportError("Failed running SQL upgrade procedure:\n" + sql_procedure );
				Altiris.NS.Logging.EventLog.ReportError(e.Message);
				return -1;
			}

			Altiris.NS.Logging.EventLog.ReportVerbose(sql_restore);
			try {
				DatabaseAPI.ExecuteNonQuery(sql_restore);
			} catch (Exception e){
				Altiris.NS.Logging.EventLog.ReportError("Failed running SQL data restore procedure:\n" + sql_restore );
				Altiris.NS.Logging.EventLog.ReportError(e.Message);
				return -1;
			}

			return 0;
		}

		private static int UpgradeInactiveComputerTables(string collectionguid) {
			string [] tables = { "TREND_InactiveComputerCounts", "TREND_InactiveComputer_Current", "TREND_InactiveComputer_Previous" };

			// All 3 tables have to be backed up at once
			if (NeedUpgrade(tables[0]) && NeedUpgrade(tables[1]) && NeedUpgrade(tables[2])) {	
				foreach (string table in tables) {
					string sql_backup = String.Format(sql_sprename, table, table + backup_table_suffix);
					Altiris.NS.Logging.EventLog.ReportVerbose(sql_backup);
					try {
						DatabaseAPI.ExecuteNonQuery(sql_backup);
					} catch {
					}
					
					// The previous SQL may return an exception and work properly - let's handle this
					try {
						string sql = String.Format("select 1 from sys.objects where type = 'u' and name = '{0}'", table + backup_table_suffix);
						int result = (int)DatabaseAPI.ExecuteScalar(sql);
						if (result != 1)
							return -1;
					} catch (Exception e) {
						Altiris.NS.Logging.EventLog.ReportError("Failed to sp_rename stored procedure...");
						Altiris.NS.Logging.EventLog.ReportError(e.Message);
						return -1;				
					}
				}
				int sp_install = Installer.install("spTrendInactiveComputers");
				if (sp_install != 0)
					return sp_install;
				
				try {
				DatabaseAPI.ExecuteNonQuery(sql_exec_inactivecomputers);
				} catch (Exception e) {
						Altiris.NS.Logging.EventLog.ReportError("Failed to run spTrendInactiveComputers...");
						Altiris.NS.Logging.EventLog.ReportError(e.Message);

				}
				
				string sql_restore = String.Format(sql_restore_inactivecount, collectionguid, "TREND_InactiveComputerCounts" + backup_table_suffix);

				Altiris.NS.Logging.EventLog.ReportVerbose(sql_restore);
				try {
					DatabaseAPI.ExecuteNonQuery(sql_restore);
				} catch (Exception e){
					Altiris.NS.Logging.EventLog.ReportError("Failed running SQL data restore procedure:\n" + sql_restore );
					Altiris.NS.Logging.EventLog.ReportError(e.Message);
					return -1;
				}
				sql_restore = String.Format(sql_restore_inactiveprevious, collectionguid, "TREND_InactiveComputer_previous" + backup_table_suffix);
				Altiris.NS.Logging.EventLog.ReportVerbose(sql_restore);
				try {
					DatabaseAPI.ExecuteNonQuery(sql_restore);
				} catch (Exception e){
					Altiris.NS.Logging.EventLog.ReportError("Failed running SQL data restore procedure:\n" + sql_restore );
					Altiris.NS.Logging.EventLog.ReportError(e.Message);
					return -1;
				}
				sql_restore = String.Format(sql_restore_inactivecurrent, collectionguid, "TREND_InactiveComputer_current" + backup_table_suffix);
				Altiris.NS.Logging.EventLog.ReportVerbose(sql_restore);
				try {
					DatabaseAPI.ExecuteNonQuery(sql_restore);
				} catch (Exception e){
					Altiris.NS.Logging.EventLog.ReportError("Failed running SQL data restore procedure:\n" + sql_restore );
					Altiris.NS.Logging.EventLog.ReportError(e.Message);
					return -1;
				}

			}
			return 0;
		}

		private static int UpgradeComplianceByComputerTable(string collectionguid) {
			string table = "TREND_WindowsCompliance_ByComputer";
			if (NeedUpgrade(table)) {
				string sql_restore = String.Format(sql_restore_computercompliance, collectionguid, table + backup_table_suffix);
				return UpgradeTable(table, "spTrendPatchComplianceByComputer", sql_exec_computercompliance, sql_restore);
			}
			return 0;
		}

		private static int UpgradeComplianceByUpdateTable(string collectionguid){
			string table = "TREND_WindowsCompliance_ByUpdate";
			if (NeedUpgrade(table)) {
				string sql_restore = String.Format(sql_restore_updatecompliance, collectionguid, table + backup_table_suffix);
				return UpgradeTable(table, "spTrendPatchComplianceByUpdate", sql_exec_updatecompliance, sql_restore);
			}
			return 0;
		}

		#region SQL queries
		private static readonly string backup_table_suffix = "_old";
		private static string sql_base = @"
	select 1
	  from sys.objects so
	  join sys.columns sc
		on so.object_id = sc.object_id
	 where so.name = '{0}'
	   and sc.name = 'CollectionGuid'
		";

		private static string sql_sprename = "exec sp_rename @objname='{0}', @newname='{1}'";

		private static string sql_restore_inactivecurrent = @"insert TREND_InactiveComputer_current (guid, collectionguid, _exec_time) select guid, '{0}' as CollectionGuid, [_exec_time] from {1}";
		private static string sql_restore_inactiveprevious = @"insert TREND_InactiveComputer_previous (guid, collectionguid, _exec_time) select guid, '{0}' as CollectionGuid, [_exec_time] from {1}";
		private static string sql_restore_inactivecount = @"
insert TREND_InactiveComputerCounts ([_exec_id], [timestamp], [collectionguid], [Managed machines], [Inactive computers (7 days)], [New Inactive computers], [New Active computers], [Inactive computers (17 days)])
select [_exec_id], [timestamp], '{0}' as CollectionGuid, [Managed machines], [Inactive computers (7 days)], [New Inactive computers], [New Active computers], [Inactive computers (17 days)] from {1}";
		private static string sql_restore_computercompliance =@"
insert TREND_WindowsCompliance_ByComputer ([_Exec_id], [CollectionGuid], [_Exec_time], [Percent], [Computer #], [% of Total])
select [_Exec_id], '{0}' as 'CollectionGuid', [_Exec_time], [Percent], [Computer #], [% of Total] from {1}";
		private static string sql_restore_updatecompliance = @"
insert TREND_WindowsCompliance_ByUpdate ([_Exec_id], [CollectionGuid], [_Exec_time], [Bulletin], [UPDATE], [Severity], [Installed], [Applicable], [DistributionStatus])
select [_Exec_id], '{0}' as 'CollectionGuid', [_Exec_time], [Bulletin], [UPDATE], [Severity], [Installed], [Applicable], [DistributionStatus] from {1}";

		private static string sql_exec_inactivecomputers = "exec spTrendInactiveComputers @CollectionGuid='6410074B-FFFF-FFFF-FFFF-0C8803328385'";
		private static string sql_exec_computercompliance = "exec spTrendPatchComplianceByComputer @CollectionGuid='6410074B-FFFF-FFFF-FFFF-0C8803328385'";
		private static string sql_exec_updatecompliance = "exec spTrendPatchComplianceByUpdate @CollectionGuid = '6410074B-FFFF-FFFF-FFFF-0C8803328385'";
		#endregion

		private static string [] tables = new string [] {
			  "TREND_InactiveComputerCounts"
			, "TREND_InactiveComputer_Current"
			, "TREND_InactiveComputer_Previous"
			, "TREND_WindowsCompliance_ByComputer"
			, "TREND_WindowsCompliance_ByUpdate"
		};
	}
}
