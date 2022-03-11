using ezLib.Utility;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationRecodrs
{
    public class Core
    {
        public static string connectionString = "";

        public static string oleconnString = "";
        public Core()
        {
            Logger.Write("初始化參數中...");
            try
            {
                connectionString = String.Format(
                   ConfigurationManager.AppSettings["ConnectionString"],
                   ConfigurationManager.AppSettings["UserId"],
                   DecodeString(ConfigurationManager.AppSettings["UserPassword"]));

                oleconnString = String.Format(
                   ConfigurationManager.AppSettings["oleConnectionString"],
                   ConfigurationManager.AppSettings["oleUserId"],
                   DecodeString(ConfigurationManager.AppSettings["oleUserPassword"]));
            }
            catch (Exception ex)
            {
                ExceptionLogger.Write(ex);
            }
        }

        public void Run()
        {
            string result = "";
            try
            {
                DateTime baseDate = new DateTime(1970, 1, 1);
                DateTime.TryParse(ConfigurationManager.AppSettings["StartDay"], out DateTime startDay);
                if (ConfigurationManager.AppSettings["EndDay"] != "") // 如果有EndDay代表上次取到最後時間，要從那天在往後取
                    DateTime.TryParse(ConfigurationManager.AppSettings["EndDay"], out startDay);
                DateTime endDay = startDay;

                if (ConfigurationManager.AppSettings["AddDays"] != "")
                {
                    int.TryParse(ConfigurationManager.AppSettings["AddDays"], out int days);
                    endDay = startDay.AddDays(days);
                }
                if (ConfigurationManager.AppSettings["AddMonths"] != "")
                {
                    int.TryParse(ConfigurationManager.AppSettings["AddMonths"], out int months);
                    endDay = startDay.AddMonths(months);
                }
                result = startDay.ToString("yyyy-MM-dd") + " ~ " + endDay.ToString("yyyy-MM-dd");

                Logger.Write("ExceMigration: Select docId " + result);
                double start = (startDay - baseDate).TotalDays - 1;
                double end = (endDay - baseDate).TotalDays;
                InsertMigrationRecords(start, end);
                result += " 取得完成";
                Logger.Write("取得完成");

                Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                configuration.AppSettings.Settings["EndDay"].Value = endDay.ToString("yyyy/MM/dd");
                configuration.Save(ConfigurationSaveMode.Full, true);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                result += " ERROR(" + ex.Message + ")";
                ExceptionLogger.Write(ex);
            }
            Console.Write(result);
        }

        public void InsertMigrationRecords(double start, double end)
        {
            try
            {
                //從F_SW.doctaba 取出docId
                string oraclecmd = " Select F_DOCNUMBER, F_ENTRYDATE, F_PAGES " +
                                    " From F_SW.doctaba " +
                                    " Where F_Entrydate >= {0} and F_Entrydate < {1} ";
                oraclecmd = string.Format(oraclecmd, start, end);
                //DataTable table = DAC.OracleTemplate.FillDataTable(oraclecmd, start, end);
                DataTable table = new DataTable();
                OracleConnection oleconn = new OracleConnection(oleconnString);
                try
                {
                    oleconn.Open();
                    DataSet dataSet = new DataSet();

                    OracleCommand cmd = new OracleCommand(oraclecmd);

                    cmd.CommandType = CommandType.Text;

                    cmd.Connection = oleconn;

                    using (OracleDataAdapter dataAdapter = new OracleDataAdapter())
                    {
                        dataAdapter.SelectCommand = cmd;
                        dataAdapter.Fill(dataSet);
                        table = dataSet.Tables[0];
                    }
                    Logger.Write("Table.Row.Count= " + table.Rows.Count);
                }
                finally
                {
                    oleconn.Close();
                }
                //寫入SQLServer
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        var cmd = conn.CreateCommand();
                        cmd.CommandText = " IF NOT EXISTS(SELECT 1 FROM dbo.MigrationRecords WHERE Original_DocId = @Original_DocId )" +
                                          " BEGIN " +
                                          "   INSERT INTO MigrationRecords(Original_DocId, Original_Pages) " +
                                          "   VALUES (@Original_DocId, @Original_Pages) " +
                                          " END "
                                          ;
                        cmd.Transaction = transaction;
                        foreach (DataRow row in table.Rows)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddRange(new List<SqlParameter> {
                                new SqlParameter { ParameterName = "@Original_DocId"    , Value = row["F_DOCNUMBER"] == null ? "" : row["F_DOCNUMBER"].ToString()  },
                                new SqlParameter { ParameterName = "@Original_Pages"    , Value = row["F_PAGES"] == null ? "0" : row["F_PAGES"].ToString()  }
                            }.ToArray());
                            if (transaction != null) cmd.Transaction = transaction;
                            cmd.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                ExceptionLogger.Write(ex);
                throw ex;
            }
            finally
            {

            }
        }

        private string DecodeString(string source)
        {
            string key = "705434F8-5F797444-2B667B50-714F10";
            string returnString = "";
            try
            {
                returnString = ezLib.Utility.AESCryptography.Decrypt(key, source);
            }
            catch
            {
                throw new Exception($"Can not decode string , {source}");
            }
            return returnString;
        }
        
        static string Cmd(string[] cmd)
        {
            Process proc = new Process();
            try
            {
                proc.StartInfo.FileName = cmd[0];
                proc.StartInfo.Arguments = cmd[1];
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;

                proc.Start();

                string stdout = "";
                using (StreamReader reader = proc.StandardOutput)
                {
                    stdout += reader.ReadToEnd();
                }

                proc.WaitForExit();

                return stdout;
            }
            finally
            {
                proc.Close();
            }
        }
    }
}
