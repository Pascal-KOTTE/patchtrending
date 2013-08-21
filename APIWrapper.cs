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
using Altiris.Resource;
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

    class DatabaseAPI
    {
        public static DataTable GetTable(string sqlStatement)
        {

            using (DatabaseContext context = DatabaseContext.GetContext()) {
                return GetTable(sqlStatement, context);
            }
        }

        public static DataTable GetTable(string sqlStatement, DatabaseContext context) {
            Stopwatch chrono = new Stopwatch();
            chrono.Start();

            Counters.SqlQueries++;

            DataTable t = new DataTable();
            try {
                SqlCommand cmdAllResources = context.CreateCommand() as SqlCommand;
                cmdAllResources.CommandText = sqlStatement;

                using (SqlDataReader r = cmdAllResources.ExecuteReader()) {
                    t.Load(r);
                }
            } catch (Exception e) {
                Altiris.NS.Logging.EventLog.ReportException("Failed to execute SQL query: " + sqlStatement, e);
            }

            chrono.Stop();
            string msg = string.Format("SQL query completed in {0} ms, taking {1} ticks. SQL statement is:\n{2}",    
                chrono.ElapsedMilliseconds.ToString(), chrono.ElapsedTicks.ToString(), sqlStatement);
            Altiris.NS.Logging.EventLog.ReportProfile(msg);
            return t;

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
}
