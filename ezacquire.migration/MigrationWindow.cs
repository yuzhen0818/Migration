using ezacquire.migration.Utility;
using ezacquire.migration.Utility.Models;
using ezLib.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ezacquire.migration
{
    public partial class MigrationWindow : Form
    {
        #region ::Field::
        List<BackgroundWorker> bws = new List<BackgroundWorker>();
        int ThreadCount;
        int CompletedThread = 0;
        int ExceTotal = 0;
        List<string> docIds = new List<string>();
        List<string> overThread = new List<string>();
        Dictionary<string, List<string>> contents = new Dictionary<string, List<string>>();

        MigrationRecordsDao migrationRecordsDao = new MigrationRecordsDao();
        DocumentManage documentManage = new DocumentManage();
        string startTime = "";
        string endTime = "";
        string weekendStartTime = "";
        string weekendEndTime = "";
        string NeedGetData = "";
        bool isClosed = false;
        string NeedTimer = "Y";
        string stopTime = "";
        string restartTime = "";
        string NeedRestart = "Y";
        bool isRestart = false;
        string imageTempFolder = "";
        #endregion

        #region ::Constructor::
        public MigrationWindow(string[] args)
        {
            InitializeComponent();
            lblTime.Text = "";
            startTime = ConfigurationManager.AppSettings["StartTime"];
            endTime = ConfigurationManager.AppSettings["EndTime"];
            weekendStartTime = ConfigurationManager.AppSettings["WeekendStartTime"];
            weekendEndTime = ConfigurationManager.AppSettings["WeekendEndTime"];

            NeedGetData = ConfigurationManager.AppSettings["NeedGetData"];
            NeedTimer = ConfigurationManager.AppSettings["NeedTimer"];
            NeedRestart = ConfigurationManager.AppSettings["NeedRestart"];

            stopTime = ConfigurationManager.AppSettings["StopTime"];
            restartTime = ConfigurationManager.AppSettings["RestartTime"];

            imageTempFolder = ConfigurationManager.AppSettings["ImageTempFolder"];
            var result = Commons.RecreateDirectory(imageTempFolder);

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
                    contents[i.ToString()] = new List<string>();
                else
                    contents.Add(i.ToString(), new List<string>());
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
            isClosed = false;
            if (bws.Count > 0 && bws[0].IsBusy)
            {
                DialogResult result = System.Windows.Forms.DialogResult.Yes;
                if(e != null)
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
            else
                progressBar1.Style = ProgressBarStyle.Blocks;
            timer1.Enabled = false;
            timer2.Enabled = false;
        }

        //時間到啟動
        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Interval = 30000;
            string time = startTime;
            if (Commons.IsHolidays(DateTime.Now)) time = weekendStartTime;
            if (!string.IsNullOrEmpty(time) && !DateTime.Now.ToString("HHmm").Equals(time))
            {
                listBoxRecord.Items.Add($"還沒到指定時間，跳出，不開始，HHmm = {DateTime.Now.ToString("HHmm")} , StartTime = {time}");
                Logger.Write($"還沒到指定時間，跳出，不開始，HHmm = {DateTime.Now.ToString("HHmm")} , StartTime = {time}", "Timer");
                return;
            }

            RunWorker();

            timer2.Enabled = true;
            timer1.Enabled = false;
        }

        //時間到停止
        private void timer2_Tick(object sender, EventArgs e)
        {
            string time = endTime;
            if (Commons.IsHolidays(DateTime.Now)) time = weekendEndTime;
            if (!DateTime.Now.ToString("HHmm").Equals(time))
            {
                //listBoxRecord.Items.Add($"還沒到指定時間，跳出，不結束，HHmm = {DateTime.Now.ToString("HHmm")} , EndTime = {time}");
                //Logger.Write($"還沒到指定時間，跳出，不結束，HHmm = {DateTime.Now.ToString("HHmm")} , EndTime = {time}", "Timer");
                return;
            }
            Logger.Write($"到達指定時間，結束，HHmm = {DateTime.Now.ToString("HHmm")} , EndTime = {time}");
            isClosed = false;
            for (int i = 0; i < ThreadCount; i++)
            {
                if (bws[i].IsBusy)
                    bws[i].CancelAsync();
            }
            progressBar1.Style = ProgressBarStyle.Blocks;
            timer1.Enabled = true;
            timer2.Enabled = false;
        }

        //每分鐘去檢查Oracle是否活著
        private void timer3_Tick(object sender, EventArgs e)
        {
            Logger.Write("Check Oracle Status");
            var result = migrationRecordsDao.CheckSystemWakeup();
            Logger.Write("Check Oracle Status : " + result);
            if (result)
            {
                timer3.Enabled = false;
                isClosed = false;
                for (int i = 0; i < ThreadCount; i++)
                {
                    if (contents.ContainsKey(i.ToString()))
                        contents[i.ToString()].Clear();
                    if (!bws[i].IsBusy)
                        bws[i].RunWorkerAsync(i);
                }
            }
        }

        //每分鐘判斷是不是晚上 11.55
        private void timer4_Tick(object sender, EventArgs e)
        {
            if (!DateTime.Now.ToString("HHmm").Equals(stopTime))
            {
                return;
            }
            Logger.Write($"{stopTime}，停止到 {restartTime}");
            isClosed = false;
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

        #region ::Private Method::
        private bool ExceMigration()
        {
            bool result = true;
            try
            {
                /*DateTime baseDate = new DateTime(1970, 1, 1);
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
                lblTime.Text = startDay.ToString("yyyy-MM-dd") + " ~ " + endDay.ToString("yyyy-MM-dd");

                Logger.Write("ExceMigration: Select docId " + lblTime.Text);
                double start = (startDay - baseDate).TotalDays - 1;
                double end = (endDay - baseDate).TotalDays;
                migrationRecordsDao.InsertMigrationRecords(start, end);
                lblTime.Text += " 取得完成";
                Logger.Write("取得完成");

                Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                configuration.AppSettings.Settings["EndDay"].Value = endDay.ToString("yyyy/MM/dd");
                configuration.Save(ConfigurationSaveMode.Full, true);
                ConfigurationManager.RefreshSection("appSettings");*/

                string exePath = ConfigurationManager.AppSettings["exePath"];
                string[] cmd = { exePath, "" };
                string data = Cmd(cmd);
                lblTime.Text = data;
                if (data.Contains("ERROR"))
                {
                    Logger.Write("ExceMigrationByExe ERROR");
                    result = false;
                }
            }
            catch (Exception ex)
            {
                //lblTime.Text += " ERROR";
                listBoxRecord.Items.Add("ExceMigration Error: " + ex.Message);
                ExceptionLogger.Write(ex);
                result = false;
            }
            return result;
        }

        private void GetImageFromFileNet()
        {
            GC.Collect();
            Commons.RecreateDirectory(imageTempFolder);
            WriteLoggerListBox("==================================================");
            docIds = migrationRecordsDao.GetDocIdList();
            WriteLoggerListBox($"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")} 取得待取出影像共有 {docIds.Count}筆");
            if (docIds.Count == 0 && NeedGetData.Equals("Y")) //沒有資料就一直重複取 //因有多台電腦會跑，所以用Config設定哪台要執行
            {
                if (!ExceMigration())
                {
                    isClosed = true;
                    btnClose_Click(btnClose, null);
                    return;
                }
            }
            int index = 0;
            int count = docIds.Count / ThreadCount;
            if (docIds.Count % ThreadCount != 0) count++;
            for (int i = 0; i < ThreadCount; i++)
            {
                if (index + count > docIds.Count) count = docIds.Count - index;
                if (contents.ContainsKey(i.ToString()))
                {
                    contents[i.ToString()].Clear();
                    contents[i.ToString()] = docIds.GetRange(index, count);
                }
                else
                    contents.Add(i.ToString(), docIds.GetRange(index, count));
                index += count;
                if (index >= docIds.Count) break;
            }
        }

        private void WriteLoggerListBox(string msg)
        {
            if (listBoxRecord.Items.Count > 10000)
                listBoxRecord.Items.Clear();
            listBoxRecord.Items.Add(msg);
            Logger.Write(msg);
        }

        private void RunWorker()
        {
            GetImageFromFileNet(); //取得要做的資料
            ExceTotal = -1;
            CompletedThread = 0;

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

        private string AddImage(OriginalData originalData, DocumentDao documentDao, out string mimeType)
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
                    {
                        if (indexData.Key.ToUpper().Equals("ISREJECT"))
                        {
                            IndexData serviceNumber = documentAdd.DocumentIndex.IndexData.Where(x => x.Key.ToUpper().Equals("SERVICENUMBER")).FirstOrDefault();
                            if (serviceNumber != null)
                            {
                                List<string> serviceNumberValue = serviceNumber.Value.Where(s => !string.IsNullOrEmpty(s)).ToList();
                                if (serviceNumberValue != null && serviceNumberValue.Count > indexData.Value.Count)
                                {
                                    for (int i = 0; i < (serviceNumberValue.Count - indexData.Value.Count); i++)
                                        indexData.Value.Add("N");
                                }
                            }
                        }
                        indexDatas.Add(indexData);
                    }
                    else
                    {
                        if (indexData.Key.ToUpper().Equals("ISREJECT"))
                        {
                            indexData.Value = new List<string>() { "N" };
                            IndexData serviceNumber = documentAdd.DocumentIndex.IndexData.Where(x => x.Key.ToUpper().Equals("SERVICENUMBER")).FirstOrDefault();
                            if (serviceNumber != null)
                            {
                                List<string> serviceNumberValue = serviceNumber.Value.Where(s => !string.IsNullOrEmpty(s)).ToList();
                                if (serviceNumberValue != null && serviceNumberValue.Count > indexData.Value.Count)
                                {
                                    for (int i = 0; i < serviceNumberValue.Count - 1; i++)
                                        indexData.Value.Add("N");
                                }
                            }
                            indexDatas.Add(indexData);
                        }
                    }
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

        private string Cmd(string[] cmd)
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
        #endregion

        #region <<BackgroundWorker >>
        //背景執行
        private void do_work(object sender, DoWorkEventArgs e)
        {
            int NowThread = Convert.ToInt32(e.Argument);
            int count = 0;
            Logger.Write("執行緒 " + NowThread.ToString() + " 啟動");
            DocumentDao documentDao = new DocumentDao();
            while (true)
            {
                try
                {
                    if (((BackgroundWorker)sender).CancellationPending == true)
                    {
                        e.Cancel = true;
                        Logger.Write("執行緒 " + NowThread.ToString() + " 停止");
                        return;
                    }
                    try
                    {
                        ExceTotal++;
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

                            if (NowThread == 0)
                            {
                                var isExce = true;
                                foreach (var od in contents)
                                {
                                    if (od.Value != null && od.Value.Count > 0)
                                        isExce = false;
                                }
                                if (isExce)
                                    GetImageFromFileNet(); //取得要做的資料
                            }
                        }
                        if (contents[NowThread.ToString()].Count == 0) continue;
                        Logger.Write("執行緒 " + NowThread.ToString() + " **exeCount=" + count + " / " + contents[NowThread.ToString()].Count);

                        string Original_DocId = contents[NowThread.ToString()][count];
                        var result = documentDao.DoDownloadAction(Original_DocId, out OriginalData data);

                        count++;

                        ((BackgroundWorker)sender).ReportProgress(1);

                        string resultMsg = "執行緒 " + NowThread.ToString() + " : " + Original_DocId + "=> " + result;
                        Logger.Write(resultMsg);
                        Logger.Write("");
                        var writeResult = "";
                        if (result.StartsWith("P|") || isClosed)
                        {
                            isClosed = true;
                            ((BackgroundWorker)sender).CancelAsync();
                        }
                        else
                        if (result.StartsWith("S|"))
                        {
                            if(data != null && !string.IsNullOrEmpty(data.Original_ImageSHA1))
                            {
                                writeResult = AddImage(data, documentDao, out string mimeType);
                                if (string.IsNullOrEmpty(writeResult))
                                {
                                    migrationRecordsDao.UpdateMigrationRecordsByezAcquire(data.Original_DocId, writeResult, 0, "E", "");
                                }
                                else
                                {
                                    //var sha1 = GetSHA1(result, mimeType, out int pages);  //用另一支程式做
                                    int pages = 0;
                                    string sha1 = "";
                                    migrationRecordsDao.UpdateMigrationRecordsByezAcquire(data.Original_DocId, writeResult, pages, "S", sha1);

                                    //刪除暫存影像檔案
                                    try
                                    {
                                        Directory.Delete(data.File_Path, true);
                                    }
                                    catch (Exception ex) { ExceptionLogger.Write(ex, "刪除檔案。"); };
                                }
                            }
                        }
                        else
                        {
                            documentDao.Dispose();
                            documentDao = new DocumentDao();
                        }

                        resultMsg += ", " + writeResult;
                        if (listBoxRecord.InvokeRequired)
                        {
                            listBoxRecord.Invoke(new MethodInvoker(delegate
                            {
                                listBoxRecord.Items.Add(resultMsg);
                            }));
                        }
                        else
                        {
                            listBoxRecord.Items.Add(resultMsg);
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionLogger.Write(ex);
                        documentDao.Dispose();
                        documentDao = new DocumentDao();
                    }
                }
                catch (Exception ex)
                {
                    ExceptionLogger.Write(ex);
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
                    if (isClosed)
                    {
                        Logger.Write("有 P 先跳出Thread。");
                        timer3.Enabled = true;
                    }
                    else
                    if (!isRestart && NeedTimer.Equals("Y"))
                    {
                        timer1.Enabled = true;
                        timer2.Enabled = false;
                    }
                    CompletedThread = 0;
                }
            }
        }
        #endregion
    }
}