using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ezacquire.migration.Utility
{
    public class Commons
    {
        public static bool IsHolidays(DateTime date)
        {
            // 週休二日
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                return true;
            }

            string[] holidays = ConfigurationManager.AppSettings["Holidays"].Split(',');

            // 國定假日(國曆)
            foreach (var holiday in holidays)
            {
                if (date.ToString("MMdd").Equals(holiday))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
