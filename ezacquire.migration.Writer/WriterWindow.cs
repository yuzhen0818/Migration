using ezacquire.migration.Utility;
using ezacquire.migration.Utility.Models;
using ezLib.Utility;
using ezLib.WebUtility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ezacquire.migration.Writer
{
    public partial class WriterWindow : Form
    {
        #region ::Field::
        List<BackgroundWorker> bws = new List<BackgroundWorker>();
        int ThreadCount;
        int CompletedThread = 0;
        int ExceTotal = 0;
        List<string> overThread = new List<string>();
        Dictionary<string, List<OriginalData>> originalDataList = new Dictionary<string, List<OriginalData>>();

        MigrationRecordsDao migrationRecordsDao = new MigrationRecordsDao();
        DocumentManage documentManage = new DocumentManage();
        DocumentDao documentDao = new DocumentDao();
        string startTime = "";
        string endTime = "";
        string weekendStartTime = "";
        string weekendEndTime = "";
        #endregion

        #region ::Constructor::
        public WriterWindow()
        {
            InitializeComponent();
            lblTime.Text = "";

            startTime = ConfigurationManager.AppSettings["StartTime"];
            endTime = ConfigurationManager.AppSettings["EndTime"];
            weekendStartTime = ConfigurationManager.AppSettings["WeekendStartTime"];
            weekendEndTime = ConfigurationManager.AppSettings["WeekendEndTime"];

            int.TryParse(ConfigurationManager.AppSettings["ThreadCount"], out ThreadCount);
            if (ThreadCount <= 0)
                ThreadCount = 1;

            for (int i = 0; i < ThreadCount; i++)
            {
                BackgroundWorker backgroundWorker = new BackgroundWorker();
                backgroundWorker.WorkerReportsProgress = true;
                backgroundWorker.WorkerSupportsCancellation = true;
                backgroundWorker.DoWork += new DoWorkEventHandler(do_work);
                backgroundWorker.ProgressChanged += new
                ProgressChangedEventHandler(backgroundWorker_ProgressChanged);
                backgroundWorker.RunWorkerCompleted += new
                RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);
                bws.Add(backgroundWorker);

                if (originalDataList.ContainsKey(i.ToString()))
                    originalDataList[i.ToString()] = new List<OriginalData>();
                else
                    originalDataList.Add(i.ToString(), new List<OriginalData>());
            }
        }
        #endregion

        #region ::Event::
        private void btnStart_Click(object sender, EventArgs e)
        {
            timer1.Interval = 1000;
            timer1.Enabled = true;
            progressBar1.Style = ProgressBarStyle.Marquee;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            if (bws.Count > 0 && bws[0].IsBusy)
            {
                DialogResult result;
                result = MessageBox.Show("程式執行中，確定要關閉嗎?", "", MessageBoxButtons.YesNo);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    for (int i = 0; i < ThreadCount; i++)
                    {
                        bws[i].CancelAsync();
                    }
                    progressBar1.Style = ProgressBarStyle.Blocks;
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Interval = 30000;
            string time = startTime;
            if (Commons.IsHolidays(DateTime.Now)) time = weekendStartTime;
            if (!string.IsNullOrEmpty(time) && !DateTime.Now.ToString("HHmm").Equals(time))
            {
                listBoxRecord.Items.Add($"還沒到指定時間，跳出，不開始，HHmm = {DateTime.Now.ToString("HHmm")} , StartTime = {time}");
                Logger.Write($"還沒到指定時間，跳出，不開始，HHmm = {DateTime.Now.ToString("HHmm")} , EndTime = {time}");
                return;
            }

            ExceUploadImageToIR(0);
            ExceTotal = -1;
            CompletedThread = 0;

            progressBar1.Style = ProgressBarStyle.Marquee;
            bool IsBusy = false;
            for (int i = 0; i < ThreadCount; i++)
            {
                if (bws[i].IsBusy)
                {
                    IsBusy = true;
                    break;
                }
            }
            if (!IsBusy)
            {
                for (int i = 0; i < ThreadCount; i++)
                {
                    bws[i].RunWorkerAsync(i);
                }
            }
            else
                MessageBox.Show("程式執行中．．．");
            timer2.Enabled = true;
            timer1.Enabled = false;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            string time = endTime;
            if (Commons.IsHolidays(DateTime.Now)) time = weekendEndTime;
            if (!DateTime.Now.ToString("HHmm").Equals(time))
            {
                //listBoxRecord.Items.Add($"還沒到指定時間，跳出，不結束，HHmm = {DateTime.Now.ToString("HHmm")} , EndTime = {time}");
                return;
            }
            Logger.Write($"到達指定時間，結束，HHmm = {DateTime.Now.ToString("HHmm")} , EndTime = {time}");
            for (int i = 0; i < ThreadCount; i++)
            {
                bws[i].CancelAsync();
            }
            progressBar1.Style = ProgressBarStyle.Blocks;
            timer1.Enabled = true;
            timer2.Enabled = false;
        }
        #endregion

        #region ::Private Method::
        private void ExceUploadImageToIR(int nowThread)
        {
            SetListBoxItem($"==================================================({nowThread})");
            var originalDatas = migrationRecordsDao.GetOriginalDataList(nowThread);
            SetListBoxItem($"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")} 取得待轉入影像共有 {originalDatas.Count()}筆");
            int index = 0;
            int count = originalDatas.Count / ThreadCount;
            if (originalDatas.Count % ThreadCount != 0) count++;
            for (int i = 0; i < ThreadCount; i++)
            {
                if (index + count > originalDatas.Count) count = originalDatas.Count - index;
                if (originalDataList.ContainsKey(i.ToString()))
                {
                    originalDataList[i.ToString()].Clear();
                    originalDataList[i.ToString()] = originalDatas.GetRange(index, count);
                }
                else
                    originalDataList.Add(i.ToString(), originalDatas.GetRange(index, count));
                index += count;
                if (index >= originalDatas.Count) break;
            }
        }

        private string AddImage(OriginalData originalData, out string mimeType)
        {
            Logger.Write($"============ DocId:{originalData.Original_DocId} ============");
            mimeType = "";
            try
            {
                string serialNumber = DateTime.Now.ToString("yyyyMMddHHmmssfff");

                DocumentAdd documentAdd = originalData.Original_Index;
                documentAdd.DocumentIndex.DocumentType = "D";
                List<IndexData> indexDatas = new List<IndexData>();
                foreach (var indexData in documentAdd.DocumentIndex.IndexData)
                {
                    indexData.Value = indexData.Value.Where(s => !string.IsNullOrEmpty(s)).ToList();
                    if (indexData.Value != null && indexData.Value.Count > 0)
                        indexDatas.Add(indexData);
                }
                documentAdd.DocumentIndex.IndexData = indexDatas;
                if (string.Compare(originalData.Original_DocId, "2580000") < 0)
                {
                    var policydate = documentAdd.DocumentIndex.IndexData.Where(s => s.Key == "PolicyDate").ToList();
                    if (policydate == null || policydate.Count <= 0)
                    {
                        var indexData = documentDao.GetIndexData(originalData.Original_DocId);
                        indexDatas.AddRange(indexData);
                    }
                }

                string guid = Guid.NewGuid().ToString();
                documentAdd.OperatorInfo = new OperatorInfo()
                {
                    OperatorUserId = "SysAdmin",
                    ClientIPAddress = ezLib.Utility.NetworkHelper.GetLocalIPAddress(),
                    SystemId = "migration",
                    TransactionId = guid,
                    ServerIPAddress = ""
                };

                int index = 1;
                List<List<FileItem>> f = new List<List<FileItem>>();
                string[] images = Directory.GetFiles(originalData.File_Path);
                foreach (var item in images)
                {
                    List<FileItem> fileItems = new List<FileItem>();

                    string type = Path.GetExtension(item).Replace(".", "");
                    string file = Convert.ToBase64String(File.ReadAllBytes(item));
                    FileItem fileItem = new FileItem();
                    fileItem.MimeType = type;
                    fileItem.File = file;
                    fileItems.Add(fileItem);
                    f.Add(fileItems);
                    index++;
                    mimeType = Path.GetExtension(item);
                }
                documentAdd.Files = f;

                string token = "";
                ezAcquireReturnCode gettoken = documentManage.GetToken();
                if (gettoken.Status.Equals("ERR"))
                {
                    //listBoxRecord.Items.Add($"Get Token失敗");
                    Logger.Write($"{originalData.Original_DocId} = Get Token失敗");
                    return "";
                }
                else
                {
                    token = gettoken.Result;
                    //listBoxRecord.Items.Add("Token->" + token);
                    //Logger.Write("Token->" + token);
                }
                var result = documentManage.WriteImage(token, documentAdd);
                if (result.Status.Equals("ERR"))
                {
                    //listBoxRecord.Items.Add($"Get retrun message from ezAcquire->ErrorId:{result.Error.ErrorId},Message:{result.Error.Message}");
                    Logger.Write($"{originalData.Original_DocId} Get retrun message from ezAcquire->ErrorId:{result.Error.ErrorId},Message:{result.Error.Message}");
                    throw new Exception($"{originalData.Original_DocId} = {result.Error.ErrorId} : {result.Error.Message}");
                }
                else
                {
                    //listBoxRecord.Items.Add($"寫入成功 , FileID:{result.Result}");
                    Logger.Write($"{originalData.Original_DocId}寫入成功 , FileID:{result.Result}");
                }
                return result.Result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.Write(ex);
                return "";
            }
        }

        private string GetSHA1(string docId, string mimeType, out int pages)
        {
            pages = 0;
            string token = "";
            ezAcquireReturnCode gettoken = documentManage.GetToken();
            if (gettoken.Status.Equals("ERR"))
            {
                Logger.Write($"Get Token失敗");
                return "";
            }
            else
            {
                token = gettoken.Result;
                Logger.Write("Token->" + token);
            }
            var result = documentManage.GetDocumentFile(token, docId, mimeType);
            if (result.Status.Equals("ERR"))
            {
                Logger.Write($"Get retrun message from ezAcquire->ErrorId:{result.Error.ErrorId},Message:{result.Error.Message}");
                throw new Exception($"{result.Error.ErrorId} : {result.Error.Message}");
            }
            else
            {
                Logger.Write($"取得影像成功 , FileString:{result.Result}");
            }

            List<DocumentFileString> fileList = new List<DocumentFileString>();
            JArray jArray = JArray.Parse(result.Result);
            foreach (JObject jo in jArray)
            {
                DocumentFileString fileApi = jo.ToObject<DocumentFileString>();

                fileList.Add(fileApi);
            }

            string sha1 = "";
            var imageTempFolder = ConfigurationManager.AppSettings["ImageTempFolder"];
            foreach (var file in fileList)
            {
                var filePath = Path.Combine(imageTempFolder, docId + "_" + file.Page + mimeType);
                try
                {
                    byte[] content = Convert.FromBase64String(file.File);
                    File.WriteAllBytes(filePath, content);

                    using (FileStream fs = File.OpenRead(filePath))
                    {
                        SHA1 sha = new SHA1Managed();
                        sha1 += BitConverter.ToString(sha.ComputeHash(fs));
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    try
                    {
                        if (File.Exists(filePath))
                            File.Delete(filePath);
                    }
                    catch { }
                }
                pages++;
            }
            return sha1;
        }

        private void SetListBoxItem(string message)
        {
            if (listBoxRecord.InvokeRequired)
            {
                listBoxRecord.Invoke(new MethodInvoker(delegate
                {
                    if (listBoxRecord.Items.Count > 10000)
                        listBoxRecord.Items.Clear();
                    listBoxRecord.Items.Add(message);
                }));
            }
            else
            {
                if (listBoxRecord.Items.Count > 10000)
                    listBoxRecord.Items.Clear();
                listBoxRecord.Items.Add(message);
            }
            Logger.Write(message);
        }

        private void RunParallelWorks<T>(int threadLimit, List<T> list, Action<T> run)
        {
            if (list == null || list.Count == 0)
                return;

            if (threadLimit == 0)
            {
                threadLimit = System.Environment.ProcessorCount * 2;
            }

            Semaphore pool = new Semaphore(threadLimit, threadLimit);

            ManualResetEvent doneEvent = new ManualResetEvent(false);
            int taskCount = list.Count;
            for (int i = 0; i < list.Count; ++i)
            {
                ThreadPool.QueueUserWorkItem(
                    (object o) => {
                        try
                        {
                            pool.WaitOne();
                            var d = (T)o;
                            run.Invoke(d);
                        }
                        catch (Exception ex)
                        {
                            ExceptionLogger.Write(ex);
                        }
                        finally
                        {
                            pool.Release();
                            if (Interlocked.Decrement(ref taskCount) == 0)
                            {
                                doneEvent.Set();
                            }
                        }
                    }
                    , list[i]
                );
            }
            doneEvent.WaitOne();
        }
        #endregion

        #region <<BackgroundWorker >>
        //背景執行
        private void do_work(object sender, DoWorkEventArgs e)
        {
            int NowThread = Convert.ToInt32(e.Argument);
            int count = 0;
            Logger.Write("執行緒 " + NowThread.ToString() + " 啟動");
            while (true)
            {
                try
                {
                    if (((BackgroundWorker)sender).CancellationPending == true)
                    {
                        e.Cancel = true;
                        return;
                    }

                    try
                    {
                        ExceTotal++;

                        if (count >= originalDataList[NowThread.ToString()].Count)
                        {
                            if (!overThread.Contains(NowThread.ToString()))
                            {
                                overThread.Add(NowThread.ToString());
                            }
                            if (count > 0)
                            {
                                originalDataList[NowThread.ToString()].Clear();
                                count = 0;
                            }

                            //if (NowThread == 0 && overThread.Count == ThreadCount)
                            if (NowThread == 0)
                            {
                                //overThread.Clear();
                                var isExce = true;
                                foreach (var od in originalDataList)
                                {
                                    if (od.Value != null && od.Value.Count > 0)
                                        isExce = false;
                                }
                                if (isExce)
                                    ExceUploadImageToIR(NowThread); //取得要做的資料
                            }
                        }
                        if (originalDataList[NowThread.ToString()].Count == 0) continue;

                        var data = originalDataList[NowThread.ToString()][count];
                        var result = AddImage(data, out string mimeType);
                        if (string.IsNullOrEmpty(result))
                        {
                            migrationRecordsDao.UpdateMigrationRecordsByezAcquire(data.Original_DocId, result, 0, "E", "");
                        }
                        else
                        {
                            //var sha1 = GetSHA1(result, mimeType, out int pages);  //用另一支程式做
                            int pages = 0;
                            string sha1 = "";
                            migrationRecordsDao.UpdateMigrationRecordsByezAcquire(data.Original_DocId, result, pages, "S", sha1);

                            //刪除暫存影像檔案，同時清空 MigrationRecords 裡的 File_Path 欄位
                            /*try
                            {
                                Directory.Delete(data.File_Path, true);
                            }
                            catch { };
                            migrationRecordsDao.UpdateMigrationRecordsStatus(data.Original_DocId, "S", "");*/
                        }
                        count++;

                        ((BackgroundWorker)sender).ReportProgress(1);

                        string msg = data.Original_DocId + "=> " + result;
                        SetListBoxItem("執行緒 " + NowThread.ToString() + ": " + msg);

                    }
                    catch (Exception ex)
                    {
                        string msg = NowThread.ToString() + "=> " + ex.Message;
                        SetListBoxItem(msg);
                    }
                }
                catch (Exception ex)
                {
                    string msg = NowThread.ToString() + "=> " + ex.Message;
                    SetListBoxItem(msg);
                }
            }
        }
        //處理進度條更新
        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }
        //讀取完畢
        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            CompletedThread++;
            if (e.Cancelled)
            {
                if (CompletedThread.Equals(ThreadCount))
                {
                    progressBar1.Style = ProgressBarStyle.Blocks;
                    listBoxRecord.Items.Add("執行完畢");
                    timer1.Enabled = true;
                    timer2.Enabled = false;
                }
            }
        }
        #endregion
    }
}
