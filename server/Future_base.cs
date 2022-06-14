using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace server
{
    public class Future_base
    {
        public int future_id;
        public String future_source;
        public String future_name;
        public int future_short;
        public int future_code;
        public String open_time;
        public String close_time;
        public String last_day_trade;
        public String last_day_close_time;
        public int is_pass_day;
        public String non_trading_days;
        public String see_time;
        public int is_show;
        public int decimal_num;
        public int trade_cost;
        public int stay_cost;
        public int future_price;
        public int single_order_max_num;
        public int total_order_max_num;
        public int max_stay_day;
        public int max_stay_order_num;
        public int max_stay_future;
        public String enable_trade_type;
        public int enable_buy_type;
        public float stop_trade_precent;
        public float trade_precent;
        public float auto_sell_precent;
        public int sell_wait_time;
        public int lowest_price;
        public String is_enabled;


        public Future_base(String[] arr)
        {
            future_id = Convert.ToInt32(arr[0]);
            future_source = arr[1];
            future_name = arr[2];
            future_short = Convert.ToInt32(arr[3]);
            future_code = Convert.ToInt32(arr[4]);
            open_time = arr[5];
            close_time = arr[6];
            last_day_trade = arr[7];
            last_day_close_time = arr[8];
            is_pass_day = Convert.ToInt32(arr[9]);
            non_trading_days = arr[10];
            see_time = arr[11];
            is_show = Convert.ToInt32(arr[12]);
            decimal_num = Convert.ToInt32(arr[13]);
            trade_cost = Convert.ToInt32(arr[14]);
            stay_cost= Convert.ToInt32(arr[15]);
            future_price= Convert.ToInt32(arr[16]);
            single_order_max_num= Convert.ToInt32(arr[17]);
            total_order_max_num= Convert.ToInt32(arr[18]);
            max_stay_day= Convert.ToInt32(arr[19]);
            max_stay_order_num= Convert.ToInt32(arr[20]);
            max_stay_future= Convert.ToInt32(arr[21]);
            enable_trade_type=arr[22];
            enable_buy_type= Convert.ToInt32(arr[23]);
            stop_trade_precent = Convert.ToSingle(arr[24]);
            trade_precent = Convert.ToSingle(arr[25]);
            auto_sell_precent = Convert.ToSingle(arr[26]);
            sell_wait_time= Convert.ToInt32(arr[27]);
            lowest_price= Convert.ToInt32(arr[28]);
            String is_enabled=arr[29];
        }
    }

}
