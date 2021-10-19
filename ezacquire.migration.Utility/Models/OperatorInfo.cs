using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ezacquire.migration.Utility.Models
{
    public class OperatorInfo
    {
        public string OperatorUserId { get; set; }

        public string ClientIPAddress { get; set; }

        public string SystemId { get; set; }
        public string ServerIPAddress { get; set; }
        public string TransactionId { get; set; }
    }
}
