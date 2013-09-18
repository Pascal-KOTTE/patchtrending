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

        public static string version = "version 11";

        static int Main(string[] args) {
            Timer.Init();
            Counters.Init();

            string filename = "site-layout.txt";
            if (args.Length == 1) {
                filename = args[0];
                EventLog.ReportInfo("The custom site-layout file " + filename + " will be used.");
            }

            string line, pagename;
            StringBuilder filter = new StringBuilder();
            StringBuilder index = new StringBuilder();
            string [] d;

            if (!Directory.Exists("javascript")) {
                Directory.CreateDirectory("javascript");
            }

            /* Check that we can run against the database (I.e. prerequisite table does exist) */
            bool compliance_by_update = false;
            bool compliance_by_computer = false;
            bool inactive_computer_trend = false;

            try {
                string sql = "select top 1 1 from TREND_WindowsCompliance_ByUpdate";
                if (DatabaseAPI.ExecuteScalar(sql) == 1) {
                    compliance_by_update = true;
                }
            } catch {
            }

            try {
                string sql = "select 1 from TREND_WindowsCompliance_ByComputer t group by t._Exec_id having MAX(_exec_id) > 1";
                if (DatabaseAPI.ExecuteScalar(sql) == 1) {
                    compliance_by_computer = true;
                }
            } catch {
            }

            try {
                string sql = "select top 1 1 from TREND_InactiveComputerCounts";
                if (DatabaseAPI.ExecuteScalar(sql) == 1) {
                    inactive_computer_trend = true;
                }
            } catch {
            }


            if (compliance_by_update) {

                SaveToFile("javascript\\helper.js", StaticStrings.js_Helper);
                ++Counters.JsPages;
                SaveToFile("getbulletin.html", StaticStrings.html_GetBulletin_page);
                ++Counters.HtmlPages;

                EventLog.ReportInfo("Generating Top 10 bulletins by vulnerable computers page...");
                GeneratePage("top10-vulnerable", StaticStrings.sql_get_top10_vulnerable);
                AddToIndex(ref index, "top10-vulnerable");

                EventLog.ReportInfo("Generating Top 10 movers (++) page...");
                GeneratePage("top10-movers-up", StaticStrings.sql_get_top10movers_up);
                AddToIndex(ref index, "top10-movers-up");

                EventLog.ReportInfo("Generating Top 10 movers (--) page...");
                GeneratePage("top10-movers-down", StaticStrings.sql_get_top10movers_down);
                AddToIndex(ref index, "top10-movers-down");

                EventLog.ReportInfo("Generating Bottom 10 bulletins by compliance...");
                GeneratePage("bottom-10-compliance", StaticStrings.sql_get_bottom10_compliance);
                AddToIndex(ref index, "bottom-10-compliance");

                if (inactive_computer_trend) {
                    EventLog.ReportInfo("Generating Inactive-computers page...");
                    SaveToFile("inactive-computers.html", StaticStrings.html_GetInactiveComputers_page);
                    ++Counters.HtmlPages;
                    AddToIndex(ref index, "inactive-computers");
                    GenerateInactiveComputerJs();
                }

                if (compliance_by_computer) {
                    EventLog.ReportInfo("Generating Compliance-by-computer page...");
                    SaveToFile("compliance-by-computer.html", StaticStrings.html_ComputerCompliance_page);
                    ++Counters.HtmlPages;
                    AddToIndex(ref index, "compliance-by-computer");
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

                Timer.Stop();
                string msg = string.Format("SiteBuilder completed in {0} ms, taking {1} ticks to generate {2} pages ({3} " + "html and {4} javascript) with {5} sql queries executed.", Timer.duration(), Timer.tickCount(),
                                                Counters.Pages, Counters.HtmlPages, Counters.JsPages, Counters.SqlQueries);
                Altiris.NS.Logging.EventLog.ReportInfo(msg);
            } else {
                Console.WriteLine("We cannot execute anything as the prerequisite table TREND_WindowsCompliance_ByUpdate is missing.");
            }

            return 0;
        }

        public static void AddToIndex(ref StringBuilder b, string s) {
            b.Append("<li><a href=\"" + s + ".html\">" + s + "</a></li>");
        }

        public static void GenerateIndex(ref StringBuilder b, bool byComputer, bool inactive) {
            StringBuilder p = new StringBuilder();

            p.Append(StaticStrings.html_Landing);
            GeneratePcComplPages(byComputer, inactive);
            if (byComputer == true && inactive ==false) {
                p.Append(StaticStrings.html_PcCompl_div);
            } else if (byComputer == false && inactive == true) {
                p.Append(StaticStrings.html_PcInactive_div);
            } else if (byComputer == false && inactive == false) {
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

        public static void GenerateUpdatePages() {
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

        public static int GeneratePage(string pagename, string sql) {
            return GeneratePage(pagename, "", sql);
        }

        public static int GeneratePage(string pagename, string filter, string sql) {

            DataTable t;
            if (filter != "")
                t = DatabaseAPI.GetTable(string.Format(sql, filter));
            else
                t = DatabaseAPI.GetTable(sql);

            if (t.Rows.Count == 0)
                return 0;

            return GeneratePage(t, pagename, "");

        }

        public static int GeneratePage(DataTable t, string pagename, string bulletin) {

            StringBuilder drawChart = new StringBuilder();
            StringBuilder htmlDivs = new StringBuilder();
            StringBuilder jsInclude = new StringBuilder();

            StringBuilder bList = new StringBuilder();


            drawChart.AppendLine("function drawChart() {");
            drawChart.AppendLine("\tvar options1 = { title: '', vAxis: { maxValue : 100, minValue : 0 }};\n");
            drawChart.AppendLine("\tvar options2 = { title: '', vAxis: { minValue : 0 }};\n");

            bool isBulletin;

            if (bulletin == "")
                isBulletin = true;
            else
                isBulletin = false;

            string entry = "";
            string curr_bul = "";
            string curr_gra = "";
            string curr_dat = "";
            string curr_div = "";

            foreach (DataRow r in t.Rows) {

                entry = r[0].ToString();
                curr_bul = GetJSString(entry);
                curr_dat = "d_" + curr_bul;
                curr_gra = "g_" + curr_bul;
                curr_div = curr_bul + "_div";

                jsInclude.AppendFormat("\t<script type=\"text/javascript\" src=\"javascript/{0}_0.js\"></script>\n", curr_bul);
                jsInclude.AppendFormat("\t<script type=\"text/javascript\" src=\"javascript/{0}_1.js\"></script>\n", curr_bul);

                // Generate the compliance % graph js
                drawChart.AppendFormat("\t\tvar {0}_0 = google.visualization.arrayToDataTable(formatDateString({1}_0, 0));\n", curr_dat, curr_bul);
                drawChart.AppendFormat("\t\tvar {0}_0 = new google.visualization.LineChart(document.getElementById('{1}_0'));\n", curr_gra, curr_div);
                drawChart.AppendFormat("\t\t{0}_0.draw({1}_0, options1);\n", curr_gra, curr_dat);

                // Generate the inst / appl graph js
                drawChart.AppendFormat("\t\tvar {0}_1 = google.visualization.arrayToDataTable(formatDateString({1}_1, 0));\n", curr_dat, curr_bul);
                drawChart.AppendFormat("\t\tvar {0}_1 = new google.visualization.LineChart(document.getElementById('{1}_1'));\n", curr_gra, curr_div);
                drawChart.AppendFormat("\t\t{0}_1.draw({1}_1, options2);\n", curr_gra, curr_dat);

                // Generate the divs
                if (isBulletin) {
                    htmlDivs.AppendFormat("<h3><a href='{0}.html'>{0}</a></h3>\n", entry);
                } else {
                    htmlDivs.AppendFormat("<h3>{0}</h3>\n", entry);
                }

                htmlDivs.AppendLine("<table width '80%'>");
                htmlDivs.AppendLine("<tr><td>Installed versus Applicable</td><td>Compliance status in %</td></tr><tr>");
                htmlDivs.AppendFormat("<td><div id='{0}_1' style='width: 500px; height: 200px;'></div></td>\n", curr_div);
                htmlDivs.AppendFormat("<td><div id='{0}_0' style='width: 500px; height: 200px;'></div></td>\n", curr_div);
                htmlDivs.AppendLine("</tr></table>");

                if (isBulletin) {
                    string bulletin_compliance = "";
                    string bulletin_stats = "";
                    GetBulletinData(ref bulletin_compliance, ref bulletin_stats, entry);
                    SaveToFile("javascript\\" + curr_bul + "_0.js", bulletin_compliance);
                    SaveToFile("javascript\\" + curr_bul + "_1.js", bulletin_stats);
                } else {
                    string update_compliance = "";
                    string update_stats = "";
                    GetUpdateData(ref update_compliance, ref update_stats, entry, bulletin);
                    SaveToFile("javascript\\" + curr_bul + "_0.js", update_compliance);
                    SaveToFile("javascript\\" + curr_bul + "_1.js", update_stats);
                }
                Counters.JsPages += 2;
            }

            Console.WriteLine("\tGenerating graphing javascript for {0}...", entry);

            drawChart.AppendLine("}");
            SaveToFile("javascript\\" + pagename + ".js", drawChart.ToString());
            Counters.JsPages ++;

            Console.WriteLine("\tGenerating html page for {0}...", entry);
            GenerateBulletinHtml(ref htmlDivs, ref jsInclude, pagename);
            Counters.HtmlPages++;

            return t.Rows.Count;
        }

        public static void GenerateGlobalPage() {
            Counters.HtmlPages++;
            SaveToFile("javascript\\global.js", StaticStrings.js_GlobalCompliance);
            string globalcompliance = "";
            string globalstats = "";
            GetGlobalData(ref globalcompliance, ref globalstats);
            SaveToFile("javascript\\global_0.js", globalcompliance);
            SaveToFile("javascript\\global_1.js", globalstats);
            Counters.JsPages += 3;
        }

        public static void GeneratePcComplPages(bool hasData, bool smaller) {
            string data = "";
            string data_full = "";

            if (!hasData) {
                data = "var pccompl = []";
            } else {

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
            }

            SaveToFile("javascript\\pccompl.js", data);
            SaveToFile("javascript\\pccompl_full.js", data_full);
            Counters.JsPages += 2;
        }

        public static void GenerateBulletinHtml(ref StringBuilder divs, ref StringBuilder jsfiles, string pagename){

            string html = String.Format(FormattedStrings.html_BulletinPage, pagename, divs.ToString(), jsfiles.ToString());
            SaveToFile(pagename + ".html", html);
        }

        public static void GetUpdateData(ref string compliance, ref string stats, string update, string bulletin) {
            if (update.Length == 0 || update == string.Empty)
                return;

            string sql = String.Format(FormattedStrings.sql_get_update_data, update, bulletin);
            DataTable t = DatabaseAPI.GetTable(sql);
            stats = GetJSONFromTable(t, update);

            string[,] _compliance = GetComplianceFromTable(t);
            compliance = GetComplianceJSONFromArray(_compliance, update);

        }

        private static void GetBulletinData(ref string compliance, ref string data, string bulletin) {
            string sql = String.Format(FormattedStrings.sql_get_bulletin_data, bulletin);
            DataTable t = DatabaseAPI.GetTable(sql);
            data = GetJSONFromTable(t, bulletin);

            string[,] _compliance = GetComplianceFromTable(t);
            compliance = GetComplianceJSONFromArray(_compliance, bulletin);
        }

        private static void GetGlobalData(ref string compliance, ref string data) {
            DataTable t = DatabaseAPI.GetTable(StaticStrings.sql_get_global_compliance_data);
            data = GetJSONFromTable(t, "GLOBAL");

            string[,] _compliance = GetComplianceFromTable(t);
            compliance = GetComplianceJSONFromArray(_compliance, "GLOBAL");
        }

        private static string GetComplianceJSONFromArray(string[,] t, string bulletin) {
            StringBuilder b = new StringBuilder();

            b.AppendLine("var " + GetJSString(bulletin) + "_0 = [");
            b.AppendLine("['Date', 'Compliance %'],");
            for (int i = 0; i < t.Length / 2 ; i++) {
               b.AppendLine("['" + t[i, 0] + "', " + t[i, 1].Replace(',', '.') + "],");
            }
            b.Remove(b.Length - 3, 1);
            b.AppendLine("]");

            return b.ToString();
        }

        private static void GenerateInactiveComputerJs() {
            DataTable t = DatabaseAPI.GetTable(StaticStrings.sql_get_inactive_computer_trend);
            SaveToFile("Javascript\\inactive_computers.js", GetInactiveComputer_JSONFromTable(t, "inactive_computers"));

            t = DatabaseAPI.GetTable(StaticStrings.sql_get_inactive_computer_percent);
            SaveToFile("Javascript\\inactive_computers_pc.js", GetInactiveComputer_JSONFromTable(t, "inactive_computers_pc"));
            ++Counters.JsPages;
        }

        private static string[,] GetComplianceFromTable(DataTable t) {
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

        private static string GetPcComplianceSummary() {
            string s = "";
            DataTable t = DatabaseAPI.GetTable(StaticStrings.sql_get_compliance_bypc_bottom75percent);
            if (t.Rows.Count > 0) {
                s = string.Format("There are <i><b>{0}</b> computers, <b>{1}</b>% of the total</i> reporting compliance level below 75%.", t.Rows[0][0], t.Rows[0][1]);
            }
            return s;
        }

        private static void SaveToFile(string filepath, string data) {
            using (StreamWriter outfile = new StreamWriter(filepath)) {
                outfile.Write(data);
            }
        }

        private static string GetJSONFromTable(DataTable t, string entry) {


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

        private static string GetInactiveComputer_JSONFromTable(DataTable t, string entry) {

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

        public static string GetJSString(string s) {
            s = s.Replace('-','_');
            s = s.Replace('.', '_');

            return s;
        }
    }
}
