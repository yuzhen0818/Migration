using ezacquire.migration.Utility.Models;
using ezLib.Utility;
using ezLib.WebUtility;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using TTI.IDM;
using TTI.MLI.Data;

namespace ezacquire.migration.Utility
{
    public class MigrationRecordsDao
    {
        public static string connectionString = "";

        public static string oleconnString = "";
        public static string iServerName = "";
        public static int iCaseCount = 500;
        public MigrationRecordsDao()
        {
            Logger.Write("初始化參數中...");
            Console.WriteLine("初始化參數中...");
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

                iServerName = ConfigurationManager.AppSettings["ServerName"];
                iCaseCount = int.Parse(ConfigurationManager.AppSettings["CaseCount"]);
            }
            catch (Exception ex)
            {
                ExceptionLogger.Write(ex);
            }
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
        }
        
        public void UpdateMigrationRecords(string docId, long docSize, string index, string readStatus, string sha1, string filePath, string isBpm)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        var cmd = conn.CreateCommand();
                        cmd.CommandText = " UPDATE MigrationRecords SET Original_ReadDT=GETDATE(), Original_DocSize = @original_DocSize" +
                                          ", Original_Index=@original_Index, Original_ReadStatus=@original_ReadStatus, Original_ImageSHA1=@original_ImageSHA1 " +
                                          ", File_Path=@filePath, Is_Bpm=@is_Bpm " +
                                          " WHERE Original_DocId = @original_DocId ";
                        cmd.Parameters.AddRange(new List<SqlParameter> {
                            new SqlParameter { ParameterName = "@original_DocSize"      , Value = docSize           },
                            new SqlParameter { ParameterName = "@original_Index"        , Value = index             },
                            new SqlParameter { ParameterName = "@original_ReadStatus"   , Value = readStatus        },
                            new SqlParameter { ParameterName = "@original_ImageSHA1"    , Value = sha1              },
                            new SqlParameter { ParameterName = "@filePath"              , Value = filePath          },
                            new SqlParameter { ParameterName = "@is_Bpm"                , Value = isBpm             },
                            new SqlParameter { ParameterName = "@original_DocId"        , Value = docId             }
                        }.ToArray());
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();

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
        }

        public void UpdateMigrationRecordsStatus(string docId, string readStatus, string filePath)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        var cmd = conn.CreateCommand();
                        cmd.CommandText = " UPDATE MigrationRecords SET Original_ReadStatus=@original_ReadStatus " +
                                          ", File_Path=@filePath" +
                                          " WHERE Original_DocId = @original_DocId ";
                        cmd.Parameters.AddRange(new List<SqlParameter> {
                            new SqlParameter { ParameterName = "@original_ReadStatus"   , Value = readStatus    },
                            new SqlParameter { ParameterName = "@filePath"              , Value = filePath      },
                            new SqlParameter { ParameterName = "@original_DocId"        , Value = docId         }
                        }.ToArray());
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();

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
        }

        public void UpdateMigrationRecordsByezAcquire(string docId, string ezAcquireDocId, long pages, string writeStatus, string sha1)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        var cmd = conn.CreateCommand();
                        cmd.CommandText = " UPDATE MigrationRecords SET ezAcquire_DocId=@ezAcquire_DocId, ezAcquire_Pages = @ezAcquire_Pages" +
                                          ", ezAcquire_WriteDT=GETDATE(), ezAcquire_WriteStatus=@ezAcquire_WriteStatus, ezAcquire_ImageSHA1=@ezAcquire_ImageSHA1 " +
                                          " WHERE Original_DocId = @original_DocId ";
                        cmd.Parameters.AddRange(new List<SqlParameter> {
                            new SqlParameter { ParameterName = "@ezAcquire_DocId"       , Value = ezAcquireDocId    },
                            new SqlParameter { ParameterName = "@ezAcquire_Pages"       , Value = pages             },
                            new SqlParameter { ParameterName = "@ezAcquire_WriteStatus" , Value = writeStatus       },
                            new SqlParameter { ParameterName = "@ezAcquire_ImageSHA1"   , Value = sha1              },
                            new SqlParameter { ParameterName = "@original_DocId"        , Value = docId             }
                        }.ToArray());
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();

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
        }
        
        public void UpdateezAcquireSHA1(string ezAcquireDocId, long pages, string sha1, string filePath, string status)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        var cmd = conn.CreateCommand();
                        cmd.CommandText = " UPDATE MigrationRecords SET ezAcquire_Pages = @ezAcquire_Pages" +
                                          ", ezAcquire_ImageSHA1=@ezAcquire_ImageSHA1, File_Path=@filePath " +
                                          ", ezAcquire_WriteStatus=@ezAcquire_WriteStatus " +
                                          " WHERE ezAcquire_DocId = @ezAcquire_DocId ";
                        cmd.Parameters.AddRange(new List<SqlParameter> {
                            new SqlParameter { ParameterName = "@ezAcquire_DocId"       , Value = ezAcquireDocId    },
                            new SqlParameter { ParameterName = "@ezAcquire_Pages"       , Value = pages             },
                            new SqlParameter { ParameterName = "@ezAcquire_ImageSHA1"   , Value = sha1              },
                            new SqlParameter { ParameterName = "@filePath"              , Value = filePath          },
                            new SqlParameter { ParameterName = "@ezAcquire_WriteStatus" , Value = status            }
                        }.ToArray());
                        cmd.Transaction = transaction;
                        cmd.ExecuteNonQuery();

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
        }

        public List<string> GetDocIdList(int nowThread = 0)
        {
            try
            {
                string subServerName = "";
                if (nowThread > 0) subServerName = "_" + nowThread;
                List<string> docIds = new List<string>();
                //寫入SQLServer
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "[DataMigration].[dbo].[getUnPickupImage]";
                    cmd.Parameters.AddRange(new List<SqlParameter> {
                            new SqlParameter { ParameterName = "@iTransferServerName" , Value = iServerName + subServerName },
                            new SqlParameter { ParameterName = "@iCaseCount" , Value = iCaseCount     }
                         }.ToArray());
                    var dr = cmd.ExecuteReader(CommandBehavior.SingleResult);
                    while (dr.Read())
                    {
                        var Original_DocId = dr["Original_DocId"].ToString();
                        docIds.Add(Original_DocId);
                    }
                    conn.Close();
                }
                return docIds;
            }
            catch (Exception ex)
            {
                ExceptionLogger.Write(ex);
                throw ex;
            }
        }

        public List<OriginalData> GetOriginalDataList(int nowThread = 0)
        {
            try
            {
                string subServerName = "";
                if (nowThread > 0) subServerName = "_" + nowThread;
                List<OriginalData> originalDatas = new List<OriginalData>();
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "[DataMigration].[dbo].[getUnWritedImage]";
                    cmd.Parameters.AddRange(new List<SqlParameter> {
                            new SqlParameter { ParameterName = "@iWriterServerName" , Value = iServerName + subServerName   },
                            new SqlParameter { ParameterName = "@iCaseCount" , Value = iCaseCount     }
                         }.ToArray());
                    var dr = cmd.ExecuteReader(CommandBehavior.SingleResult);
                    while (dr.Read())
                    {
                        OriginalData originalData = new OriginalData()
                        {
                            Original_DocId = dr["Original_DocId"].ToString(),
                            Original_DocSize = dr["Original_DocSize"].ToString(),
                            Original_ImageSHA1 = dr["Original_ImageSHA1"].ToString(),
                            Original_Index = JsonConvert.DeserializeObject<DocumentAdd>(dr["Original_Index"].ToString()),
                            Original_Pages = dr["Original_Pages"].ToString(),
                            File_Path = dr["File_Path"].ToString()
                        };
                        originalDatas.Add(originalData);
                    }
                    conn.Close();
                }
                return originalDatas;
            }
            catch (Exception ex)
            {
                ExceptionLogger.Write(ex);
                throw ex;
            }
        }

        public DocImageData GetDocImageData(string docId)
        {
            try
            {
                //從F_SW.doctaba 取出docId
                string oraclecmd = " Select DIM_CommitTime, DIM_ScanTime, DIM_VerifyTime, DIM_Batch, DIM_FilingSerial, F_ENTRYDATE " +
                                    ", DIM_POLICYDATE, DIM_CLOSEDATE" +
                                    " From F_SW.doctaba " +
                                    " Left Join DocImage on  to_char(F_DOCNUMBER) = DIM_FNDocID " +
                                    " Where F_DOCNUMBER = '{0}' ";
                oraclecmd = string.Format(oraclecmd, docId);
                //DataTable table = DAC.OracleTemplate.FillDataTable(oraclecmd, docId);
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
                }
                finally
                {
                    oleconn.Close();
                }

                DocImageData docImageData = new DocImageData();
                foreach (DataRow row in table.Rows)
                {
                    docImageData.DIM_Batch = row["DIM_Batch"] == null ? "" : row["DIM_Batch"].ToString();
                    docImageData.DIM_CommitTime = row["DIM_CommitTime"] == null ? "" : row["DIM_CommitTime"].ToString();
                    docImageData.DIM_ScanTime = row["DIM_ScanTime"] == null ? "" : row["DIM_ScanTime"].ToString();
                    docImageData.DIM_VerifyTime = row["DIM_VerifyTime"] == null ? "" : row["DIM_VerifyTime"].ToString();
                    docImageData.FillingSerial = row["DIM_FilingSerial"] == null ? "" : row["DIM_FilingSerial"].ToString();
                    docImageData.DepartCode = "";//row["DepartCode"] == null ? "" : row["DepartCode"].ToString();
                    docImageData.CreateDateTime = row["F_ENTRYDATE"] == null ? "" : row["F_ENTRYDATE"].ToString();
                }
                return docImageData;
            }
            catch (Exception ex)
            {
                ExceptionLogger.Write(ex);
                throw ex;
            }
        }

        public List<ezAcquireData> GetezAcquireDocIdList(int nowThread = 0)
        {
            try
            {
                string subServerName = "";
                if (nowThread > 0) subServerName = "_" + nowThread;
                List<ezAcquireData> ezAcquireDatas = new List<ezAcquireData>();
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "[DataMigration].[dbo].[getUnWriteSHA1]";
                    cmd.Parameters.AddRange(new List<SqlParameter> {
                            new SqlParameter { ParameterName = "@iServerName" , Value = iServerName + subServerName },
                            new SqlParameter { ParameterName = "@iCaseCount" , Value = iCaseCount     }
                         }.ToArray());
                    var dr = cmd.ExecuteReader(CommandBehavior.SingleResult);
                    while (dr.Read())
                    {
                        ezAcquireData ezAcquireData = new ezAcquireData()
                        {
                            DocId = dr["ezAcquire_DocId"].ToString(),
                            FilePath = dr["File_Path"].ToString()
                        };
                        ezAcquireDatas.Add(ezAcquireData);
                    }
                    conn.Close();
                }
                return ezAcquireDatas;
            }
            catch (Exception ex)
            {
                ExceptionLogger.Write(ex);
                throw ex;
            }
        }

        public bool CheckSystemWakeup()
        {
            bool result = true;
            //從F_SW.doctaba 取出docId
            string oraclecmd = " Select DIM_CommitTime, DIM_ScanTime, DIM_VerifyTime, DIM_Batch, DIM_FilingSerial, F_ENTRYDATE " +
                                " From DocImage " +
                                " Where DIM_FNDocID = '100000' ";
            //DataTable table = DAC.OracleTemplate.FillDataTable(oraclecmd, docId);
            DataTable table = new DataTable();
            OracleConnection oleconn = new OracleConnection(oleconnString);
            try
            {
                oleconn.Open();
            }
            catch(Exception ex)
            {
                result = false;
                ExceptionLogger.Write(ex);
            }
            finally
            {
                oleconn.Close();
            }
            return result;
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
    }
}
