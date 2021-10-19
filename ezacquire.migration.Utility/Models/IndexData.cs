using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ezacquire.migration.Utility.Models
{
    public class IndexData : IEquatable<IndexData>
    {
        public string Key { get; set; }
        public List<string> Value { get; set; }

        public IndexData()
        {
            this.Value = new List<string>();
        }

        public IndexData(string index)
        {
            this.Key = index;
            this.Value = new List<string>();
        }

        public bool Equals(IndexData other)
        {
            if (this.Key == other.Key)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
