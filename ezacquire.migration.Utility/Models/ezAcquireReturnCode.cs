using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ezacquire.migration.Utility.Models
{
    public class ezAcquireReturnCode
    {
        public string Status { get; set; }
        public string Result { get; set; }
        public Error Error { get; set; }
    }
    public class Error
    {
        public string ErrorId { get; set; }
        public string Message { get; set; }
    }
}
