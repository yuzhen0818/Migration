using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ezacquire.migration.Utility.Models
{
    public class DocumentIndexPartial
    {
        public string ScanType { get; set; }
        public string DocumentType { get; set; }
        public string FormID { get; set; }
        public string ScanCaseId { get; set; }
        public string BatchNumber { get; set; }
        public string CreateDateTime { get; set; }
        public string FilingNumber { get; set; }
        public string ScanUserId { get; set; }
        public string ScanDateTime { get; set; }
        public string VerifyUserId { get; set; }
        public string VerifyDateTime { get; set; }
        public string ScanStation { get; set; }
        public string CommitServer { get; set; }
        public string MimeType { get; set; }
        public string MimeTypeDetail { get; set; }
        public string UploadDateTime { get; set; }
        public List<IndexData> IndexData { get; set; }
    }
}
