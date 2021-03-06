using ezacquire.migration.Utility.Models;
using ezLib.Utility;
using ezLib.WebUtility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TTI.IDM;

namespace ezacquire.migration.Utility
{
    public class DocumentDao : IDisposable
    {
        // 登入 ISRA
        private string ISLibraryName = "";
        private string ISUserID = "";
        private string ISPassword = "";
        private string ISClassName = "";
        private bool isServerMode = false;
        private IdmIS ISObj = null;
        private string imageTempFolder = "";
        MigrationRecordsDao migrationRecordsDao;
        private bool disposed = false;

        #region ::Constructor::
        public DocumentDao()
        {
            Logger.Write("初始化參數中...");
            Console.WriteLine("初始化參數中...");
            try
            {
                if (ISObj == null) { ISObj = IdmIS.GetInstance(); Logger.Write("Create New IS Object !!!!"); }
                SetConfigurationValues();
                SetDataFolder();
                LogonIS();
                migrationRecordsDao = new MigrationRecordsDao();
            }
            catch (Exception ex)
            {
                ExceptionLogger.Write(ex);
            }
        }
        #endregion

        public void test(int NowThread, int count)
        {
            if (count % 1000 == 0)
                Logger.Write(NowThread + ":" + ISUserID + " ___ " + count);
        }

        #region ::Dispose::
        // 實現IDisposable中的Dispose方法
        public void Dispose()
        {
            //必須為true
            Dispose(true);
            //通知垃圾回收機制不再呼叫終結器（析構器）
            GC.SuppressFinalize(this);
        }

        // 不是必要的，提供一個Close方法僅僅是為了更符合其他語言（如C++）的規範
        public void Close()
        {
            Dispose();
        }

        // 必須，以備程式設計師忘記了顯式呼叫Dispose方法
        ~DocumentDao()
        {
            //必須為false
            Dispose(false);
        }

        ///<summary>/// 非密封類修飾用protected virtual
        /// 密封類修飾用private
        ///</summary>///<param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }
            if (disposing)
            {
                ISObj = null;
                migrationRecordsDao = null;
            }
            //讓型別知道自己已經被釋放
            disposed = true;
        }
        #endregion

        #region :: Initial Settings ::

        private void SetConfigurationValues()
        {
            Logger.Write("[START]");

            ISLibraryName = ConfigurationManager.AppSettings["ISLibraryName"];
            ISUserID = ConfigurationManager.AppSettings["ISUserID"];
            ISPassword = ConfigurationManager.AppSettings["ISPassword"];
            ISClassName = ConfigurationManager.AppSettings["ISClassName"];
            isServerMode = Boolean.Parse(ConfigurationManager.AppSettings["IsServerMode"]);

            imageTempFolder = ConfigurationManager.AppSettings["ImageTempFolder"];

            Logger.Write("[END]");
        }

        private void SetDataFolder()
        {
            Logger.Write("[START]");

            try
            {
                if (!Directory.Exists(imageTempFolder))
                {
                    Directory.CreateDirectory(imageTempFolder);
                }
            }
            catch
            {
                throw new Exception("建立 ImageTempFolder 失敗!");
            }

            Logger.Write("[END]");
        }
        #endregion :: Initial Settings ::

        private bool LogonIS()
        {
            Logger.Write("[START]");

            Logger.Write(string.Format("ISObj.LogonLib ISLibraryName='{0}' , ISUserID='{1}' , ISPassword='{2}' ,isServerMode='{3}' ",
                                        ISLibraryName, ISUserID, ISPassword, isServerMode));

            bool success = true;
            if (ISObj.LogonLib(ISLibraryName, ISUserID, ISPassword, false, isServerMode))
            {
                Logger.Write("登入 IS 成功");
            }
            else
            {
                Logger.Write("登入 IS 失敗");
                success = false;
            }

            Logger.Write("[END]");

            return success;
        }

