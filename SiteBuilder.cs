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

        public static string version = "v9";

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
            try {
                DataTable r = DatabaseAPI.GetTable("select top 1 1 from TREND_WindowsCompliance_ByUpdate");
                if (r.Rows[0][0].ToString() == "1") {
                    compliance_by_update = true;
                }
            } catch {
            }

            try {
                DataTable r = DatabaseAPI.GetTable("select 1 from TREND_WindowsCompliance_ByComputer t group by t._Exec_id having MAX(_exec_id) > 1");
                if (r.Rows[0][0].ToString() == "1") {
                    compliance_by_computer = true;
                }
            } catch {
            }


            if (compliance_by_update) {
                EventLog.ReportInfo("Adding top-10-vulnerable page to the index builder.");
                AddToIndex(ref index, "top10-vulnerable");
                EventLog.ReportInfo("Adding bottom-10-compliance page to the index builder.");
                AddToIndex(ref index, "bottom-10-compliance");
                EventLog.ReportInfo("Generating site pages from the layout...");
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
                            int j = GeneratePage(pagename, filter.ToString(), StaticStrings.sqlGetBulletinsIn);
                            if (j > 0)
                                AddToIndex(ref index, pagename);
                        }
                    }
                    Console.WriteLine("Generating Top 10 bulletins by vulnerable computers page...");
                    GeneratePage("top10-vulnerable", StaticStrings.sqlGetTop10Vulnerable);

                    Console.WriteLine("Generating Bottom 10 bulletins by compliance...");
                    GeneratePage("bottom-10-compliance", StaticStrings.sqlGetBottom10Compliance);

                    GenerateIndex(ref index, compliance_by_computer);
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
            }

            return 0;
        }

        public static void AddToIndex(ref StringBuilder b, string s) {
            b.Append("<li><a href=\"" + s + ".html\">" + s + "</a></li>");
        }

        public static void GenerateIndex(ref StringBuilder b, bool byComputer) {
            StringBuilder p = new StringBuilder();
            p.Append(StaticStrings.LandingHtml);
            GeneratePcComplPages(byComputer);
            if (byComputer) {
                p.Append(StaticStrings.PcComplHtml);
                // Had summary data for bottom 75% here
                p.Append(GetPcComplianceSummary());
            }
            p.Append(StaticStrings.BulletinSearch);
            // Add compliance by computer graphs here
            p.AppendLine("<h2 style=\"text-align: center; width:80%\">Custom compliance views</h2>");
            if (b.Length > 0) {
                p.AppendLine("<div class=\"wrapper\"><ul>");
                p.Append(b.ToString());
                p.AppendLine("</ul><br/></div>");
            }
            p.AppendLine(@"<p>This site is generated from a customizable site layout file. It includes graphs (installed
                versus applicable and compliance in %) for bulletins that are active. It does not take into account the 
                targetted computers, so you could have very low compliance level whilst the bulletin is not targetting 
                the complete environment.</p>");
            p.AppendLine(@"<h4 style=""text-align: center"">Generated by PatchTrendingSiteBuilder " + version + " on " + DateTime.Now.ToString() + "</h4>");
            p.AppendLine("</body></html>");
            SaveToFile("default.html", p.ToString());
            SaveToFile("default.htm", p.ToString());
            SaveToFile("getbulletin.html", StaticStrings.GetBulletinHtml);
            Counters.HtmlPages += 3;
        }

        public static void GenerateUpdatePages() {
            EventLog.ReportInfo("Generating update pages for bulletins now...");
            DataTable t = DatabaseAPI.GetTable(StaticStrings.sqlGetAllBulletins);

            string bulletin;

            foreach (DataRow r in t.Rows) {
                bulletin = r[0].ToString();
                DataTable u = DatabaseAPI.GetTable(string.Format(StaticStrings.sqlGetUpdatesByBulletin, bulletin));
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

                jsInclude.AppendLine("\t<script type=\"text/javascript\" src=\"javascript/" + curr_bul +
                                                                                        "_0.js\"></script>");
                jsInclude.AppendLine("\t<script type=\"text/javascript\" src=\"javascript/" + curr_bul +
                                                                                        "_1.js\"></script>");

                // Generate the compliance % graph js
                drawChart.AppendLine("\t\tvar " + curr_dat + "_0 = google.visualization.arrayToDataTable("
                                                                                            + curr_bul + "_0);");
                drawChart.AppendLine("\t\tvar " + curr_gra +
                        "_0 = new google.visualization.LineChart(document.getElementById('" + curr_div + "_0'));");
                drawChart.AppendLine("\t\t" + curr_gra + "_0.draw(" + curr_dat + "_0, options1);\n");

                // Generate the inst / appl graph js
                drawChart.AppendLine("\t\tvar " + curr_dat + "_1 = google.visualization.arrayToDataTable(" 
                                                                                            + curr_bul + "_1);");
                drawChart.AppendLine("\t\tvar " + curr_gra +
                        "_1 = new google.visualization.LineChart(document.getElementById('" + curr_div + "_1'));");
                drawChart.AppendLine("\t\t" + curr_gra + "_1.draw(" + curr_dat + "_1, options2);\n");

                // Generate the divs
                if (isBulletin) {
                    htmlDivs.AppendLine("<h3><a href=\"" + entry + ".html\">" + entry + "</a></h3>");
                } else {
                    htmlDivs.AppendLine("<h3>" + entry + "</h3>");
                }

                htmlDivs.AppendLine("<table width '80%'>");
                htmlDivs.AppendLine("<tr><td>Installed versus Applicable</td>"
                                                + "<td>Compliance status in %</td></tr><tr>");
                htmlDivs.AppendLine("<td><div id='" + curr_div + "_1"
                                            + "' style='width: 500px; height: 200px;'></div></td>");
                htmlDivs.AppendLine("<td><div id='" + curr_div + "_0"
                                            + "' style='width: 500px; height: 200px;'></div></td>");
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

            Console.WriteLine("\tGenerating graphing javascript for " + entry + "...");

            drawChart.AppendLine("}");
            SaveToFile("javascript\\" + pagename + ".js", drawChart.ToString());
            Counters.JsPages ++;

            Console.WriteLine("\tGenerating html page for " + entry + "...");
            GenerateBulletinHtml(ref htmlDivs, ref jsInclude, pagename);
            Counters.HtmlPages++;

            return t.Rows.Count;
        }

        public static void GenerateGlobalPage() {
            Counters.HtmlPages++;
            SaveToFile("javascript\\global.js", StaticStrings.GlobalComplianceJavascript);
            string globalcompliance = "";
            string globalstats = "";
            GetGlobalData(ref globalcompliance, ref globalstats);
            SaveToFile("javascript\\global_0.js", globalcompliance);
            SaveToFile("javascript\\global_1.js", globalstats);
            Counters.JsPages += 3;
        }

        public static void GeneratePcComplPages(bool hasData) {
            string data = "";
            string data_full = "";

            if (!hasData) {
                data = "var pccompl = []";
            } else {

                DataTable t = DatabaseAPI.GetTable(StaticStrings.sql_compliancebypc_count);
                StringBuilder b = new StringBuilder();
                StringBuilder c = new StringBuilder();

                b.AppendLine("var " + GetJSString("pccompl") + " = [");
                c.AppendLine("var " + GetJSString("pccompl_full") + " = [");

                foreach (DataRow r in t.Rows) {
                    if (Convert.ToInt32(r[0]) < 75) {
                        c.AppendLine("['" + r[0].ToString() + "', "
                            + r[1] + ", "
                            + r[2] + ", "
                            + r[3] + ", "
                            + r[4] + ", '"
                            // Compose tooltip:
                            + "<div style=\"font-family: Arial; font-size: 14px; text-align: center;\">" + r[0] + "% compliant:</div>"
                            + "<div style=\"font-family: Arial; font-size: 12px;\">"

                            + "<p> <b>" + r[3] + " computers (" + r[5] + "% of total)</b> </p>"
                            + "<p> Min = " + r[1]
                            + ", Prev = " + r[2]
                            // + "<br/>Curr = " + r[3]
                            + ", Max = " + r[4]
                            + " </p></div>'],");
                    } else {
                        b.AppendLine("['" + r[0].ToString() + "', "
                            + r[1] + ", "
                            + r[2] + ", "
                            + r[3] + ", "
                            + r[4] + ", '"
                            // Compose tooltip:
                            + "<div style=\"font-family: Arial; font-size: 14px; text-align: center;\">" + r[0] + "% compliant:</div>"
                            + "<div style=\"font-family: Arial; font-size: 12px;\">"

                            + "<p> <b>" + r[3] + " computers (" + r[5] + "% of total)</b> </p>"
                            + "<p> Min = " + r[1]
                            + ", Prev = " + r[2]
                            // + "<br/>Curr = " + r[3]
                            + ", Max = " + r[4]
                            + " </p></div>'],");
                    }
                }
                // Remove the last comma we inserted
                c.Remove(c.Length - 3, 1);
                c.AppendLine("]");

                b.Remove(b.Length - 3, 1);
                b.AppendLine("]");
                data = b.ToString();
                data_full = c.ToString();
            }

            SaveToFile("javascript\\pccompl.js", data);
            SaveToFile("javascript\\pccompl_full.js", data_full);
            Counters.JsPages += 2;
        }

        public static void GenerateBulletinHtml(ref StringBuilder divs, ref StringBuilder jsfiles, string pagename){

            StringBuilder html = new StringBuilder();

            html.AppendLine("<html>\n\t<head>\n\t\t<script type=\"text/javascript\" src=\"https://www.google.com/jsapi\"></script>");
            html.AppendLine(jsfiles.ToString());
            html.AppendLine("<script type=\"text/javascript\" src=\"javascript/" + pagename + ".js\"></script>");
            html.AppendLine("\t\t<script type=\"text/javascript\">\n\tgoogle.load(\"visualization\", \"1\", {packages:[\"corechart\"]});\n\tgoogle.setOnLoadCallback(drawChart);");
            html.AppendLine("\t</script>");
            html.AppendLine("\t</head>\t<body>");
            html.AppendLine(divs.ToString());

            SaveToFile(pagename + ".html", html.ToString());
        }

        public static void GetUpdateData(ref string compliance, ref string stats, string update, string bulletin) {
            if (update.Length == 0 || update == string.Empty)
                return;

            string sql = @"
                         select Convert(Datetime, _Exec_time, 101) as 'Date', installed as 'Installed', Applicable as 'Applicable'
                           from TREND_WindowsCompliance_ByUpdate
                          where [update] = '" + update + @"' and bulletin = '" + bulletin + @"' ";

            DataTable t = DatabaseAPI.GetTable(sql);
            stats = GetJSONFromTable(t, update);

            string[,] _compliance = GetComplianceFromTable(t);
            compliance = GetComplianceJSONFromArray(_compliance, update);

        }

        private static void GetBulletinData(ref string compliance, ref string data, string bulletin) {
            string sql = @"
                         select Convert(Datetime, max(_Exec_time), 101) as 'Date', SUM(installed) as 'Installed', SUM(Applicable) as 'Applicable'
                           from TREND_WindowsCompliance_ByUpdate
                          where Bulletin = '" + bulletin + @"'
                          group by _Exec_id order by date";

            DataTable t = DatabaseAPI.GetTable(sql);
            data = GetJSONFromTable(t, bulletin);

            string[,] _compliance = GetComplianceFromTable(t);
            compliance = GetComplianceJSONFromArray(_compliance, bulletin);
        }

        private static void GetGlobalData(ref string compliance, ref string data) {
            string sql = @"
                         select Convert(Datetime, max(_Exec_time), 101) as 'Date', SUM(installed) as 'Installed', SUM(Applicable) as 'Applicable'
                           from TREND_WindowsCompliance_ByUpdate
                          group by _Exec_id order by date";

            DataTable t = DatabaseAPI.GetTable(sql);
            data = GetJSONFromTable(t, "global");

            string[,] _compliance = GetComplianceFromTable(t);
            compliance = GetComplianceJSONFromArray(_compliance, "global");
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

        private static string[,] GetComplianceFromTable(DataTable t) {
/*            string[,] d = new string[t.Rows.Count + 1, 2];

            d[0, 0] = DateTime.Parse(t.Rows[0][0].ToString()).AddDays(-1D).ToString();
            d[0, 1] = "0";
            int i = 1;
*/

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
            DataTable t = DatabaseAPI.GetTable(StaticStrings.sql_compliancebypc_bottom75percent);
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

        public static string GetJSString(string s) {
            s = s.Replace('-','_');
            s = s.Replace('.', '_');

            return s;
        }
    }
}
