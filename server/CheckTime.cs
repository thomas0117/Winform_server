using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace server
{
    public static class CheckTime
    {
        //檢查海外是否在報價時段
        public static bool Check_OSTime()
        {
            if ((DateTime.Now.CompareTo(Convert.ToDateTime("00:00")) >= 0) && (DateTime.Now.CompareTo(Convert.ToDateTime("06:00")) <= 0))
            {
                return !IsHoliday(DateTime.Now.AddDays(-1));
            }
            else if (DateTime.Now.CompareTo(Convert.ToDateTime("07:00")) >= 0)
            {
                return !IsHoliday(DateTime.Now);
            }
            else
            {
                return false;
            }
        }

        //檢查國內是否在報價時段
        public static bool Check_LocalTime()
        {
            if ((DateTime.Now.CompareTo(Convert.ToDateTime("08:40"))>=0 )&& (DateTime.Now.CompareTo(Convert.ToDateTime("13:45")) <=0))
            {
                return !IsHoliday(DateTime.Now);
            }
            else
            {
                return false;
            }
        }

        //檢查是否為假日
        public static bool IsHoliday(DateTime date)
        {
            TaiwanLunisolarCalendar tlc = new TaiwanLunisolarCalendar();
            String Taiwan_Date = tlc.GetMonth(date).ToString() + '/' + tlc.GetDayOfMonth(date).ToString();
            String[] Holiday_Country = { "01/01", "02/28", "04/04", "04/05", "05/01", "10/10" };
            String[] Holiday_Taiwan = { "1/1", "1/2", "1/3", "1/4", "1/5", "5/5", "8/15", "12/" + tlc.GetDaysInMonth(tlc.GetYear(date), tlc.GetMonth(date)) };
            
            // 周休二日
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek==DayOfWeek.Sunday) {
                return true;
            }

            //國曆假日
            foreach (String h_c in Holiday_Country)
            {
                if (date.ToString("MM/dd").Equals(h_c))
                {
                    return true;
                }
            }

            //農曆假日
            foreach (String h_t in Holiday_Taiwan)
            {
                if (Taiwan_Date.Equals(h_t))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
