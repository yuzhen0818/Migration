using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ezacquire.migration.Utility.Models
{
    public class DocumentAdd
    {
        public DocumentIndexPartial DocumentIndex { get; set; }

        public List<List<FileItem>> Files { get; set; }

        public OperatorInfo OperatorInfo { get; set; }
    }
}