        public string DoDownloadAction(string docId, out OriginalData originalData)
        {
            originalData = new OriginalData();
            string year = DateTime.Now.ToString("yyyy");
            string month = DateTime.Now.ToString("MM");
            string day = DateTime.Now.ToString("dd");
            string taskFolder = Path.Combine(imageTempFolder, year, month, day, docId);
            var result = Commons.RecreateDirectory(taskFolder);
            if (!result)
            {
                migrationRecordsDao.UpdateMigrationRecordsStatus(docId, "U", "");
                return "U| 創建資料夾有問題。";
            }

            try
            {
                List<string> imageList = ProcessDoc(docId, taskFolder, out originalData);
            }
            catch (Exception ex)
            {
                ExceptionLogger.Write(ex);
                string status = CheckExceptionMsg(ex.Message);
                migrationRecordsDao.UpdateMigrationRecordsStatus(docId, status, "");
                var errmsg = $"({taskFolder})";
                return status + "| Fail: " + ex.Message + " " + errmsg;
            }
            return "S| Success";
        }

        public List<IndexData> GetIndexData(string docId)
        {
            List<IndexData> indexDatas = new List<IndexData>();
            try
            {
                DocImageData docImageData = migrationRecordsDao.GetDocImageData(docId);
                indexDatas.Add(SetIndexData("PolicyDate", docImageData.PolicyDate));
                indexDatas.Add(SetIndexData("CloseDate", docImageData.CloseDate));
            }
            catch (Exception ex)
            {
                ExceptionLogger.Write(ex);
            }
            return indexDatas;
        }

        private List<string> ProcessDoc(string documentId, string taskFolder, out OriginalData originalData)
        {
            Logger.Write("");
            Logger.Write("DocumentId = " + documentId);
            originalData = new OriginalData();

            if (!LogonIS())
            {
                throw new Exception("登入IS有誤");
            }

            // 取得符合條件的 DocId
            ISVO isvo = ISObj.getDocData(documentId);
            if (isvo == null) return new List<string>();
            string isBpm = string.IsNullOrEmpty(isvo.ServiceNumber) ? "N" : "Y";
            string index = GetIndex(isvo, out DocumentAdd documentAdd);
            long docSize = 0;
            string sha1 = "";
            string status = "S";
            // 取得第一筆 DocId 的多頁 tif 影像檔
            List<string> imageFileList = new List<string>();
            if (!int.TryParse(isvo.F_PAGES, out int pageCount))
                throw new Exception("IS PageCount Error");
            if (isvo.F_CLOSED.ToUpper().Equals("TRUE")) //影像關閉及沒有Index的不移轉
            {
                status = "C";
            }
            else
            if (isvo.FormID.Equals("")) //影像關閉及沒有Index的不移轉
            {
                status = "D";
            }
            else
                imageFileList = ProcessDocId(taskFolder, isvo.F_DOCNUMBER, pageCount, out docSize, out sha1);

            migrationRecordsDao.UpdateMigrationRecords(isvo.F_DOCNUMBER, docSize, index, status, sha1, taskFolder, isBpm);
            originalData = new OriginalData()
            {
                File_Path = taskFolder,
                Original_DocId = isvo.F_DOCNUMBER,
                Original_DocSize = docSize.ToString(),
                Original_Index = documentAdd,
                Original_ImageSHA1 = sha1,
                Original_Pages = pageCount.ToString()
            };
            return imageFileList;
        }

        private List<string> ProcessDocId(string taskFolder, string docId, int pageCount, out long docSize, out string sha1)
        {
            Logger.Write("DocId = " + docId);

            docSize = 0;
            sha1 = "";
            List<string> imageFileList = new List<string>();
            for (int i = 1; i <= pageCount; i++)
            {
                string fileName = ISObj.ExportImageFilesByPage(docId, i, taskFolder, docId, true);
                Logger.Write($"docId {docId}, Page {i}, fileName:'{fileName}'");
                imageFileList.Add(fileName);
                FileInfo fi = new FileInfo(fileName);
                docSize += fi.Length;
                sha1 += GetSha1Hash(fileName);
            }

            Logger.Write($"Folder {taskFolder} : {imageFileList.Count}, totalSize:{docSize}");

            return imageFileList;
        }

