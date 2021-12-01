using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ezacquire.migration.Utility.Models
{
    public class OriginalData
    {
        /// <summary>
        /// 原始影像編號
        /// </summary>
        public string Original_DocId { set; get; }

        /// <summary>
        /// 原始影像的頁數 (從索引資料取得頁數)
        /// </summary>
        public string Original_Pages { set; get; }

        /// <summary>
        /// 原始影像的檔案大小
        /// </summary>
        public string Original_DocSize { set; get; }

        /// <summary>
        /// 原始影像索引
        /// </summary>
        public DocumentAdd Original_Index { set; get; }

        /// <summary>
        /// 取出原始影像，計算所得的SHA1值
        /// </summary>
        public string Original_ImageSHA1 { set; get; }

        /// <summary>
        /// 影像暫存路徑
        /// </summary>
        public string File_Path { set; get; }

        public OriginalData()
        {
            Original_DocId = "";
            Original_Pages = "";
            Original_DocSize = "";
            Original_ImageSHA1 = "";
            Original_Index = new DocumentAdd();
        }
    }



    public class ezAcquireData
    {
        public string DocId { set; get; }
        public string FilePath { set; get; }
    }
}
