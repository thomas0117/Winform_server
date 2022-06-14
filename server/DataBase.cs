using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Data;
//加入mysql 類別庫
using MySql.Data;
using MySql.Data.MySqlClient;



namespace server
{
    class ConnectDatabase
    {

        private string serverAddress;
        private string databaseName;
        private string userID;
        private string password;
        private MySqlConnection MysqlConnection;
        public int MySql_AliveCount = 0;


        public ConnectDatabase()
        {
            Initialize();
        }


        //資料初始化
        private void Initialize()
        {
            serverAddress = "203.124.11.62";
            databaseName = "web_stocks_client";
            userID = "thomas789";
            password = "12345678";
            string connectionString;
            connectionString = "SERVER=" + serverAddress + ";DATABASE=" + databaseName + ";UID=" + userID + ";PWD=" + password + ";charset = utf8";
            MysqlConnection = new MySqlConnection(connectionString);

            //connection = ConnectionOpen();
        }

        //開啟連結到資料庫
        private bool ConnectionOpen()
        {
            if (MySql_AliveCount > 10)
            {
                return false;
            }
            try
            {
                MysqlConnection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                //例外處理，常見的兩種錯誤
                //ex.Number=0:無法連接到伺服器.
                //ex.Number=1045: 無效的使用者名稱或密碼.
                switch (ex.Number)
                {
                    case 0:
                        myLog.Write("無法連接到伺服器");
                        break;
                    case 1042:
                        myLog.Write("無效的主機名稱");
                        break;
                    case 1045:
                        myLog.Write("使用者名稱/密碼錯誤");
                        break;
                }
                MySql_AliveCount++;
                return false;
            }
        }

        //關閉連結
        public bool ConnectionClose()
        {
            try
            {
                MysqlConnection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                myLog.Write(ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                myLog.Write(ex.Message);
                return false;
            }
        }

        //新增語法
        public void F_SQL_Add(String TableName, String ColumnName, String Values)
        {
            if (ConnectionOpen() == true)//開啟連結
            {
                try
                {
                    MySqlCommand command = MysqlConnection.CreateCommand();
                    command.CommandText = "Insert into " + TableName + "(" + ColumnName + ")" + " values(" + Values + ")";
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    myLog.Write(ex.ToString());
                }
                this.ConnectionClose();//關掉連結
            }
        }

        //執行查詢語法，回傳搜尋目標是否存在
        public bool F_SQL_Select(String TableName, String command)
        {
            if (ConnectionOpen() == true)//開啟連結
            {
                try
                {
                    String cmdText = "SELECT * FROM " + TableName + " WHERE " + command;
                    MySqlCommand cmd = new MySqlCommand(cmdText, MysqlConnection);
                    MySqlDataReader reader = cmd.ExecuteReader(); //execure the reader
                    while (reader.Read())
                    {
                        cmdText = reader.GetString(0);
                        this.ConnectionClose();//關掉連結
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    myLog.Write(ex.Message);
                    this.ConnectionClose();     //關掉連結
                    return false;
                }

                this.ConnectionClose();//關掉連結
            }
            return false;
        }

        //回傳要查詢的成員的所有data
        public String[] F_SQL_Select_ReturnData(String TableName, String ColumnName, String Operator, String Values)
        {
            if (ConnectionOpen() == true)//開啟連結
            {
                try
                {
                    String cmdText = "SELECT * FROM " + TableName + " WHERE " + ColumnName + " " + Operator + Values;
                    MySqlCommand cmd = new MySqlCommand(cmdText, MysqlConnection);
                    MySqlDataReader reader = cmd.ExecuteReader(); //execure the reader
                    String[] arr = new String[reader.FieldCount];
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            try
                            {
                                arr[i] = reader.GetString(i);
                            }
                            catch
                            {

                            }
                        }
                        this.ConnectionClose();//關掉連結
                        return arr;
                    }
                }
                catch (Exception ex)
                {
                    myLog.Write(ex.Message);
                    this.ConnectionClose();//關掉連結
                    return null;
                }

                this.ConnectionClose();//關掉連結              
            }
            return null;
        }

        //刪除語法
        public void F_SQL_Del(String TableName, String ColumnName, String Values)
        {
            if (ConnectionOpen() == true)//開啟連結
            {
                try
                {
                    MySqlCommand command = MysqlConnection.CreateCommand();
                    command.CommandText = "Delete FROM " + TableName + " WHERE " + ColumnName + "=" + Values;
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    myLog.Write(ex.Message);
                }
                this.ConnectionClose();//關掉連結
            }
        }


