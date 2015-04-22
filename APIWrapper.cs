using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using Altiris.Common;
using Altiris.Database;
using Altiris.NS;
using Altiris.NS.ItemManagement;
using Altiris.NS.Logging;
using Altiris.NS.ContextManagement;
using Altiris.NS.Security;
using Symantec.CWoC.PatchTrending;

namespace Symantec.CWoC.APIWrappers
{
    class SecurityAPI
    {
        public static bool is_user_admin()
        {
            bool is_altiris_admin = false;
            string identity = string.Empty;

            try
            {
                SecurityContextManager.SetContextData();
                Role role = SecurityRoleManager.Get(new Guid("{2E1F478A-4986-4223-9D1E-B5920A63AB41}"));
                if (role != null)
                    identity = role.Trustee.Identity;

                if (identity != string.Empty)
                {
                    foreach (string admin in SecurityTrusteeManager.GetCurrentUserMemberships())
                    {
                        if (admin == identity)
                        {
                            is_altiris_admin = true;
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
            return is_altiris_admin;
        }
    }

    class DatabaseAPI {
        public static DataTable GetTable(string sqlStatement) {
            DataTable t = new DataTable();
            try {
                using (DatabaseContext context = DatabaseContext.GetContext()) {
                    SqlCommand cmdAllResources = context.CreateCommand() as SqlCommand;
                    cmdAllResources.CommandText = sqlStatement;

                    using (SqlDataReader r = cmdAllResources.ExecuteReader()) {
                        t.Load(r);
                    }
                }
                ++Counters.SqlQueries;
				Counters.SqlRows += t.Rows.Count;
				
				// Logger.Log(sqlStatement);
				// Logger.Log(String.Format("SQL result set contained {0} rows.", t.Rows.Count.ToString()));
                return t;
            } catch {
                throw new Exception("Failed to execute SQL command...");
            }
        }

        public static int ExecuteNonQuery(string sqlStatement) {
            using (DatabaseContext context = DatabaseContext.GetContext()) {
                SqlCommand sql_cmd = context.CreateCommand() as SqlCommand;
                sql_cmd.CommandText = sqlStatement;

                return sql_cmd.ExecuteNonQuery();
            }
        }

        public static int ExecuteScalar(string sqlStatement) {
            using (DatabaseContext context = DatabaseContext.GetContext()) {
                SqlCommand cmd = context.CreateCommand() as SqlCommand;

                cmd.CommandText = sqlStatement;
                Object result = cmd.ExecuteScalar();

                return Convert.ToInt32(result);
            }
        }
    }

    class Timer {
        private static Stopwatch chrono;

        public static void Init() {
            chrono = new Stopwatch();
            chrono.Start();
        }

        public static void Start() {
            chrono.Start();
        }
        public static void Stop() {
            chrono.Stop();
        }
        public static string tickCount() {
            return chrono.ElapsedTicks.ToString();
        }
        public static string duration() {
            return chrono.ElapsedMilliseconds.ToString();
        }
    }

    class Logger{
        public static void LogEx(Exception e) {
            string msg = string.Format("Caught exception {0}\nInnerException={1}\nStackTrace={2}", e.Message, e.InnerException, e.StackTrace);
            Console.WriteLine(msg);
            Altiris.NS.Logging.EventLog.ReportError(msg);
        }

        public static void Log(string msg) {
            // Console.WriteLine(msg);
            Altiris.NS.Logging.EventLog.ReportInfo(msg);
        }
    }
}
