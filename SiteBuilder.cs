using Altiris.NS.Logging;
using Altiris.NS.ContextManagement;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Sql;
using System.IO;
using System.Text;
using Symantec.CWoC.APIWrappers;

namespace Symantec.CWoC.PatchTrending {
    class SiteGenerator {

        static int Main(string[] args) {
            string filename = "site-layout.txt";
            if (args.Length == 1) {
                filename = args[0];
                EventLog.ReportInfo("The custom site-layout file " + filename + " will be used.");
            }

            SiteBuilder builder = new SiteBuilder();
            return builder.Build(filename);
        }
    }

    class SiteBuilder {

        public string version = "version 13";
        private StringBuilder SiteMap;

        public SiteBuilder() {
            Timer.Init();
            Counters.Init();

            Altiris.NS.Logging.EventLog.ReportInfo("SiteBuilder is starting.");
            SiteMap = new StringBuilder();

            // Make sure we have the required sub-folder for javascript files
            if (!Directory.Exists("javascript")) {
                Directory.CreateDirectory("javascript");
            }
        }

        public int Build(string filename) {
            string line, pagename;
            StringBuilder filter = new StringBuilder();
            StringBuilder index = new StringBuilder();
            string[] d;


            /* Check that we can run against the database (I.e. prerequisite table does exist) */
            bool compliance_by_update = TestSql("select top 1 1 from TREND_WindowsCompliance_ByUpdate");
            bool compliance_by_computer = TestSql("select 1 from TREND_WindowsCompliance_ByComputer t group by t._Exec_id having MAX(_exec_id) > 1");
            bool inactive_computer_trend = TestSql("select top 1 1 from TREND_InactiveComputerCounts");

            AddToSiteMap("Global compliance", "getbulletin.html?global");
            SaveToFile("menu.css", StaticStrings.css_navigation);
            SaveToFile("help.html", StaticStrings.html_help);
            Counters.HtmlPages += 2;

            if (compliance_by_update) {

                SaveToFile("javascript\\helper.js", StaticStrings.js_Helper);
                ++Counters.JsPages;

                SaveToFile("getbulletin.html", StaticStrings.html_GetBulletin_page);
                ++Counters.HtmlPages;

                EventLog.ReportInfo("Generating Top 10 bulletins by vulnerable computers page...");
                GeneratePage("top10-vulnerable", StaticStrings.sql_get_topn_vulnerable);
                AddToIndex(ref index, "top10-vulnerable");

                EventLog.ReportInfo("Generating Top 25 update by vulnerable computers page...");
                GeneratePage("top25-vulnerable-upd", StaticStrings.sql_get_topn_vulnerable_upd);
                AddToIndex(ref index, "top25-vulnerable-upd");


                EventLog.ReportInfo("Generating Top 10 movers (++) page...");
                GeneratePage("top10-movers-up", StaticStrings.sql_get_topn_movers_up);
                AddToIndex(ref index, "top10-movers-up");

                EventLog.ReportInfo("Generating Top 10 movers (--) page...");
                GeneratePage("top10-movers-down", StaticStrings.sql_get_topn_movers_down);
                AddToIndex(ref index, "top10-movers-down");

                EventLog.ReportInfo("Generating Bottom 10 bulletins by compliance...");
                GeneratePage("bottom-10-compliance", StaticStrings.sql_get_bottomn_compliance_upd);
                AddToIndex(ref index, "bottom-10-compliance");

                EventLog.ReportInfo("Generating Bottom 25 updates by compliance...");
                GeneratePage("bottom-25-compliance-upd", StaticStrings.sql_get_bottomn_compliance);
                AddToIndex(ref index, "bottom-25-compliance-upd");


                if (inactive_computer_trend) {
                    EventLog.ReportInfo("Generating Inactive-computers page...");
                    GenerateInactiveComputerJs();
                    SaveToFile("inactive-computers.html", StaticStrings.html_GetInactiveComputers_page);
                    ++Counters.HtmlPages;
                    AddToIndex(ref index, "inactive-computers");
                    AddToSiteMap("inactive-computers", "inactive-computers.html");
                }

                if (compliance_by_computer) {
                    EventLog.ReportInfo("Generating Compliance-by-computer page...");
                    SaveToFile("compliance-by-computer.html", StaticStrings.html_ComputerCompliance_page);
                    ++Counters.HtmlPages;
                    AddToIndex(ref index, "compliance-by-computer");
                    AddToSiteMap("compliance-by-computer", "compliance-by-computer.html");
                }

                EventLog.ReportInfo("Generating site pages from the layout file...");
                try {
                    using (StreamReader reader = new StreamReader(filename)) {
                        while (!reader.EndOfStream) {
                            filter = new StringBuilder();
                            line = reader.ReadLine();
                            d = line.Split(',');

                            pagename = d[0];
                            Console.WriteLine(pagename.ToUpper());
                            if (d.Length > 1) {
                                filter.Append("'" + d[1].Trim() + "'");
                            }
                            for (int i = 2; i < d.Length; i++) {
                                filter.Append(", '" + d[i].Trim() + "'");
                            }
                            int j = GeneratePage(pagename, filter.ToString(), StaticStrings.sql_get_bulletins_in);
                            if (j > 0)
                                AddToIndex(ref index, pagename);
                        }
                    }

                    GenerateIndex(ref index, compliance_by_computer, inactive_computer_trend);
                    GenerateGlobalPage();
                    Console.WriteLine("Generating updates pages...");
                    GenerateUpdatePages();
                } catch (Exception e) {
                    Console.WriteLine(e.Message + "\n" + e.StackTrace);
                    Console.ReadLine();
                }

                SaveToFile("sitemap.html", SiteMap.ToString());
                ++Counters.HtmlPages;

                Timer.Stop();
                string msg = string.Format("SiteBuilder took {0} ms to generate {1} pages ({2} html and {3} javascript) with {4} sql queries executed.", Timer.duration(), Counters.Pages, Counters.HtmlPages, Counters.JsPages, Counters.SqlQueries);
                Altiris.NS.Logging.EventLog.ReportInfo(msg);
            } else {
                Console.WriteLine("We cannot execute anything as the prerequisite table TREND_WindowsCompliance_ByUpdate is missing.");
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
            } else if (byComputer == false && inactive == false) {
                // Nothing to add in this case :D.
            } else {
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
            string data = "";
            string data_full = "";

            if (hasData) {
                int bottom = 74;
                if (smaller)
                    bottom = 84;

                DataTable t = DatabaseAPI.GetTable(StaticStrings.sql_get_compliance_bypccount);
                StringBuilder b = new StringBuilder();
                StringBuilder c = new StringBuilder();

                b.AppendLine("var " + GetJSString("pccompl") + " = [");
                c.AppendLine("var " + GetJSString("pccompl_full") + " = [");

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
            } else {                
                data = "var pccompl = []";
            }

            SaveToFile("javascript\\pccompl.js", data);
            SaveToFile("javascript\\pccompl_full.js", data_full);
            Counters.JsPages += 2;
        }

        private void SaveToFile(string filepath, string data) {
            using (StreamWriter outfile = new StreamWriter(filepath.ToLower())) {
                outfile.Write(data);
            }
        }

        private string GetJSString(string s) {
            s = s.Replace('-', '_');
            s = s.Replace('.', '_');
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
                t = DatabaseAPI.GetTable(string.Format(sql, filter));
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
            jsInclude.AppendLine("<link rel='stylesheet' type='text/css' href='menu.css'>");

            bool isBulletin, isUpdate;

            if (bulletin == "") {
                isBulletin = true;
            } else {
                isBulletin = false;
            }

            if (pagename.EndsWith("-upd")) {
                isUpdate = true;
            } else {
                isUpdate = false;
            }

            string entry = "";
            string curr_bulletin = "";
            string curr_graph = "";
            string curr_data = "";
            string curr_div = "";

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

            Console.WriteLine("\tGenerating graphing javascript for {0}...", entry);

            drawChart.AppendLine("}");
            SaveToFile("javascript\\" + pagename + ".js", drawChart.ToString());
            Counters.JsPages++;

            Console.WriteLine("\tGenerating html page for {0}...", entry);
            GenerateBulletinHtml(ref htmlDivs, ref jsInclude, pagename);
            Counters.HtmlPages++;

            SiteMap.Append("</ul>");
            return t.Rows.Count;
        }

        private void GenerateGlobalPage() {
            Counters.HtmlPages++;
            SaveToFile("javascript\\global.js", StaticStrings.js_GlobalCompliance);
            string globalcompliance = "";
            string globalstats = "";
            GetGlobalData(ref globalcompliance, ref globalstats);
            SaveToFile("javascript\\global_0.js", globalcompliance);
            SaveToFile("javascript\\global_1.js", globalstats);
            Counters.JsPages += 3;
        }

        private void GenerateBulletinHtml(ref StringBuilder divs, ref StringBuilder jsfiles, string pagename) {
            string html = String.Format(FormattedStrings.html_BulletinPage, pagename, divs.ToString(), jsfiles.ToString(), pagename.ToLower());
            SaveToFile(pagename + ".html", html);
        }

        private void GetUpdateData(ref string compliance, ref string stats, string update, string bulletin) {
            if (update.Length == 0 || update == string.Empty)
                return;

            string sql = String.Format(FormattedStrings.sql_get_update_data, update, bulletin);
            DataTable t = DatabaseAPI.GetTable(sql);
            stats = GetJSONFromTable(t, update);

            string[,] _compliance = GetComplianceFromTable(t);
            compliance = GetComplianceJSONFromArray(_compliance, update);

        }

        private void GetBulletinData(ref string compliance, ref string data, string bulletin) {
            string sql = String.Format(FormattedStrings.sql_get_bulletin_data, bulletin);
            DataTable t = DatabaseAPI.GetTable(sql);
            data = GetJSONFromTable(t, bulletin);

            string[,] _compliance = GetComplianceFromTable(t);
            compliance = GetComplianceJSONFromArray(_compliance, bulletin);
        }

        private void GetGlobalData(ref string compliance, ref string data) {
            DataTable t = DatabaseAPI.GetTable(StaticStrings.sql_get_global_compliance_data);
            data = GetJSONFromTable(t, "global");

            string[,] _compliance = GetComplianceFromTable(t);
            compliance = GetComplianceJSONFromArray(_compliance, "global");
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
            DataTable t = DatabaseAPI.GetTable(StaticStrings.sql_get_inactive_computer_trend);
            SaveToFile("Javascript\\inactive_computers.js", GetInactiveComputer_JSONFromTable(t, "inactive_computers"));

            t = DatabaseAPI.GetTable(StaticStrings.sql_get_inactive_computer_percent);
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
                    Single inst = Convert.ToSingle(r[1]);
                    Single appl = Convert.ToSingle(r[2]);
                    Single compliance = (inst / appl) * 100;

                    d[i, 1] = compliance.ToString();
                } else {
                    d[i, 1] = "0";
                }
                i++;
            }

            return d;
        }

        private string GetPcComplianceSummary() {
            string s = "";
            DataTable t = DatabaseAPI.GetTable(StaticStrings.sql_get_compliance_bypc_bottom75percent);
            if (t.Rows.Count > 0) {
                s = string.Format("There are <i><b>{0}</b> computers, <b>{1}</b>% of the total</i> reporting compliance level below 75%.", t.Rows[0][0], t.Rows[0][1]);
            }
            return s;
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
            EventLog.ReportInfo("Generating update pages for bulletins now...");
            DataTable t = DatabaseAPI.GetTable(StaticStrings.sql_get_all_bulletins);

            string bulletin;

            foreach (DataRow r in t.Rows) {
                bulletin = r[0].ToString();
                DataTable u = DatabaseAPI.GetTable(string.Format(StaticStrings.sql_get_updates_bybulletin, bulletin));
                Console.WriteLine("Generating all update graphs for bulletin " + bulletin);
                GeneratePage(u, bulletin, bulletin);
            }

        }

        private void AddToSiteMap(string page, string url) {
            SiteMap.AppendFormat("<li><a href='{1}'>{0}</a></li>\n", page, url);
        }
    }
}
