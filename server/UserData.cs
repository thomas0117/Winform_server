using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace server
{
    public class User_acc
    {
        public int user_id;
        public String login_name;
        public String login_pwd;
        public String sys_name;
        public String sys_pwd;
        public String sys_no;
        public String user_name_true;
        public String user_reg_time;
        public int user_level;
        public int user_type;
        public int is_online;
        public int is_test;
        public String manager_permission;
        public String login_ip;
        public String login_time;
        public String service_staff;
        public String service_hotline;
        public int money_default;
        public int money_balance;
        public int money_open;
        public int money_today;

        public User_acc(String[] userdata)
        {
            user_id = Convert.ToInt32(userdata[0]);
            login_name = userdata[1];
            login_pwd = userdata[2];
            sys_name = userdata[3];
            sys_pwd = userdata[4];
            sys_no = userdata[5];
            user_name_true = userdata[6];
            user_reg_time = userdata[7];
            user_level = Convert.ToInt32(userdata[8]);
            user_type = Convert.ToInt32(userdata[9]);
            is_online = Convert.ToInt32(userdata[10]);
            is_test = Convert.ToInt32(userdata[11]);
            manager_permission = userdata[12];
            login_ip = userdata[13];
            login_time = userdata[14];
            service_staff = userdata[15];
            service_hotline = userdata[16];
            money_default = Convert.ToInt32(userdata[17]);
            money_balance = Convert.ToInt32(userdata[18]);
            money_open = Convert.ToInt32(userdata[19]);
            money_today = Convert.ToInt32(userdata[20]);
        }
    }

}
