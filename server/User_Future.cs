using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace server
{
    class User_Future
    {
        public int uf_id;
        public int user_id;
        public int future_id;
        public String future_num;
        public String future_name;
        public int trade_cost;
        public int out_cost;
        public int stay_cost;
        public int future_price;
        public int single_order_max_num;
        public int total_order_max_num;
        public int max_stay_day;
        public int max_stay_order_num;
        public int max_stay_future_num;
        public String enable_trade_type;
        public int enable_buy_type;
        public int future_status;
        public String status_change_time;
        public int stop_trade_percent;
        public int trade_percent;
        public String auto_sell_percent;
        public Double sell_wait_time;
        public int lowest_price;
        public String cost_type;
        public Double cost_base;
        public int is_enabled;


        public User_Future(String[] arr)
        {
                uf_id = Convert.ToInt32(arr[0]);
                user_id = Convert.ToInt32(arr[1]);
                future_id = Convert.ToInt32(arr[2]);
                future_num = arr[3];
                future_name = arr[4];
                trade_cost = Convert.ToInt32(arr[5]);
                out_cost = Convert.ToInt32(arr[6]);
                stay_cost = Convert.ToInt32(arr[7]);
                future_price = Convert.ToInt32(arr[8]);
                single_order_max_num = Convert.ToInt32(arr[9]);
                total_order_max_num = Convert.ToInt32(arr[10]);
                max_stay_day = Convert.ToInt32(arr[11]);
                max_stay_order_num = Convert.ToInt32(arr[12]);
                max_stay_future_num = Convert.ToInt32(arr[13]);
                enable_trade_type = arr[14];
                enable_buy_type = Convert.ToInt32(arr[15]);
                future_status = Convert.ToInt32(arr[16]);
                status_change_time = arr[17];
                stop_trade_percent = Convert.ToInt32(arr[18]);
                trade_percent = Convert.ToInt32(arr[19]);
                auto_sell_percent = arr[20];
                sell_wait_time = Convert.ToInt32(arr[21]);
                lowest_price = Convert.ToInt32(arr[22]);
                cost_type = arr[23];
                cost_base = Convert.ToInt32(arr[24]);
                is_enabled = Convert.ToInt32(arr[25]);           
        }
    }
}
