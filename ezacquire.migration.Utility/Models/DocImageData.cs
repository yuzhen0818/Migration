using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ezacquire.migration.Utility.Models
{
    public class DocImageData
    {
        public string DIM_CommitTime { set; get; }
        public string DIM_ScanTime { set; get; }
        public string DIM_VerifyTime { set; get; }
        public string DIM_Batch { set; get; }
        public string FillingSerial { set; get; }
        public string DepartCode { set; get; }
        public string PolicyDate { set; get; }
        public string CloseDate { set; get; }
        private string F_ENTRYDATE { set; get; }
        public string CreateDateTime{
            set {

                DateTime baseDate = new DateTime(1970, 1, 1);
                DateTime createdate = new DateTime(1970, 1, 1);
                try
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (double.TryParse(value, out double days))
                            createdate = baseDate.AddDays(days);
                    }
                    F_ENTRYDATE = createdate.ToString("yyyy/MM/dd HH:mm:ss");
                }
                catch(Exception ex)
                {
                    F_ENTRYDATE = "";
                    throw ex;
                }
            }
            get {
                return F_ENTRYDATE;
            }
        }

        public DocImageData()
        {
            DIM_CommitTime = "";
            DIM_ScanTime = "";
            DIM_VerifyTime = "";
            DIM_Batch = "";
            FillingSerial = "";
            DepartCode = "";
            F_ENTRYDATE = "";
            PolicyDate = "";
            CloseDate = "";
        }
    }
}