        //登入
        public bool F_SQL_Login(String SQL_command)
        {
            if (ConnectionOpen() == true)//開啟連結
            {
                try
                {
                    MySqlCommand command = new MySqlCommand(SQL_command, MysqlConnection);
                    command.CommandText = SQL_command;
                    //command.ExecuteNonQuery();
                    MySqlDataReader reader = command.ExecuteReader(); //execure the reader
                    while (reader.Read())
                    {
                        this.ConnectionClose();//關掉連結
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    myLog.Write(ex.Message);
                    this.ConnectionClose();     //關掉連結
                }
                this.ConnectionClose();//關掉連結
            }
            return false;
        }


        public void F_SQL_CreateTable_QuoteData(String TableName)
        {
            if (ConnectionOpen() == true)
            {
                string SQL_command = "CREATE TABLE IF NOT EXISTS " + TableName +
                            "(Time varchar(20) NOT NULL ," +
                            "StockNo varchar (20) NOT NULL," +
                            "StockName varchar(20) NOT NULL," +
                            "Ref int ," +
                            "TickQty int ," +
                            "FutureOI int ," +
                            "Open int ," +
                            "High int ," +
                            "Low int ," +
                            "Close int ," +
                            "TQty int ," +
                            "Bid int ," +
                            "Bc int ," +
                            "Ask int ," +
                            "Ac int ,"+
                            "Deci int, "+
                            "MyKey varchar(30) )";
                try
                {
                    MySqlCommand command = new MySqlCommand(SQL_command, MysqlConnection);
                    command.CommandText = SQL_command;
                    command.ExecuteNonQuery();
                    
                }
                catch (MySqlException ex)
                {
                    myLog.Write(ex.Message);
                }
                this.ConnectionClose();//關掉連結
            }
        }

        //找到並回傳使用者未成交委託    state=0則全新的單 state=3 為執行過但還未完成的單
        public List<Pre_Order> F_SQL_SelectPre_Order(int state)
        {
            List<Pre_Order> PO_list = new List<Pre_Order>();
            Pre_Order PO = null;

            if (ConnectionOpen() == true)//開啟連結
            {
                try
                {
                    String cmdText = "SELECT * FROM  pre_order WHERE  state = "+state.ToString();
                    MySqlCommand cmd = new MySqlCommand(cmdText, MysqlConnection);
                    MySqlDataReader reader = cmd.ExecuteReader(); //execure the reader
                    while (reader.Read())
                    {
                        String[] arr = new String[reader.FieldCount];
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            try
                            {
                                arr[i] = reader.GetString(i);
                            }
                            catch (Exception ee)
                            {
                                myLog.Write(ee.ToString());
                            }
                        }

                        PO = new Pre_Order(arr);
                        if (PO != null)
                        {
                            PO_list.Add(PO);
                        }
                    }
                }
                catch (Exception ex)
                {
                    myLog.Write(ex.Message);
                }

                this.ConnectionClose();//關掉連結
                return PO_list;
            }
            return PO_list;
        }

        //自定義語法
        public void F_SQL_command(String SQL_command)
        {
            if (ConnectionOpen() == true)//開啟連結
            {
                try
                {
                    MySqlCommand command = new MySqlCommand(SQL_command, MysqlConnection);
                    command.CommandText = SQL_command;
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    myLog.Write(ex.Message);
                }
                this.ConnectionClose();//關掉連結
            }
        }

        public int F_SQL_Add_ReturnID(String TableName, String ColumnName, String Values)
        {
            if (ConnectionOpen() == true)//開啟連結
            {
                int i = 0;
                try
                {
                    MySqlCommand command = MysqlConnection.CreateCommand();
                    command.CommandText = "Insert into " + TableName + "(" + ColumnName + ")" + " values(" + Values + "); SELECT @@IDENTITY;";
                    i = Convert.ToInt32(command.ExecuteScalar());
                }
                catch (Exception ex)
                {
                    myLog.Write(ex.ToString());
                }
                this.ConnectionClose();//關掉連結
                return i;
            }
            return 0;
        }

        //回傳要查詢的欄位所有data
        public List<User_acc> F_SQL_Select_User_acc()
        {
            List<User_acc> user_list = new List<User_acc>();
            User_acc user = null;

            if (ConnectionOpen() == true)//開啟連結
            {
                try
                {
                    String cmdText = "SELECT * FROM user_acc";
                    MySqlCommand cmd = new MySqlCommand(cmdText, MysqlConnection);
                    MySqlDataReader reader = cmd.ExecuteReader(); //execure the reader
                    while (reader.Read())
                    {
                        String[] arr = new String[reader.FieldCount];
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            try
                            {
                                arr[i] = reader.GetString(i);
                            }
                            catch (Exception ee)
                            {
                                //myLog.Write(ee.ToString());
                            }
                        }

                        user = new User_acc(arr);
                        if (user != null)
                        {
                            user_list.Add(user);
                        }
                    }
                }
                catch (Exception ex)
                {
                    myLog.Write(ex.Message);
                }

                this.ConnectionClose();//關掉連結
                return user_list;
            }
            return user_list;
        }

        //找出此使用者所有訂單    
        public List<Order_Entrust> F_SQL_SelectEntrust(int userid)
        {
            List<Order_Entrust> OE_list = new List<Order_Entrust>();
            Order_Entrust OE = null;

            if (ConnectionOpen() == true)//開啟連結
            {
                try
                {
                    String cmdText = "SELECT * FROM  order_entrust WHERE  user_id = " + userid.ToString();
                    MySqlCommand cmd = new MySqlCommand(cmdText, MysqlConnection);
                    MySqlDataReader reader = cmd.ExecuteReader(); //execure the reader

                    if (reader.FieldCount == 0) { return OE_list; }

                    while (reader.Read())
                    {
                        String[] arr = new String[reader.FieldCount];
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            try
                            {
                                arr[i] = reader.GetString(i);
                            }
                            catch (Exception ee)
                            {
                                //myLog.Write(ee.ToString());
                            }
                        }

                        OE = new Order_Entrust(arr);
                        if (OE != null)
                        {
                            OE_list.Add(OE);
                        }
                    }
                }
                catch (Exception ex)
                {
                    myLog.Write(ex.Message);
                }

                this.ConnectionClose();//關掉連結
                return OE_list;
            }
            return OE_list;
        }


        public String F_SQL_command_return_string(String SQL_command)
        {
            if (ConnectionOpen() == true)//開啟連結
            {
                try
                {
                    MySqlCommand command = new MySqlCommand(SQL_command, MysqlConnection);
                    MySqlDataReader reader = command.ExecuteReader(); //execure the reader
                    String s="";
                    while (reader.Read())
                    {
                        s = reader.GetString(0);
                    }
                    this.ConnectionClose();//關掉連結
                    return s;
                }
                catch (Exception ex)
                {
                    
                }
                this.ConnectionClose();//關掉連結
                return "";
            }
            return "";
        }

    }
}