using ezLib.Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
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

        /// <summary>
        /// 創建資料夾
        /// </summary>
        /// <param name="directory"></param>
        public static bool RecreateDirectory(string directory)
        {
            bool result = true;
            if (Directory.Exists(directory))
            {
                try
                {
                    Directory.Delete(directory, true);
                }
                catch (Exception ex)
                {
                    result = false;
                    ExceptionLogger.Write(ex, "刪除資料夾發生錯誤 >> " + directory + "。");
                }
            }

            if (!Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception ex)
                {
                    result = false;
                    ExceptionLogger.Write(ex, "建立資料夾發生錯誤 >> " + directory + "。");
                }
            }
            return result;
        }
    }
}
