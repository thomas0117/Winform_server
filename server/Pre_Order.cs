using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace server
{
    public class Pre_Order
    {
        public int pre_id;
        public int user_id;
        public int future_id;
        public String future_source;
        public String futrue_name;
        public int buy_type;
        public Double order_price;
        public int order_num;
        public Double entry_price;
        public DateTime order_time;
        public DateTime entry_time;
        public int type_order;
        public int is_day_trade;
        public int state;

        public Pre_Order(String[] arr)
        {

            pre_id = Convert.ToInt32(arr[0]);
            user_id=Convert.ToInt32(arr[1]);
            future_id=Convert.ToInt32(arr[2]);
            future_source = arr[3];
            futrue_name=arr[4];
            buy_type=Convert.ToInt32(arr[5]);
            order_price = Convert.ToDouble(arr[6]);
            order_num=Convert.ToInt32(arr[7]);
            entry_price=Convert.ToDouble(arr[8]);
            order_time=Convert.ToDateTime(arr[9]);
            entry_time=Convert.ToDateTime(arr[10]);
            type_order=Convert.ToInt32(arr[11]);
            is_day_trade = Convert.ToInt32(arr[12]);
            state=Convert.ToInt32(arr[13]);
        }

    }
}
