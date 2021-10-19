using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ezacquire.migration.Utility.Models
{
    public class ImportData
    {
        /// <summary>
        /// [必填] 表單代碼
        /// </summary>
        public string formid { set; get; }

        /// <summary>
        /// [必填] 掃描管道
        /// </summary>
        public string scantype { set; get; }

        /// <summary>
        /// [必填] 寫入的索引值，使用indexdescription裡設定的名稱
        /// </summary>
        public List<IndexData> index { set; get; }
    }
}
