using ezacquire.migration.Utility;
using ezacquire.migration.Utility.Models;
using ezLib.Utility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ezacquire.migration.Checker
{
    public partial class CheckerWindow : Form
    {
        #region ::Field::
        List<BackgroundWorker> bws = new List<BackgroundWorker>();
        int ThreadCount;
        int CompletedThread = 0;
        MigrationRecordsDao migrationRecordsDao = new MigrationRecordsDao();
        DocumentManage documentManage = new DocumentManage();
        Dictionary<string, List<ezAcquireData>> contents = new Dictionary<string, List<ezAcquireData>>();
        List<string> overThread = new List<string>();
        string startTime = "";
        string endTime = "";
        string weekendStartTime = "";
        string weekendEndTime = "";
        string NeedTimer = "Y";
        string stopTime = "";
        string restartTime = "";
        string NeedRestart = "Y";
        bool isRestart = false;
        #endregion

        #region ::Constructor::
        public CheckerWindow(string[] args)
        {
            InitializeComponent();
            
            startTime = ConfigurationManager.AppSettings["StartTime"];
            endTime = ConfigurationManager.AppSettings["EndTime"];
            weekendStartTime = ConfigurationManager.AppSettings["WeekendStartTime"];
            weekendEndTime = ConfigurationManager.AppSettings["WeekendEndTime"];

            NeedTimer = ConfigurationManager.AppSettings["NeedTimer"];
            NeedRestart = ConfigurationManager.AppSettings["NeedRestart"];
            stopTime = ConfigurationManager.AppSettings["StopTime"];
            restartTime = ConfigurationManager.AppSettings["RestartTime"];

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

                if (contents.ContainsKey(i.ToString()))
                    contents[i.ToString()] = new List<ezAcquireData>();
                else
                    contents.Add(i.ToString(), new List<ezAcquireData>());
            }

            timer1.Enabled = false;
            timer2.Enabled = false;
            timer4.Enabled = (NeedRestart == "Y");
            timer5.Enabled = false;

            if (args != null && args.Count() > 0)
            {
                btnStart_Click(btnStart, null);
            }
        }
        #endregion

        #region ::Event::
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (NeedTimer == "Y")
            {
                timer1.Interval = 1000;
                timer1.Enabled = true;
            }
            else
                RunWorker();
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
            RunWorker();
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

        //每分鐘判斷是不是晚上 11.55
        private void timer4_Tick(object sender, EventArgs e)
        {
            if (!DateTime.Now.ToString("HHmm").Equals(stopTime))
            {
                return;
            }
            Logger.Write($"{stopTime}，停止到 {restartTime}");
            for (int i = 0; i < ThreadCount; i++)
            {
                if (bws[i].IsBusy)
                    bws[i].CancelAsync();
            }
            progressBar1.Style = ProgressBarStyle.Blocks;
            isRestart = true;
            timer5.Enabled = true;
            timer4.Enabled = false;
        }

        //11.55停止到12.05
        private void timer5_Tick(object sender, EventArgs e)
        {
            if (!DateTime.Now.ToString("HHmm").Equals(restartTime))
            {
                return;
            }
            Logger.Write($"{restartTime} 重新啟動");
            RunWorker();
            progressBar1.Style = ProgressBarStyle.Marquee;
            isRestart = false;
            timer4.Enabled = true;
            timer5.Enabled = false;
        }
        #endregion

        #region ::private method::
        private void GetezAcquireDocId(int nowThread)
        {
            GC.Collect();
            SetListBoxItem($"==================================================({nowThread})");
            var docIds = migrationRecordsDao.GetezAcquireDocIdList(nowThread);
            SetListBoxItem($"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")} {nowThread}取得已轉入影像共有 {docIds.Count()}筆");
            int index = 0;
            int count = docIds.Count / ThreadCount;
            if (docIds.Count % ThreadCount != 0) count++;
            for (int i = 0; i < ThreadCount; i++)
            {
                if (index + count > docIds.Count) count = docIds.Count - index;
                if (contents.ContainsKey(i.ToString()))
                    contents[i.ToString()] = docIds.GetRange(index, count);
                else
                    contents.Add(i.ToString(), docIds.GetRange(index, count));
                index += count;
                if (index >= docIds.Count) break;
            }
        }

        private string UpdateezAcquireSHA1(string docId, string filePath)
        {
            string result = "OK";
            try
            {
                var sha1 = GetSHA1(docId, "", out int pages);  //用另一支程式做
                migrationRecordsDao.UpdateezAcquireSHA1(docId, pages, sha1, "", "F");

                //刪除暫存影像檔案
                try
                {
                    Directory.Delete(filePath, true);
                }
                catch { };
            }
            catch(Exception ex)
            {
                result = ex.Message;
            }
            return result;
        }

        private string GetSHA1(string docId, string mimeType, out int pages)
        {
            pages = 0;
            string token = "";
            ezAcquireReturnCode gettoken = documentManage.GetToken();
            if (gettoken.Status.Equals("ERR"))
            {
                Logger.Write($"Get Token失敗");
                throw new Exception($"Get Token失敗");
            }
            else
            {
                token = gettoken.Result;
                //Logger.Write("Token->" + token);
            }
            var result = documentManage.GetDocumentFile(token, docId, mimeType);
            if (result.Status.Equals("ERR"))
            {
                Logger.Write($"Get retrun message from ezAcquire->ErrorId:{result.Error.ErrorId},Message:{result.Error.Message}");
                throw new Exception($"{result.Error.ErrorId} : {result.Error.Message}");
            }
            else
            {
                Logger.Write($"取得影像成功 , FileString:{result.Status}");
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

        private void RunWorker()
        {
            GetezAcquireDocId(0);
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
                        if (count >= contents[NowThread.ToString()].Count)
                        {

                            if (!overThread.Contains(NowThread.ToString()))
                            {
                                overThread.Add(NowThread.ToString());
                            }

                            if (count > 0)
                            {
                                contents[NowThread.ToString()].Clear();
                                count = 0;
                            }

                            if(NowThread == 0)
                            {
                                var isExce = true;
                                foreach (var od in contents)
                                {
                                    if (od.Value != null && od.Value.Count > 0)
                                        isExce = false;
                                }
                                if (isExce)
                                    GetezAcquireDocId(NowThread); //取得要做的資料
                            }
                        }
                        if (contents[NowThread.ToString()].Count == 0) continue;

                        ezAcquireData data = contents[NowThread.ToString()][count];
                        Logger.Write(NowThread.ToString() + "=> --- ezAcquire.docId= " + data.DocId);
                        string result = UpdateezAcquireSHA1(data.DocId, data.FilePath);
                        count++;

                        ((BackgroundWorker)sender).ReportProgress(1);

                        SetListBoxItem(data.DocId + ": " + result);

                    }
                    catch (Exception ex)
                    {
                        string msg = NowThread.ToString() + "=> " + ex.Message;
                        SetListBoxItem(msg);
                    }
                    finally
                    {
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
                    if (!isRestart && NeedTimer.Equals("Y"))
                    {
                        timer1.Enabled = true;
                        timer2.Enabled = false;
                    }
                }
            }
        }
        #endregion
    }
}