        /// <summary>
        /// 取SHA1
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string GetSha1Hash(string filePath)
        {
            using (FileStream fs = File.OpenRead(filePath))
            {
                SHA1 sha = new SHA1Managed();
                return BitConverter.ToString(sha.ComputeHash(fs));
            }
        }

        private string GetIndex(ISVO isvo, out DocumentAdd documentAdd)
        {
            DocImageData docImageData = migrationRecordsDao.GetDocImageData(isvo.F_DOCNUMBER);
            List<IndexData> indexDatas = new List<IndexData>();
            indexDatas.Add(SetIndexData("IsBadDoc", isvo.IsBadDoc));
            indexDatas.Add(SetIndexData("Branch", isvo.Branch));
            indexDatas.Add(SetIndexData("PolicyID", new string[] { isvo.PolicyID1, isvo.PolicyID2, isvo.PolicyID3 }));
            indexDatas.Add(SetIndexData("POSNumber", new string[] { isvo.PosNumber, isvo.PosNumber2, isvo.PosNumber3 }));
            indexDatas.Add(SetIndexData("IsReject", new string[] { isvo.IsReject, isvo.IsReject2, isvo.IsReject3 }));
            indexDatas.Add(SetIndexData("ClientID", isvo.ClientID));
            indexDatas.Add(SetIndexData("ServiceNumber", new string[] { isvo.ServiceNumber, isvo.ServiceNumber2, isvo.ServiceNumber3 }));
            indexDatas.Add(SetIndexData("PolicyDate", docImageData.PolicyDate));
            indexDatas.Add(SetIndexData("CloseDate", docImageData.CloseDate));
            indexDatas.Add(SetIndexData("Important", isvo.Important));
            indexDatas.Add(SetIndexData("Inquire", isvo.Inquire));
            indexDatas.Add(SetIndexData("DepartCode", docImageData.DepartCode));

            documentAdd = new DocumentAdd();
            documentAdd.DocumentIndex = new DocumentIndexPartial()
            {
                FormID = isvo.FormID,
                ScanDateTime = docImageData.DIM_ScanTime,
                ScanType = isvo.ScanType,
                ScanUserId = "",    //**
                BatchNumber = docImageData.DIM_Batch,
                CreateDateTime = docImageData.CreateDateTime,
                VerifyDateTime = docImageData.DIM_VerifyTime,
                VerifyUserId = "",  //**
                ScanCaseId = docImageData.DIM_Batch + "001", //**
                DocumentType = "E",
                FilingNumber = isvo.FillingSerial,
                ScanStation = "",
                CommitServer = "",
                MimeType = "",
                MimeTypeDetail = "",
                UploadDateTime = docImageData.CreateDateTime,
                IndexData = indexDatas
            };
            return documentAdd.ToJson();
        }

        private IndexData SetIndexData(string name, string value)
        {
            IndexData indexData = new IndexData(name);
            indexData.Value.Add(value);
            return indexData;
        }

        private IndexData SetIndexData(string name, string[] values)
        {
            IndexData indexData = new IndexData(name);
            foreach (var value in values)
                indexData.Value.Add(value);
            return indexData;
        }

        private string CheckExceptionMsg(string msg)
        {
            string result = "E";
            try
            {
                string errorCode = ConfigurationManager.AppSettings["ErrorCode"]; // P:ORA...
                var errorCodes = errorCode.Split(';');

                foreach (var errCode in errorCodes)
                {
                    string code = errCode.Split(':')[0];
                    var errors = errCode.Split(':')[1].Split(',').ToList();
                    //if (code.Equals("P"))
                    //    errors.Add("不合法");
                    foreach (var error in errors)
                    {
                        if (msg.Contains(error))
                        {
                            result = code;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionLogger.Write(ex, "檢查錯誤訊息有問題。");
            }
            return result;
        }

    }
}