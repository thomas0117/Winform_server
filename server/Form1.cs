using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace server
{
    public partial class Form1 : Form
    {
        delegate void delegate_void();
        public int m_getservertime_count = 0;
        Socket serversocket;
        IPAddress m_IPA = new IPAddress(Dns.GetHostByName(Dns.GetHostName()).AddressList[0].Address);
        String localip;
        int port = 3766;
        Thread runthread;
        Thread Thread_Select_Pre;
        List<MySocket> clientlist = new List<MySocket>();
        DateTime servertime;
        int m_send_A_count = 0;
        ConnectDatabase Server_CD = new ConnectDatabase();
        List<User_acc> List_User_acc = new List<User_acc>();


        //國內報價變數
        FOnNotifyConnection fOnNotifyConnection;
        FOnNotifyQuote fOnNotifyQuote;
        FOnNotifyBest5 fOnNotifyBest5;
        public int m_L_Code;
        public bool m_L_Connect = false;
        public bool m_L_Quote_15s_flag = true;                      //15秒內都有報價則為true
        public int m_L_countline = 0;
        public bool m_L_IsQuoteTime = false;
        public int m_L_ReEnterMonitor_count = 0;
        public bool m_L_ReadyToRequest = false;
        public int m_L_ReadyToRequest_count = 0;
        public bool m_L_FirstGetStock = true;
        public String m_L_RequestSTOCK = "";
        public int m_L_Best5_nPageNo = 0;
        ConnectDatabase L_CD = new ConnectDatabase();
        List<STOCK> stocklist = new List<STOCK>();

        //海外報價變數
        OSFOnConnect OSfOnConnect;
        OSFOnGetStockIdx OSfOnNotifyQuote;
        OSFOnGetStockIdx OSfOnNotifyBest5;
        public int m_OS_Code;
        public bool m_OS_Connect = false;
        public bool m_OS_Quote_15s_flag = true;                    //15秒內都有報價則為true
        public int m_OS_countline = 0;
        public bool m_OS_IsQuoteTime = false;
        public int m_OS_ReEnterMonitor_count = 0;
        public bool m_OS_ReadyToRequest = false;
        public int m_OS_ReadyToRequest_count = 0;
        public bool m_OS_OverseaProducts_flag = true;
        public bool m_OS_FirstGetStock = true;
        public String m_OS_RequestSTOCK = "";
        public int m_OS_Best5_nPageNo = 0;
        ConnectDatabase OS_CD = new ConnectDatabase();
        List<FOREIGN> foreignlist = new List<FOREIGN>();

        //表單
        #region Form
        public Form1()
        {
            InitializeComponent();
            //F_Update_User_money_open();

            RichTextBox.CheckForIllegalCrossThreadCalls = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            localip = m_IPA.ToString();
            this.Text = "IP:" + localip + "port:" + port;

            runthread = new Thread(run);
            runthread.Start();
            runthread.IsBackground = true;


            //---------國內報價server---------
            fOnNotifyConnection = new FOnNotifyConnection(OnNotifyConnection);
            GC.KeepAlive(fOnNotifyConnection);
            GC.SuppressFinalize(fOnNotifyConnection);

            fOnNotifyQuote = new FOnNotifyQuote(OnNotifyQuote);
            GC.KeepAlive(fOnNotifyQuote);
            GC.SuppressFinalize(fOnNotifyQuote);

            fOnNotifyBest5 = new FOnNotifyBest5(OnNotifyBest5);
            GC.KeepAlive(fOnNotifyBest5);
            GC.SuppressFinalize(fOnNotifyBest5);

            iniStockserver();

            Functions.SKQuoteLib_AttachConnectionCallBack(fOnNotifyConnection);
            Functions.SKQuoteLib_AttachQuoteCallBack(fOnNotifyQuote);
            Functions.SKQuoteLib_AttachBest5CallBack(fOnNotifyBest5);

            //---------海外報價server---------
            OSfOnConnect = new OSFOnConnect(OSOnConnect);
            GC.KeepAlive(OSfOnConnect);
            GC.SuppressFinalize(OSfOnConnect);

            OSfOnNotifyQuote = new OSFOnGetStockIdx(OSOnNotifyQuote);
            GC.KeepAlive(OSfOnNotifyQuote);
            GC.SuppressFinalize(OSfOnNotifyQuote);

            OSfOnNotifyBest5 = new OSFOnGetStockIdx(OSOnNotifyBest5);
            GC.KeepAlive(OSfOnNotifyBest5);
            GC.SuppressFinalize(OSfOnNotifyBest5);

            OSiniStockserver();


            OSFunctions.SKOSQuoteLib_AttachConnectCallBack(OSfOnConnect);
            OSFunctions.SKOSQuoteLib_AttachQuoteCallBack(OSfOnNotifyQuote);
            OSFunctions.SKOSQuoteLib_AttachBest5CallBack(OSfOnNotifyBest5);


            //TimerStart
            Time_Minute.Start();
            ServerTime.Start();
            Time_15s.Start();
            timer1.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (serversocket != null)
                {
                    serversocket.Close();
                }
                if (runthread != null)
                {
                    runthread.Abort();
                }
                if (Thread_Select_Pre != null)
                {
                    Thread_Select_Pre.Abort();
                }

                myLog.Write("server Close");
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
            catch (Exception ee)
            {
                F_ShowAndWrite(ee.ToString());
            }
        }

        private void Time_15s_Tick(object sender, EventArgs e)
        {
            F_CheckConnection();            //檢查連線狀態
            if (label5.ForeColor == Color.Green)
            {
                //檢查國內報價狀態
                if (m_L_IsQuoteTime == true)
                {
                    F_L_CheckQuoteAlive();
                }
                //檢查海外報價狀態
                if (m_OS_IsQuoteTime == true)
                {
                    F_OS_CheckQuoteAlive();
                }
            }

            Thread test = new Thread(F_Refresh_User_list);
            test.IsBackground = true;
            test.Start();
        }

        private void ServerTime_Tick(object sender, EventArgs e)
        {
            m_send_A_count++;
            //取得Server時間
            F_GetServerTime();

            //如果隔日 要做的事情
            if (DateTime.Compare(DateTime.Now, Convert.ToDateTime("00:00:00")) >= 0 && DateTime.Compare(DateTime.Now, Convert.ToDateTime("00:00:01")) <= 0)
            {
                F_Change_OE_is_today();
            }

            if (label5.ForeColor == Color.Green)
            {

                //海外報價時間檢查
                if (CheckTime.Check_OSTime())
                {
                    label7.ForeColor = Color.Green;
                    m_OS_IsQuoteTime = true;
                }
                else
                {
                    label7.ForeColor = Color.Red;
                    m_OS_IsQuoteTime = false;
                }

                //國內報價時間檢查
                if (CheckTime.Check_LocalTime())
                {
                    label6.ForeColor = Color.Green;
                    m_L_IsQuoteTime = true;
                }
                else
                {
                    label6.ForeColor = Color.Red;
                    m_L_IsQuoteTime = false;
                }

                //國內執行Request
                if (m_L_ReadyToRequest == true)
                {
                    m_L_ReadyToRequest_count++;
                    if (m_L_ReadyToRequest_count >= 3)
                    {
                        if (m_L_FirstGetStock == true)              //只有第一次啟動程式會執行
                        {
                            m_L_FirstGetStock = false;
                            F_L_GetRequestSTOCK();                  //取得需要的國內商品
                            //F_L_CreateQuoteDB();                    //把需要的商品建立DATATABLE                           
                        }
                        requestStock();
                        requestBest5();
                        m_L_ReadyToRequest_count = 0;
                        m_L_ReadyToRequest = false;
                        Time_15s.Start();
                    }
                }

                //海外執行Request
                if (m_OS_ReadyToRequest == true)
                {
                    m_OS_ReadyToRequest_count++;
                    if (m_OS_ReadyToRequest_count >= 3)
                    {
                        if (m_OS_FirstGetStock == true)             //只有第一次程式會執行
                        {
                            m_OS_FirstGetStock = false;
                            F_OS_GetRequestSTOCK();                 //取得需要的海外商品
                            //F_OS_CreateQuoteDB();                   //把需要的商品建立DATATABLE
                            Thread_Select_Pre = new Thread(F_Select_New_Pre_Order);
                            Thread_Select_Pre.Start();
                            Thread_Select_Pre.IsBackground = true;

                            //執行已執行過但未完成的委託
                            Thread pre_order_thread = new Thread(F_Select_Undone_pre_Oreder);
                            pre_order_thread.IsBackground = true;
                            pre_order_thread.Start();
                        }
                        OSrequestStock();
                        OSrequestBest5();
                        m_OS_ReadyToRequest_count = 0;
                        m_OS_ReadyToRequest = false;
                    }
                }

                //資料庫關閉時能夠自動停止使用資料庫機制
                if (L_CD.MySql_AliveCount > 10 || OS_CD.MySql_AliveCount > 10 || Server_CD.MySql_AliveCount > 10)
                {
                    checkBox1.Checked = false;
                    L_CD.MySql_AliveCount = 0;
                    OS_CD.MySql_AliveCount = 0;
                    Server_CD.MySql_AliveCount = 0;
                }

                //廣播A
                if (m_send_A_count >= 20)
                {
                    broadCast("A,\n");
                    m_send_A_count = 0;
                }

                label9.Text = clientlist.Count.ToString();
                lbl_LocalCount.Text = m_L_countline.ToString();
                label4.Text = m_OS_countline.ToString();
            }
        }

        private void Time_Minute_Tick(object sender, EventArgs e)
        {
            if (label5.ForeColor == Color.Green)
            {
                if (m_L_IsQuoteTime == true || m_OS_IsQuoteTime == true)
                {
                    RestartQuote_Time();
                }
            }
        }

        public void F_ShowAndWrite(String s)
        {
            richTextBox1.AppendText(s + '\n');
            myLog.Write(s);
        }

        #endregion

        //伺服器連線相關
        #region serversocket
        public void run()
        {
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, port);

            serversocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serversocket.Bind(ipep);
            serversocket.Listen(1000);                              //最多可以1000個連線

            myLog.Write("server start");
            richTextBox1.AppendText("主機:" + localip + "\n");
            richTextBox1.AppendText("prot:" + port + "\n");
            while (true)
            {
                MySocket client = new MySocket(serversocket.Accept());
                richTextBox1.AppendText("接受一個新連線!" + "\n");
                myLog.Write(client.remoteEndPoint.ToString() + " 已連線");
                try
                {
                    

                    foreach (STOCK pSTOCK in stocklist)
                    {

                        String sline = DateTime.Now.ToString("yyyyMMdd-HH:mm:ss") + ',' + pSTOCK.m_caStockNo + ',' + pSTOCK.m_caName + ',' + pSTOCK.m_nRef + ',' + pSTOCK.m_nTickQty + ','
                        + pSTOCK.m_nFutureOI + ',' + pSTOCK.m_nOpen + ',' + pSTOCK.m_nHigh + ',' + pSTOCK.m_nLow + ',' + pSTOCK.m_nClose + ',' + pSTOCK.m_nTQty +
                        ',' + pSTOCK.m_nBid + ',' + pSTOCK.m_nBc + ',' + pSTOCK.m_nAsk + ',' + pSTOCK.m_nAc + ',' + pSTOCK.m_sDecimal + ','
                        + pSTOCK.m_sStockidx.ToString() + ',' + '\n';
                        client.send(sline);
                    }
                    foreach (FOREIGN pFOREIGN in foreignlist)
                    {

                        String fline = DateTime.Now.ToString("yyyyMMdd-HH:mm:ss") + ',' + pFOREIGN.m_caStockNo + ',' + pFOREIGN.m_caStockName + ',' + pFOREIGN.m_nRef + ',' +
                        pFOREIGN.m_nTickQty + ',' + "0" + ',' + pFOREIGN.m_nOpen + ',' + pFOREIGN.m_nHigh + ',' + pFOREIGN.m_nLow + ',' + pFOREIGN.m_nClose
                        + ',' + pFOREIGN.m_nTQty + ',' + pFOREIGN.m_nBid + ',' + pFOREIGN.m_nBc + ',' + pFOREIGN.m_nAsk + ',' + pFOREIGN.m_nAc + ',' +
                        pFOREIGN.m_sDecimal + ',' + "60" + pFOREIGN.m_sStockidx.ToString() + ',' + '\n';
                        client.send(fline);

                    }

                    client.listener(login, handlemsg, F_GetBest5);
                    clientlist.Add(client);
                }
                catch (Exception ee)
                {
                    //richTextBox1.AppendText(ee.ToString() + "\n");
                    myLog.Write(ee.ToString());
                }
            }
        }

        public String handlemsg(String msg)                     //處理client傳來的訊息
        {
            if (msg != "")
            {
                richTextBox1.AppendText(msg + '\n');
            }
            return msg;
        }

        public String login(String msg)                         //處理client傳來的帳號密碼
        {
            String ID = msg.Substring(1, msg.IndexOf('@') - 1);        //取得client傳來的帳號 從字串到@前為帳號 @後為密碼
            String Password = msg.Substring(msg.IndexOf('@') + 1);   //取得密碼

            try
            {
                foreach (MySocket s in clientlist)
                {
                    if (s.UserID == ID)
                    {
                        s.send("double_connect");
                        F_ShowAndWrite(s.remoteEndPoint + " 重複登入已被結束連線");
                        s.isdead = true;
                        s.logsuccess = false;
                        s.socket.Close();
                        clientlist.Remove(s);
                        return ID;
                    }
                }
                return ID;
            }
            catch (Exception ee)
            {
                F_ShowAndWrite(ee.ToString());
            }

            return ID;
        }

        //client要登出時的function (暫時沒用)
        //public void F_Logout(String id)
        //{
        //    int i = 0;
        //    int n = 0;
        //    bool b = false;
        //    foreach (MySocket s in clientlist)
        //    {
        //        if (s.UserID == id)
        //        {
        //            n = i;
        //            b = true;
        //            s.send("logout_logout");
        //            s.isdead = true;
        //        }
        //        i++;
        //    }
        //    if (b)
        //    {
        //        clientlist.RemoveAt(n);
        //    }
        //}

        //廣播系統
        public void broadCast(string msg)
        {
            bool m_remove = false;
            int i = 0;
            int n = 0;
            try
            {
                foreach (MySocket client in clientlist)
                {
                    if (!client.isdead && client.logsuccess)                    //client還沒斷線且登入成功才傳遞資料
                    {
                        //richTextBox1.AppendText("Send to " + client.remoteEndPoint.ToString() + ":" + msg);

                        client.send(msg);
                    }
                    else if (client.isdead)
                    {
                        n = i;
                        m_remove = true;
                        client.socket.Close();
                    }
                    i++;
                }
                if (m_remove)
                {
                    clientlist.RemoveAt(n);
                }
            }
            catch (Exception ee)
            {
                F_ShowAndWrite(ee.ToString());
            }

        }

        //Ping IP
        public bool PingIP(String ip)
        {
            try
            {
                IPAddress IPA = IPAddress.Parse(ip);
                Ping tPING = new Ping();
                PingReply tReply = tPING.Send(IPA);
                tPING.Dispose();
                if (tReply.Status != IPStatus.Success)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ee)
            {
                richTextBox1.AppendText(ee.ToString() + '\n');
                myLog.Write(ee.ToString());
                return false;
            }
        }

        //檢查連線狀態
        public void F_CheckConnection()
        {
            /*
            if (!PingIP("8.8.8.8") && !PingIP("168.95.1.1") && !PingIP("173.252.112.23"))
            {
                label5.ForeColor = Color.Red;
            }
            else
            {
                label5.ForeColor = Color.Green;
            }*/
            if (isconnect.connect())
            {
                label5.ForeColor = Color.Green;
            }
            else
            {
                label5.ForeColor = Color.Red;
            }
        }

        #endregion

        //國內報價
        #region LocalQoute

        //初始化伺服器
        private void iniStockserver()
        {
            m_L_Code = Functions.SKQuoteLib_Initialize("B222875498", "aa5367");
            m_L_countline = 0;
            if (m_L_Code == 0)
            {
                F_ShowAndWrite("元件初始化完成");
            }
            else if (m_L_Code == 2003)
            {
                F_ShowAndWrite("元件已初始過，無須重複執行");
            }
            else
            {
                F_ShowAndWrite("元件初始化失敗 code");
                return;
            }

            stockMonitor();
        }

        private void stockMonitor()
        {
            Time_15s.Stop();
            m_L_Code = Functions.SKQuoteLib_EnterMonitor();
            if (m_L_Code == 0)
            {
                F_ShowAndWrite("SKQuoteLib_EnterMonitor 呼叫成功");
                m_L_ReadyToRequest = true;
            }
            else
            {
                F_ShowAndWrite("SKQuoteLib_EnterMonitor 呼叫失敗");
                iniStockserver();
            }

        }

        public void OnNotifyConnection(int nKind, int nCode)
        {
            if (nCode == 0)
            {
                if (nKind == 100)
                {
                    F_ShowAndWrite("國內連線m_L_Connect=true");
                    m_L_Connect = true;
                }
                else if (nKind == 101)
                {
                    F_ShowAndWrite("國內連線m_L_Connect=false");
                    m_L_Connect = false;
                }
            }
        }


        //報價商品要求
        public void requestStock()
        {
            int nPage = 1;

            m_L_Code = Functions.SKQuoteLib_RequestStocks(out nPage, m_L_RequestSTOCK);

            if (m_L_Code == 0)
            {
                F_ShowAndWrite("SKQuoteLib_RequestStocks 呼叫成功");
            }
            else if (m_L_Code == 101)
            {
                F_ShowAndWrite("報價斷線造成RequestStocks失敗");
            }
            else if (m_L_Code == -4)
            {
                F_ShowAndWrite("尚未初始化成功造成RequestStocks失敗");
            }
            else
            {
                F_ShowAndWrite("SKQuoteLib_RequestStocks呼叫失敗");
            }
        }

        //國內最佳五檔要求
        public void requestBest5()
        {
            String[] arr = m_L_RequestSTOCK.Split(',');
            foreach (String s in arr)
            {
                int i = 0;
                Functions.SKQuoteLib_RequestTicks(out i, s);
                i++;
            }
        }

        //報價有異動會執行此function 傳送新的報價給client
        public void OnNotifyQuote(short sMarketNo, short sStockidx)
        {
            STOCK pSTOCK;

            Functions.SKQuoteLib_GetStockByIndex(sMarketNo, sStockidx, out pSTOCK);
            string line = "";

            line = DateTime.Now.ToString("yyyyMMdd-HH:mm:ss") + ',' + pSTOCK.m_caStockNo + ',' + pSTOCK.m_caName + ',' + pSTOCK.m_nRef + ',' + pSTOCK.m_nTickQty + ','
                + pSTOCK.m_nFutureOI + ',' + pSTOCK.m_nOpen + ',' + pSTOCK.m_nHigh + ',' + pSTOCK.m_nLow + ',' + pSTOCK.m_nClose + ',' + pSTOCK.m_nTQty +
                ',' + pSTOCK.m_nBid + ',' + pSTOCK.m_nBc + ',' + pSTOCK.m_nAsk + ',' + pSTOCK.m_nAc + ',' + pSTOCK.m_sDecimal + ',' + sStockidx.ToString()
                + ',' + '\n';

            if (F_L_CheckStock(pSTOCK))
            {
                broadCast(line);
                m_L_countline += line.Length;
                if (checkBox1.Checked == true)
                {
                    F_L_WriteQuoteToDB(pSTOCK);
                }
            }

            m_L_Quote_15s_flag = true;
            m_L_ReEnterMonitor_count = 0;
        }

        //五檔有異動會執行此function 傳送給要求的clinent
        public void OnNotifyBest5(short sMarketNo, short sStockidx)
        {
            bool m_remove = false;
            int i = 0;
            int n = 0;
            BEST5 best5;

            Functions.SKQuoteLib_GetBest5(sMarketNo, sStockidx, out best5);
            string line = "";

            line = sStockidx.ToString() + ','
                + best5.m_nAsk1.ToString() + ',' + best5.m_nAsk2.ToString() + ',' + best5.m_nAsk3.ToString() + ',' + best5.m_nAsk4.ToString() + ','
                + best5.m_nAsk5.ToString() + ',' + best5.m_nAskQty1.ToString() + ',' + best5.m_nAskQty2.ToString() + ',' + best5.m_nAskQty3.ToString()
                + ',' + best5.m_nAskQty4.ToString() + ',' + best5.m_nAskQty5.ToString() + ',' + best5.m_nBid1.ToString() + ',' + best5.m_nBid2.ToString()
                + ',' + best5.m_nBid3.ToString() + ',' + best5.m_nBid4.ToString() + ',' + best5.m_nBid5.ToString() + ',' + best5.m_nBidQty1.ToString()
                + ',' + best5.m_nBidQty2.ToString() + ',' + best5.m_nBidQty3.ToString() + ',' + best5.m_nBidQty4.ToString() + ',' + best5.m_nBidQty5.ToString() + ",\n";

            broadCast(line);
            //try
            //{
            //    foreach (MySocket client in clientlist)
            //    {
            //        if (!client.isdead && client.logsuccess)                    //client還沒斷線且登入成功才傳遞資料
            //        {
            //            if (client.best5_id == (sStockidx.ToString()))
            //            {
            //                client.send(line);
            //            }
            //        }
            //        else if (client.isdead)
            //        {
            //            n = i;
            //            m_remove = true;
            //            client.socket.Close();
            //        }
            //        i++;
            //    }
            //    if (m_remove)
            //    {
            //        clientlist.RemoveAt(n);
            //    }
            //}
            //catch (Exception ee)
            //{
            //    F_ShowAndWrite(ee.ToString());
            //}

        }

        //檢查報價是否斷線
        public void F_L_CheckQuoteAlive()
        {
            if (m_L_ReEnterMonitor_count >= 5)
            {
                F_ShowAndWrite("國內報價斷線,重新連線...");
                iniStockserver();
                m_L_ReEnterMonitor_count = 0;
            }
            else if (m_L_Quote_15s_flag == false)
            {
                F_ShowAndWrite("超過15秒沒新報價,國內報價重新連線...");
                Functions.SKQuoteLib_LeaveMonitor();
                stockMonitor();
                m_L_ReEnterMonitor_count++;
            }


            m_L_Quote_15s_flag = false;
        }

        //過濾重複報價
        public bool F_L_CheckStock(STOCK pstock)
        {
            int i = 0;
            if (pstock.m_nTickQty == 0)
            {
                return false;
            }

            foreach (STOCK s in stocklist)
            {
                if (s.m_caStockNo == pstock.m_caStockNo)
                {
                    if (s.m_nTQty == pstock.m_nTQty)
                    {
                        return false;
                    }
                    else
                    {
                        stocklist[i] = pstock;
                        return true;
                    }
                }
                i++;
            }
            stocklist.Add(pstock);
            return true;
        }

        //把國內報價資料寫入資料庫
        public void F_L_WriteQuoteToDB(STOCK pSTOCK)
        {
            try
            {
                L_CD.F_SQL_Add(pSTOCK.m_caStockNo, "Time,StockNo,StockName,Ref,TickQty,FutureOI,Open,High,Low,Close,TQty,Bid,Bc,Ask,Ac,Deci,MyKey",
                  "'" + DateTime.Now.ToString("yyyyMMdd-HH:mm:ss") + "','" + pSTOCK.m_caStockNo + "','" + pSTOCK.m_caName + "'," + pSTOCK.m_nRef +
                  ',' + pSTOCK.m_nTickQty + ',' + pSTOCK.m_nFutureOI + ',' + pSTOCK.m_nOpen + ',' + pSTOCK.m_nHigh + ',' + pSTOCK.m_nLow + ',' +
                  pSTOCK.m_nClose + ',' + pSTOCK.m_nTQty + ',' + pSTOCK.m_nBid + ',' + pSTOCK.m_nBc + ',' + pSTOCK.m_nAsk + ',' +
                  pSTOCK.m_nAc + ',' + pSTOCK.m_sDecimal + ",'" + pSTOCK.m_cMarketNo.ToString().Substring(0, 1) + pSTOCK.m_sStockidx.ToString() + "'");      //新增資料
            }
            catch (Exception ee)
            {
                F_ShowAndWrite(ee.ToString());
            }
        }

        //取得最新國內商品名單
        public void F_L_GetRequestSTOCK()
        {

            STOCK pSTOCK;
            short nStockidx = 0;
            F_ShowAndWrite("取得第一次國內SOTCK");

            //TSEA特別做
            m_L_RequestSTOCK = "TSEA";
            int nCode;

            while (true)
            {
                nCode = Functions.SKQuoteLib_GetStockByIndex(0, nStockidx, out pSTOCK);

                if (nCode == 0)
                {
                    nStockidx++;
                    if (pSTOCK.m_caStockNo.IndexOf("TSEA") != -1)
                    {
                        stocklist.Add(pSTOCK);
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            nStockidx = 0;

            while (true)
            {

                nCode = Functions.SKQuoteLib_GetStockByIndex(2, nStockidx, out pSTOCK);

                if (nCode == 0)
                {
                    nStockidx++;

                    if ((pSTOCK.m_caStockNo.Substring(0, 2) == "TX" || pSTOCK.m_caStockNo.Substring(0, 2) == "TE" || pSTOCK.m_caStockNo.Substring(0, 2) == "TF") && pSTOCK.m_caStockNo.Length < 5)
                    {
                        m_L_RequestSTOCK += "," + pSTOCK.m_caStockNo;
                        stocklist.Add(pSTOCK);
                        WriteDataFile(m_L_RequestSTOCK);
                    }
                }
                else
                {
                    break;
                }
            }
        }



        //建立國內symbol檔
        public void WriteDataFile(String line)
        {
            String[] arr = line.Split(',');
            String time = String.Format("\\{0:yyyy}\\{0:MM}\\{0:dd}", DateTime.Now);
            String Filename = "Local_symbol";
            String FilePath = Directory.GetCurrentDirectory() + "\\" + Filename + ".txt";

            FileInfo finfo = new FileInfo(FilePath);
            if (finfo.Directory.Exists == false)
            {
                finfo.Directory.Create();
            }
            File.WriteAllText(FilePath, line + "\r\n");
        }

        #endregion

        //海外報價
        #region OSQuote
        private void OSiniStockserver()                             //初始化海外報價server
        {
            m_OS_countline = 0;
            m_OS_Code = OSFunctions.SKOSQuoteLib_Initialize("B222875498", "aa5367");
            if (m_OS_Code == 0)
            {
                F_ShowAndWrite("OS元件初始化完成");
            }
            else if (m_OS_Code == 2003)
            {
                F_ShowAndWrite("OS元件已初始過，無須重複執行");
            }
            else
            {
                F_ShowAndWrite("OS元件初始化失敗 code");
                return;
            }

            OSstockMonitor();
        }

        private void OSstockMonitor()
        {
            m_OS_Code = OSFunctions.SKOSQuoteLib_EnterMonitor(0);

            if (m_OS_Code == 0)
            {
                F_ShowAndWrite("OSSKQuoteLib_EnterMonitor 呼叫成功");
                m_OS_ReadyToRequest = true;
            }
            else
            {
                F_ShowAndWrite("OSSKQuoteLib_EnterMonitor 呼叫失敗");
                OSiniStockserver();
            }

        }

        public void OSOnConnect(int nKind, int nCode)
        {
            if (nKind == 100)
            {
                F_ShowAndWrite("海外連線m_OS_Connect=true");
                m_OS_Connect = true;
            }
            else if (nKind == 101)
            {
                F_ShowAndWrite("海外連線m_OS_Connect=false");
                m_OS_Connect = false;
            }
        }


        public void OSOnNotifyQuote(short sStockidx)                //報價異動執行此function
        {
            FOREIGN pFOREIGN;

            OSFunctions.SKOSQuoteLib_GetStockByIndex(sStockidx, out pFOREIGN);
            string line = "";

            line = DateTime.Now.ToString("yyyyMMdd-HH:mm:ss") + ',' + pFOREIGN.m_caStockNo + ',' + pFOREIGN.m_caStockName + ',' + pFOREIGN.m_nRef + ',' +
                pFOREIGN.m_nTickQty + ',' + "0" + ',' + pFOREIGN.m_nOpen + ',' + pFOREIGN.m_nHigh + ',' + pFOREIGN.m_nLow + ',' + pFOREIGN.m_nClose
                + ',' + pFOREIGN.m_nTQty + ',' + pFOREIGN.m_nBid + ',' + pFOREIGN.m_nBc + ',' + pFOREIGN.m_nAsk + ',' + pFOREIGN.m_nAc + ',' +
                   pFOREIGN.m_sDecimal + ',' + "60" + pFOREIGN.m_sStockidx.ToString() + ',' + '\n';

            if (F_OS_CheckForeign(pFOREIGN))
            {
                broadCast(line);
                m_OS_countline += line.Length;
                if (checkBox1.Checked == true)
                {
                    F_OS_WriteQuoteToDB(pFOREIGN);
                }
            }

            m_OS_Quote_15s_flag = true;
            m_OS_ReEnterMonitor_count = 0;

        }


        //海外最佳五檔要求
        public void OSOnNotifyBest5(short sStockidx)
        {
            bool m_remove = false;
            int i = 0;
            int n = 0;
            OSBEST5 best5;

            OSFunctions.SKOSQuoteLib_GetBest5(sStockidx, out best5);
            String line = "";

            line = "60" + sStockidx.ToString() + ',' + best5.m_nAsk1.ToString() + ',' + best5.m_nAsk2.ToString() + ',' + best5.m_nAsk3.ToString() + ',' + best5.m_nAsk4.ToString() + ','
                + best5.m_nAsk5.ToString() + ',' + best5.m_nAskQty1.ToString() + ',' + best5.m_nAskQty2.ToString() + ',' + best5.m_nAskQty3.ToString()
                + ',' + best5.m_nAskQty4.ToString() + ',' + best5.m_nAskQty5.ToString() + ',' + best5.m_nBid1.ToString() + ',' + best5.m_nBid2.ToString()
                + ',' + best5.m_nBid3.ToString() + ',' + best5.m_nBid4.ToString() + ',' + best5.m_nBid5.ToString() + ',' + best5.m_nBidQty1.ToString()
                + ',' + best5.m_nBidQty2.ToString() + ',' + best5.m_nBidQty3.ToString() + ',' + best5.m_nBidQty4.ToString() + ',' + best5.m_nBidQty5.ToString() + ",\n";

            broadCast(line);

            //try
            //{
            //    foreach (MySocket client in clientlist)
            //    {
            //        if (!client.isdead && client.loㄍgsuccess)                    //client還沒斷線且登入成功才傳遞資料
            //        {
            //            if (client.best5_id == ("60" + sStockidx.ToString()))
            //            {
            //                client.send(line);
            //            }
            //        }
            //        else if (client.isdead)
            //        {
            //            n = i;
            //            m_remove = true;
            //            client.socket.Close();
            //        }
            //        i++;
            //    }
            //    if (m_remove)
            //    {
            //        clientlist.RemoveAt(n);
            //    }
            //}
            //catch (Exception ee)
            //{
            //    F_ShowAndWrite(ee.ToString());
            //}

        }


        private void OSrequestStock()
        {
            int nPage = 1;

            m_OS_Code = OSFunctions.SKOSQuoteLib_RequestStocks(out nPage, m_OS_RequestSTOCK);

            if (m_OS_Code == 0)
            {
                F_ShowAndWrite("OSSKQuoteLib_RequestStocks 呼叫成功");
            }
            else
            {
                F_ShowAndWrite("OSSKQuoteLib_RequestStocks呼叫失敗");
            }
        }

        private void OSrequestBest5()
        {
            String[] arr = m_OS_RequestSTOCK.Split('#');
            foreach (String s in arr)
            {
                int i = 0;
                m_OS_Code = OSFunctions.SKOSQuoteLib_RequestTicks(out i, s);
                i++;
            }
        }

        //檢查海外報價是否正常
        public void F_OS_CheckQuoteAlive()
        {
            if (m_OS_ReEnterMonitor_count >= 5)
            {
                F_ShowAndWrite("海外報價斷線,重新連線...");
                OSiniStockserver();
                m_OS_ReEnterMonitor_count = 0;
            }
            else if (m_OS_Quote_15s_flag == false)
            {
                F_ShowAndWrite("超過15秒沒新報價,海外報價重新連線...");
                OSFunctions.SKOSQuoteLib_LeaveMonitor();
                OSstockMonitor();
                Functions.SKQuoteLib_LeaveMonitor();
                stockMonitor();
                m_OS_ReEnterMonitor_count++;
            }

            m_OS_Quote_15s_flag = false;
        }

        //過濾重複報價
        public bool F_OS_CheckForeign(FOREIGN pforeign)
        {
            int i = 0;
            if (pforeign.m_nTickQty == 0)
            {
                return false;
            }
            foreach (FOREIGN f in foreignlist)
            {
                if (f.m_caStockNo == pforeign.m_caStockNo)
                {
                    if (f.m_nTQty == pforeign.m_nTQty || pforeign.m_nTickQty == 0)
                    {
                        return false;
                    }
                    else
                    {
                        foreignlist[i] = pforeign;
                        return true;
                    }

                }
                i++;
            }
            foreignlist.Add(pforeign);
            return true;
        }

        //把海外報價資料寫入資料庫
        public void F_OS_WriteQuoteToDB(FOREIGN pFOREIGN)
        {
            try
            {
                OS_CD.F_SQL_Add(pFOREIGN.m_caStockNo, "Time,StockNo,StockName,Ref,TickQty,FutureOI,Open,High,Low,Close,TQty,Bid,Bc,Ask,Ac,Deci,MyKey",
                  "'" + DateTime.Now.ToString("yyyyMMdd-HH:mm:ss") + "','" + pFOREIGN.m_caStockNo + "','" + pFOREIGN.m_caStockName + "'," + pFOREIGN.m_nRef +
                  ',' + pFOREIGN.m_nTickQty + ',' + "0" + ',' + pFOREIGN.m_nOpen + ',' + pFOREIGN.m_nHigh + ',' + pFOREIGN.m_nLow + ',' +
                  pFOREIGN.m_nClose + ',' + pFOREIGN.m_nTQty + ',' + pFOREIGN.m_nBid + ',' + pFOREIGN.m_nBc + ',' + pFOREIGN.m_nAsk + ',' +
                  pFOREIGN.m_nAc + ',' + pFOREIGN.m_sDecimal + ",'" + "60" + pFOREIGN.m_sStockidx.ToString() + "'");      //新增資料
            }
            catch (Exception ee)
            {
                F_ShowAndWrite(ee.ToString());
            }
        }

        //取得最新海外商品名單
        public void F_OS_GetRequestSTOCK()
        {
            int count = 0;
            short nStockidx = 0;
            F_ShowAndWrite("取得第一次海外SOTCK");
            while (true)
            {
                FOREIGN pFOREIGN;

                int nCode = OSFunctions.SKOSQuoteLib_GetStockByIndex(nStockidx, out pFOREIGN);

                if (nCode == 0)
                {
                    nStockidx++;

                    if (pFOREIGN.m_caExchangeNo.IndexOf("CME") != -1 && pFOREIGN.m_caStockNo.Substring(0, 2) == "NQ")
                    {

                        if (m_OS_RequestSTOCK == "")
                        {
                            m_OS_RequestSTOCK = pFOREIGN.m_caExchangeNo + ',' + pFOREIGN.m_caStockNo;
                            foreignlist.Add(pFOREIGN);
                        }
                        else
                        {
                            m_OS_RequestSTOCK += '#' + pFOREIGN.m_caExchangeNo + ',' + pFOREIGN.m_caStockNo;
                            foreignlist.Add(pFOREIGN);
                        }
                    }
                    else if (pFOREIGN.m_caExchangeNo.IndexOf("CME") != -1 && pFOREIGN.m_caStockNo.Substring(0, 2) == "EC")
                    {
                        if (count < 2)
                        {
                            m_OS_RequestSTOCK += '#' + pFOREIGN.m_caExchangeNo + ',' + pFOREIGN.m_caStockNo;
                            foreignlist.Add(pFOREIGN);
                            count++;
                        }
                    }
                    else if (pFOREIGN.m_caExchangeNo.IndexOf("CBT") != -1 && pFOREIGN.m_caStockNo.Substring(0, 2) == "YM")
                    {
                        if (count < 4)
                        {
                            m_OS_RequestSTOCK += '#' + pFOREIGN.m_caExchangeNo + ',' + pFOREIGN.m_caStockNo;
                            foreignlist.Add(pFOREIGN);
                            count++;
                        }
                    }
                    else if (pFOREIGN.m_caExchangeNo.IndexOf("NYM") != -1 && pFOREIGN.m_caStockNo.Substring(0, 2) == "CL")
                    {
                        if (count < 6)
                        {
                            m_OS_RequestSTOCK += '#' + pFOREIGN.m_caExchangeNo + ',' + pFOREIGN.m_caStockNo;
                            foreignlist.Add(pFOREIGN);
                            count++;
                        }
                    }
                    else if (pFOREIGN.m_caExchangeNo.IndexOf("NYM") != -1 && pFOREIGN.m_caStockNo.Substring(0, 2) == "GC")
                    {
                        if (count < 8)
                        {
                            m_OS_RequestSTOCK += '#' + pFOREIGN.m_caExchangeNo + ',' + pFOREIGN.m_caStockNo;
                            foreignlist.Add(pFOREIGN);
                            count++;
                        }
                    }
                    else if (pFOREIGN.m_caExchangeNo.IndexOf("NYM") != -1 && pFOREIGN.m_caStockNo.Substring(0, 2) == "SI")
                    {
                        if (count < 10)
                        {
                            m_OS_RequestSTOCK += '#' + pFOREIGN.m_caExchangeNo + ',' + pFOREIGN.m_caStockNo;
                            foreignlist.Add(pFOREIGN);
                            count++;
                        }
                    }
                    else if (pFOREIGN.m_caExchangeNo.IndexOf("SGX") != -1 && pFOREIGN.m_caStockNo.IndexOf("NKN") != -1)
                    {
                        if (count < 12)
                        {
                            m_OS_RequestSTOCK += '#' + pFOREIGN.m_caExchangeNo + ',' + pFOREIGN.m_caStockNo;
                            foreignlist.Add(pFOREIGN);
                            count++;
                        }
                    }
                    else if (pFOREIGN.m_caExchangeNo.IndexOf("SGX") != -1 && pFOREIGN.m_caStockNo.IndexOf("CN") != -1)
                    {
                        if (count < 14)
                        {
                            m_OS_RequestSTOCK += '#' + pFOREIGN.m_caExchangeNo + ',' + pFOREIGN.m_caStockNo;
                            foreignlist.Add(pFOREIGN);
                            count++;
                        }
                    }
                    else if (pFOREIGN.m_caExchangeNo.IndexOf("HKF") != -1 && pFOREIGN.m_caStockNo.Substring(0, 3) == "MHI")
                    {
                        if (count < 16)
                        {
                            m_OS_RequestSTOCK += '#' + pFOREIGN.m_caExchangeNo + ',' + pFOREIGN.m_caStockNo;
                            foreignlist.Add(pFOREIGN);
                            count++;
                        }
                    }
                    else if (pFOREIGN.m_caExchangeNo.IndexOf("EUR") != -1 && pFOREIGN.m_caStockNo.Substring(0, 3) == "DAX")
                    {
                        if (count < 18)
                        {
                            m_OS_RequestSTOCK += '#' + pFOREIGN.m_caExchangeNo + ',' + pFOREIGN.m_caStockNo;
                            foreignlist.Add(pFOREIGN);
                            count++;
                        }
                    }
                    else if (pFOREIGN.m_caExchangeNo.IndexOf("CFX") != -1 && pFOREIGN.m_caStockNo.Substring(0, 3) == "CIF")
                    {
                        if (count < 20)
                        {
                            m_OS_RequestSTOCK += '#' + pFOREIGN.m_caExchangeNo + ',' + pFOREIGN.m_caStockNo;
                            foreignlist.Add(pFOREIGN);
                            count++;
                        }
                    }
                    WriteOSDataFile(m_OS_RequestSTOCK);
                }
                else
                {
                    break;
                }
            }

        }


        //建立海外symbol檔
        public void WriteOSDataFile(String line)
        {
            String[] arr = line.Split(',');
            String time = String.Format("\\{0:yyyy}\\{0:MM}\\{0:dd}", DateTime.Now);
            String Filename = "OS_symbol";
            String FilePath = Directory.GetCurrentDirectory() + "\\" + Filename + ".txt";

            FileInfo finfo = new FileInfo(FilePath);
            if (finfo.Directory.Exists == false)
            {
                finfo.Directory.Create();
            }
            File.WriteAllText(FilePath, line + "\r\n");
        }

        #endregion


        public void RestartQuote_Time()
        {
            String[] check_time = { "0700", "0843", "0845", "0915", "0943", "1024", "1450", "1644", "1825" };
            String now_time = servertime.ToString("HHmm");
            try
            {
                foreach (String time in check_time)
                {
                    if (String.Compare(time, now_time) == 0)
                    {
                        if (now_time == "0815")
                        {
                            F_ShowAndWrite("重新啟動國內報價....");
                            requestStock();
                            requestBest5();
                            //Functions.SKQuoteLib_LeaveMonitor();
                            //stockMonitor();
                        }
                        else
                        {
                            F_ShowAndWrite("重新啟動海外報價....");
                            OSrequestStock();
                            OSrequestBest5();
                            //OSFunctions.SKOSQuoteLib_LeaveMonitor();
                            //OSstockMonitor();
                        }
                    }
                }
            }
            catch (Exception ee)
            {
                F_ShowAndWrite(ee.ToString());
            }
        }

        //public void F_SendFirstData(MySocket s)
        //{

        //    foreach (STOCK pSTOCK in stocklist)
        //    {
        //        String sline = DateTime.Now.ToString("yyyyMMdd-HH:mm:ss") + ',' + pSTOCK.m_caStockNo + ',' + pSTOCK.m_caName + ',' + pSTOCK.m_nRef + ',' + pSTOCK.m_nTickQty + ','
        //        + pSTOCK.m_nFutureOI + ',' + pSTOCK.m_nOpen + ',' + pSTOCK.m_nHigh + ',' + pSTOCK.m_nLow + ',' + pSTOCK.m_nClose + ',' + pSTOCK.m_nTQty +
        //        ',' + pSTOCK.m_nBid + ',' + pSTOCK.m_nBc + ',' + pSTOCK.m_nAsk + ',' + pSTOCK.m_nAc + ',' + pSTOCK.m_sDecimal + ',' + '\n';
        //        s.send(sline);
        //    }
        //    foreach (FOREIGN pFOREIGN in foreignlist)
        //    {
        //        String fline = DateTime.Now.ToString("yyyyMMdd-HH:mm:ss") + ',' + pFOREIGN.m_caStockNo + ',' + pFOREIGN.m_caStockName + ',' + pFOREIGN.m_nRef + ',' +
        //        pFOREIGN.m_nTickQty + ',' + "0" + ',' + pFOREIGN.m_nOpen + ',' + pFOREIGN.m_nHigh + ',' + pFOREIGN.m_nLow + ',' + pFOREIGN.m_nClose
        //        + ',' + pFOREIGN.m_nTQty + ',' + pFOREIGN.m_nBid + ',' + pFOREIGN.m_nBc + ',' + pFOREIGN.m_nAsk + ',' + pFOREIGN.m_nAc + ',' +
        //        pFOREIGN.m_sDecimal + ',' + '\n';
        //        s.send(fline);
        //    }
        //}

        public void F_L_CreateQuoteDB()
        {
            foreach (STOCK pSTOCK in stocklist)
            {
                L_CD.F_SQL_CreateTable_QuoteData(pSTOCK.m_caStockNo);
            }
        }

        public void F_OS_CreateQuoteDB()
        {
            foreach (FOREIGN pFOREIGN in foreignlist)
            {
                OS_CD.F_SQL_CreateTable_QuoteData(pFOREIGN.m_caStockNo);
            }
        }

        public void F_GetServerTime()
        {
            servertime = servertime.AddSeconds(1);

            if (DateTime.Compare(servertime, DateTime.Now) < 0)
            {
                servertime = DateTime.Now;
            }

            lbl_servertime.Text = servertime.ToString("HH:mm:ss");

        }

        //獲取最佳五檔client要求
        public String F_GetBest5(String caStockNo)
        {
            foreach (STOCK pSTOCK in stocklist)
            {
                if (pSTOCK.m_caStockNo == caStockNo)
                {
                    m_L_Best5_nPageNo++;
                    if (m_L_Best5_nPageNo > 49)
                    {
                        m_L_Best5_nPageNo = 1;
                    }
                    Functions.SKQuoteLib_RequestTicks(out m_L_Best5_nPageNo, pSTOCK.m_caStockNo);
                    return pSTOCK.m_sStockidx.ToString();
                }
            }
            foreach (FOREIGN pFOREIGN in foreignlist)
            {
                if (pFOREIGN.m_caStockNo == caStockNo)
                {
                    m_OS_Best5_nPageNo++;
                    if (m_OS_Best5_nPageNo > 49)
                    {
                        m_OS_Best5_nPageNo = 1;
                    }
                    m_OS_Code = OSFunctions.SKOSQuoteLib_RequestTicks(out m_OS_Best5_nPageNo, pFOREIGN.m_caExchangeNo + ',' + pFOREIGN.m_caStockNo);
                    return "60" + pFOREIGN.m_sStockidx.ToString();
                }
            }
            return "NoBest5";
        }

        //server第一次執行時啟用 找出已執行但未完成的委託並執行
        private void F_Select_Undone_pre_Oreder()
        {
            ConnectDatabase CDB = new ConnectDatabase();
            List<Pre_Order> Order_List_Pre_Order = new List<Pre_Order>();
            try
            {
                Order_List_Pre_Order = CDB.F_SQL_SelectPre_Order(3);                                             //抓取用戶的未成交委託  

                if (Order_List_Pre_Order.Count != 0)
                {
                    foreach (Pre_Order pre in Order_List_Pre_Order)
                    {
                        if (pre.type_order == 0)
                        {
                            if (pre.buy_type == 0)
                            {
                                richTextBox1.AppendText("抓取到用戶id:" + pre.user_id + "  下了 市價單 多單" + pre.order_num + "口   價錢:" + pre.order_price + "\n");
                            }
                            else
                            {
                                richTextBox1.AppendText("抓取到用戶id:" + pre.user_id + "  下了 市價單 空單" + pre.order_num + "口   價錢:" + pre.order_price + "\n");
                            }
                        }
                        else
                        {
                            if (pre.buy_type == 0)
                            {
                                richTextBox1.AppendText("抓取到用戶id:" + pre.user_id + "  下了 限價單 多單" + pre.order_num + "口   價錢:" + pre.order_price + "\n");
                            }
                            else
                            {
                                richTextBox1.AppendText("抓取到用戶id:" + pre.user_id + "  下了 限價單 空單" + pre.order_num + "口   價錢:" + pre.order_price + "\n");
                            }
                        }

                        Thread DoEntrust_Thread = new Thread(new ParameterizedThreadStart(F_DoEntrust));              //如果這筆委託是新的就執行成訂單
                        DoEntrust_Thread.Start(pre);
                        DoEntrust_Thread.IsBackground = true;
                    }
                    Order_List_Pre_Order = null;
                }

                Thread.Sleep(1);
            }
            catch (Exception ee)
            {
                myLog.Write(ee.ToString());
            }
        }

        //尋找所有新委託並執行
        private void F_Select_New_Pre_Order()
        {
            ConnectDatabase CDB = new ConnectDatabase();
            List<Pre_Order> Order_List_Pre_Order = new List<Pre_Order>();
            try
            {
                while (true)
                {
                    Order_List_Pre_Order = CDB.F_SQL_SelectPre_Order(0);                                             //抓取用戶的未成交委託  

                    if (Order_List_Pre_Order.Count != 0)
                    {
                        foreach (Pre_Order pre in Order_List_Pre_Order)
                        {
                            if (pre.type_order == 0)
                            {
                                if (pre.buy_type == 0)
                                {
                                    richTextBox1.AppendText("抓取到用戶id:" + pre.user_id + "  下了 市價單 多單" + pre.order_num + "口   價錢:" + pre.order_price + "\n");
                                }
                                else
                                {
                                    richTextBox1.AppendText("抓取到用戶id:" + pre.user_id + "  下了 市價單 空單" + pre.order_num + "口   價錢:" + pre.order_price + "\n");
                                }
                            }
                            else
                            {
                                if (pre.buy_type == 0)
                                {
                                    richTextBox1.AppendText("抓取到用戶id:" + pre.user_id + "  下了 限價單 多單" + pre.order_num + "口   價錢:" + pre.order_price + "\n");
                                }
                                else
                                {
                                    richTextBox1.AppendText("抓取到用戶id:" + pre.user_id + "  下了 限價單 空單" + pre.order_num + "口   價錢:" + pre.order_price + "\n");
                                }
                            }

                            CDB.F_SQL_command("Update  pre_order  SET  state  = 3  WHERE  pre_id  =" + pre.pre_id.ToString());

                            Thread DoEntrust_Thread = new Thread(new ParameterizedThreadStart(F_DoEntrust));              //如果這筆委託是新的就執行成訂單
                            DoEntrust_Thread.Start(pre);
                            DoEntrust_Thread.IsBackground = true;
                        }
                        Order_List_Pre_Order = null;
                    }

                    Thread.Sleep(1);
                }
            }
            catch (Exception ee)
            {
                myLog.Write(ee.ToString());
            }
        }

        //執行委託
        public void F_DoEntrust(object ob)
        {
            ConnectDatabase CDB = new ConnectDatabase();
            //Thread.Sleep(3000);
            Pre_Order pre = (Pre_Order)ob;
            STOCK stock_find = stocklist.Find(item => item.m_caStockNo == pre.future_source);
            FOREIGN foreign_find = foreignlist.Find(item => item.m_caStockNo == pre.future_source);
            Double Now_Price;
            String[] arr_user_future = CDB.F_SQL_Select_ReturnData("user_future", "future_num", "=", "'" + pre.future_source + "' and user_id = " + pre.user_id.ToString());
            if (arr_user_future == null)
            {
                F_Build_user_future(pre.user_id);
                arr_user_future = CDB.F_SQL_Select_ReturnData("user_future", "future_num", "=", "'" + pre.future_source + "' and user_id = " + pre.user_id.ToString());
            }
            User_Future user_future = new User_Future(arr_user_future);
            int id;


            while (true)
            {
                String[] pre_data = CDB.F_SQL_Select_ReturnData("pre_order", "pre_id", "=", pre.pre_id.ToString());
                pre = new Pre_Order(pre_data);

                TimeSpan Ts = DateTime.Now - pre.order_time;
                if (Ts.Seconds >= user_future.sell_wait_time)
                {

                    //如果取消委託則終止訂單
                    if (CDB.F_SQL_Select("pre_order", "pre_id = " + pre.pre_id.ToString() + " and (state = 1 or  state = 2)"))
                    {
                        break;
                    }

                    //查看是海外還是國內訂單  如果都沒找到資料則跳出訂單
                    if (stock_find.m_caStockNo != null)
                    {
                        stock_find = stocklist.Find(item => item.m_caStockNo == pre.future_source);
                        Now_Price = stock_find.m_nClose / Math.Pow(10, Convert.ToDouble(stock_find.m_sDecimal));
                    }
                    else if (foreign_find.m_caStockNo != null)
                    {
                        foreign_find = foreignlist.Find(item => item.m_caStockNo == pre.future_source);
                        Now_Price = foreign_find.m_nClose / Math.Pow(10, Convert.ToDouble(foreign_find.m_sDecimal));
                    }
                    else
                    {
                        break;
                    }

                    //防止價錢為0的時候交易
                    if (Now_Price != 0)
                    {
                        //市價單
                        if (pre.type_order == 0)
                        {
                            //使委託狀態變成完成委託 完成時間 成交價錢
                            CDB.F_SQL_command("Update  pre_order  SET  entry_price  =" + Now_Price.ToString() + ", entry_time ='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',state  = 1  WHERE  pre_id  =" + pre.pre_id.ToString());
                            pre.entry_price = Now_Price;

                            //把委託資料都新增到訂單
                            id = CDB.F_SQL_Add_ReturnID("order_entrust", "user_id,future_id,future_source,future_name,cost_trade,cost_stay,order_num,order_status,order_time,sell_num,type_buy,type_order,order_price,is_day_trade,user_profit,cost_type,lowest_price,lowest_cost,pre_id",
                                     pre.user_id.ToString() + "," + pre.future_id.ToString() + ",'" + pre.future_source + "','" + pre.futrue_name + "'," + user_future.trade_cost.ToString() + "," + user_future.stay_cost.ToString()
                                     + "," + pre.order_num.ToString() + "," + "1" + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + pre.order_num.ToString() + "," + pre.buy_type.ToString() + "," + pre.type_order.ToString() + "," + pre.entry_price.ToString()
                                     + "," + pre.is_day_trade.ToString() + ",-" + user_future.trade_cost.ToString() + ",'" + user_future.cost_type.ToString() + "'," + user_future.lowest_price.ToString() + "," + user_future.cost_base.ToString() + "," + pre.pre_id.ToString());
                            if (pre.buy_type == 0)
                            {
                                richTextBox1.AppendText("用戶id:" + pre.user_id + "  成交了市價單 多單" + pre.order_num + "口   價錢:" + Now_Price + "\n");
                            }
                            else
                            {
                                richTextBox1.AppendText("用戶id:" + pre.user_id + "  成交了市價單 空單" + pre.order_num + "口   價錢:" + Now_Price + "\n");
                            }
                            //檢查有無其他反向單並平倉
                            F_Closing(pre, id);


                            break;
                        }
                        else     //限價單
                        {
                            if (pre.buy_type == 0)  //多單
                            {
                                if (Now_Price <= pre.order_price)
                                {
                                    id = CDB.F_SQL_Add_ReturnID("order_entrust", "user_id,future_id,future_source,future_name,cost_trade,cost_stay,order_num,order_status,order_time,sell_num,type_buy,type_order,order_price,is_day_trade,user_profit,cost_type,lowest_price,lowest_cost,price_hope",
                                        pre.user_id.ToString() + "," + pre.future_id.ToString() + ",'" + pre.future_source + "','" + pre.futrue_name + "'," + user_future.trade_cost.ToString() + "," + user_future.stay_cost.ToString()
                                        + "," + pre.order_num.ToString() + "," + "1" + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + pre.order_num.ToString() + "," + pre.buy_type.ToString() + "," + pre.type_order.ToString() + "," + Now_Price.ToString() + ","
                                        + pre.is_day_trade.ToString() + ",-" + user_future.trade_cost.ToString() + ",'" + user_future.cost_type.ToString() + "'," + user_future.lowest_price.ToString() + "," + user_future.cost_base.ToString() + "," + pre.order_price.ToString());
                                    CDB.F_SQL_command("Update  pre_order  SET  entry_price  =" + Now_Price.ToString() + ", entry_time ='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',state  = 1  WHERE  pre_id  =" + pre.pre_id.ToString());
                                    richTextBox1.AppendText("用戶id:" + pre.user_id + "  成交了限價單 多單" + pre.order_num + "口   價錢:" + Now_Price + "\n");
                                    pre.entry_price = Now_Price;
                                    F_Closing(pre, id);

                                    break;
                                }

                            }
                            else if (pre.buy_type == 1)  //空單
                            {
                                if (Now_Price >= pre.order_price)
                                {
                                    id = CDB.F_SQL_Add_ReturnID("order_entrust", "user_id,future_id,future_source,future_name,cost_trade,cost_stay,order_num,order_status,order_time,sell_num,type_buy,type_order,order_price,is_day_trade,user_profit,cost_type,lowest_price,lowest_cost,price_hope",
                                        pre.user_id.ToString() + "," + pre.future_id.ToString() + ",'" + pre.future_source + "','" + pre.futrue_name + "'," + user_future.trade_cost.ToString() + "," + user_future.stay_cost.ToString()
                                        + "," + pre.order_num.ToString() + "," + "1" + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + pre.order_num.ToString() + "," + pre.buy_type.ToString() + "," + pre.type_order.ToString() + "," + Now_Price.ToString() + ","
                                        + pre.is_day_trade.ToString() + ",-" + user_future.trade_cost.ToString() + ",'" + user_future.cost_type.ToString() + "'," + user_future.lowest_price.ToString() + "," + user_future.cost_base.ToString() + "," + pre.order_price.ToString());

                                    CDB.F_SQL_command("Update  pre_order  SET  entry_price  =" + Now_Price.ToString() + ", entry_time ='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',state  = 1  WHERE  pre_id  =" + pre.pre_id.ToString());
                                    richTextBox1.AppendText("用戶id:" + pre.user_id + "  成交了限價單 空單" + pre.order_num + "口   價錢:" + Now_Price + "\n");
                                    pre.entry_price = Now_Price;
                                    F_Closing(pre, id);


                                    break;
                                }
                            }
                        }
                    }
                    Thread.Sleep(1);
                }
            }
        }


        public void F_Closing(Pre_Order pre, int id)
        {
            ConnectDatabase CDB = new ConnectDatabase();

            String[] entrust;
            while (pre.order_num != 0)
            {
                //找尋反向單
                entrust = CDB.F_SQL_Select_ReturnData("order_entrust", "user_id", "=", pre.user_id.ToString() + " and future_id =" + pre.future_id.ToString() + " and type_buy != " + pre.buy_type.ToString() + " and order_status != 2");

                if (entrust != null)
                {
                    Order_Entrust OE = new Order_Entrust(entrust);
                    //如果委託的口數大於反向的未平倉口數
                    if (pre.order_num > OE.sell_num)
                    {
                        double profit = Convert.ToDouble(OE.sell_num) * (pre.entry_price - OE.order_price) * 200;
                        pre.order_num -= Convert.ToInt32(OE.sell_num);
                        CDB.F_SQL_command("Update  order_entrust  SET  sell_num  = 0 , order_status =  2,sell_price = " + pre.entry_price.ToString() + ", sell_time= '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',user_profit=user_profit+" + profit.ToString() + ",sell_id=" + id.ToString() + " WHERE  order_id  =" + OE.order_id.ToString());                  //把過去的舊單平倉
                        CDB.F_SQL_command("Update  order_entrust  SET  sell_num  =" + pre.order_num.ToString() + ",order_status=0 , user_profit=user_profit+" + profit.ToString() + " ,pre_id =" + OE.order_id.ToString() + " WHERE  order_id  =" + id.ToString());      //扣掉新單的未平倉口數
                        CDB.F_SQL_command("Update  user_acc  SET  money_balance  = money_balance+" + profit.ToString() + "  WHERE  user_id  =" + pre.user_id.ToString());
                    }
                    else if (pre.order_num < Convert.ToInt32(OE.sell_num))                //如果委託的口數小於反向的未平倉口數
                    {
                        double profit = pre.order_num * (pre.entry_price - OE.order_price) * 200;
                        OE.sell_num = OE.sell_num - pre.order_num;
                        pre.order_num = 0;
                        CDB.F_SQL_command("Update  order_entrust  SET  sell_num  = " + OE.sell_num + ",order_status=0 ,user_profit=user_profit+" + profit.ToString() + " WHERE  order_id  =" + OE.order_id.ToString());                     //扣掉舊單的未平倉口數
                        CDB.F_SQL_command("Update  order_entrust  SET  sell_num  = 0 , order_status =  2,sell_price = " + OE.order_price.ToString() + ", sell_time= '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',user_profit=user_profit+" + profit.ToString() + ",sell_id=" + OE.order_id.ToString() + " ,pre_id =" + OE.order_id.ToString() + "  WHERE  order_id  =" + id.ToString());               //把新單平倉
                        CDB.F_SQL_command("Update  user_acc  SET  money_balance  = money_balance+" + profit.ToString() + "  WHERE  user_id  =" + pre.user_id.ToString());
                    }
                    else                                                                  //如果委託的口數等於反向的未平倉口數
                    {
                        double profit = Convert.ToDouble(pre.order_num) * (pre.entry_price - OE.order_price) * 200;
                        pre.order_num = 0;
                        CDB.F_SQL_command("Update  order_entrust  SET  sell_num  = 0 , order_status =  2,sell_price = " + pre.order_price.ToString() + ", sell_time= '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',user_profit=user_profit+" + profit.ToString() + ",sell_id=" + id.ToString() + "  WHERE  order_id  =" + OE.order_id.ToString());               //把舊單平倉
                        CDB.F_SQL_command("Update  order_entrust  SET  sell_num  = 0 , order_status =  2,sell_price = " + OE.order_price.ToString() + ", sell_time= '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',user_profit=user_profit+" + profit.ToString() + ",sell_id=" + OE.order_id.ToString() + " ,pre_id =" + OE.order_id.ToString() + "  WHERE  order_id  =" + id.ToString());               //把新單平倉
                        CDB.F_SQL_command("Update  user_acc  SET  money_balance  = money_balance+" + profit.ToString() + "  WHERE  user_id  =" + pre.user_id.ToString());
                    }
                }
                else
                {
                    CDB.F_SQL_command("Update  order_entrust  SET  pre_id  = " + pre.pre_id.ToString() + "  WHERE  order_id  =" + id.ToString());
                    break;
                }
            }
        }

        private void F_Update_User_money_open()
        {
            //DateTime dt1 = DateTime.Now;
            ConnectDatabase CDB = new ConnectDatabase();
            //String[] User_id_arr = CDB.F_SQL_Select_ReturnColumnData("user_acc", "user_id").Split(',');        

            foreach (User_acc user in List_User_acc)
            {
                List<Order_Entrust> OE_list = CDB.F_SQL_SelectEntrust(user.user_id);
                Double Now_Price;
                Double money_open = 0;

                foreach (Order_Entrust OE in OE_list)
                {
                    if (OE.order_status == 0 || OE.order_status == 1)
                    {
                        STOCK stock_find = stocklist.Find(item => item.m_caStockNo == OE.future_source);
                        FOREIGN foreign_find = foreignlist.Find(item => item.m_caStockNo == OE.future_source);


                        if (stock_find.m_caStockNo != null)
                        {
                            Now_Price = stock_find.m_nClose / Math.Pow(10, Convert.ToDouble(stock_find.m_sDecimal));
                            money_open += (Now_Price - OE.order_price) * OE.sell_num * 200;

                        }
                        else if (foreign_find.m_caStockNo != null)
                        {
                            Now_Price = foreign_find.m_nClose / Math.Pow(10, Convert.ToDouble(foreign_find.m_sDecimal));
                            money_open += (Now_Price - OE.order_price) * OE.sell_num * 200;
                        }
                        else
                        {

                        }
                    }
                }

                CDB.F_SQL_command("Update  user_acc  SET  money_open = " + money_open.ToString() + "  WHERE  user_id  =" + user.user_id.ToString());
            }
            //DateTime dt2 = DateTime.Now;
            //TimeSpan tp = new TimeSpan(dt2.Ticks - dt1.Ticks);
            //richTextBox1.AppendText(tp.ToString() + "\n");
        }

        //搜尋並更新使用者資訊
        private void F_Refresh_User_list()
        {
            //DateTime dt1 = DateTime.Now;
            ConnectDatabase CDB = new ConnectDatabase();
            List_User_acc = CDB.F_SQL_Select_User_acc();

            F_Update_money_today();
            F_Refresh_Lv_User();

            //DateTime dt2 = DateTime.Now;
            //TimeSpan tp = new TimeSpan(dt2.Ticks - dt1.Ticks);
            //richTextBox1.AppendText(tp.ToString() + "\n");
        }

        private void F_Update_money_today()
        {
            ConnectDatabase CDB = new ConnectDatabase();
            List<Order_Entrust> OE_list = new List<Order_Entrust>();

            //foreach (User_acc user in List_User_acc)
            //{
            //    OE_list = CDB.F_SQL_SelectEntrust(user.user_id);
            //    foreach (Order_Entrust OE in OE_list)
            //    {
            //        if (OE.order_status == 0 || OE.order_status == 1)
            //        {

            //        }
            //    }
            F_Update_User_money_open();

            //    //String money_today;
            //    //money_today=CDB.F_SQL_command_return_string("SELECT SUM(user_profit)/2 - SUM(cost_trade)/2 FROM  order_entrust  WHERE  (order_status = 0 or order_status = 2) and is_today = 1 and user_id = "+ua.user_id.ToString());
            //    //if (money_today == "")
            //    //{
            //    //    money_today = "0";
            //    //}
            //    //CDB.F_SQL_command("update user_acc set money_today ="+ money_today + " where user_id = "+ua.user_id.ToString());
            //}
        }

        private void F_Refresh_Lv_User()
        {
            if (this.InvokeRequired)
            {
                delegate_void Refresh_User_list = new delegate_void(F_Refresh_Lv_User);
                this.Invoke(Refresh_User_list);
            }
            else
            {
                int i = 0;
                //lv_User_acc.Items.Clear();
                foreach (User_acc user in List_User_acc)
                {
                    //新增用戶OR更改數據
                    if (i + 1 > lv_User_acc.Items.Count)
                    {
                        ListViewItem lvi = new ListViewItem(user.login_name.ToString());
                        lvi.SubItems.Add(user.money_balance.ToString());
                        lv_User_acc.Items.Add(lvi);
                    }
                    else
                    {
                        lv_User_acc.Items[i].SubItems[0].Text = user.login_name.ToString();
                        lv_User_acc.Items[i].SubItems[1].Text = user.money_balance.ToString();
                    }

                    //調整上下線用戶的顏色
                    if (user.is_online == 0)
                    {
                        lv_User_acc.Items[i].ForeColor = Color.Gray;
                    }
                    else
                    {
                        lv_User_acc.Items[i].ForeColor = Color.Green;
                    }

                    //列表的用戶比搜尋到的用戶多 則刪掉多餘的列表
                    if (lv_User_acc.Items.Count > List_User_acc.Count)
                    {
                        lv_User_acc.Items.RemoveAt(lv_User_acc.Items.Count - 1);
                    }

                    i++;
                }

            }
        }


        //建立user_future
        private void F_Build_user_future(int user_id)
        {
            ConnectDatabase CDB = new ConnectDatabase();
            String[] userdata = CDB.F_SQL_Select_ReturnData("user_acc", "user_id", "=", user_id.ToString());
            User_acc user = new User_acc(userdata);

            for (int i = 1; i < 19; i++)
            {
                String[] future_data = CDB.F_SQL_Select_ReturnData("future_base", "future_id", "=", i.ToString());
                Future_base fb = new Future_base(future_data);
                CDB.F_SQL_Add("user_future", "user_id,future_id,future_num,future_name,trade_cost,out_cost,stay_cost,future_price,single_order_max_num,"
                + "total_order_max_num,max_stay_day,max_stay_order_num,max_stay_future_num,enable_trade_type,enable_buy_type,future_status,status_change_time,"
                + "stop_trade_percent,trade_percent,auto_sell_percent,sell_wait_time,lowest_price,cost_type,cost_base", user.user_id.ToString() + "," +
                fb.future_id.ToString() + ",'" + fb.future_source + "','" + fb.future_name + "'," + fb.trade_cost.ToString() + "," + "0" + "," + fb.stay_cost.ToString() + "," +
                fb.future_price.ToString() + "," + fb.single_order_max_num.ToString() + "," + fb.total_order_max_num.ToString() + "," + fb.max_stay_day.ToString() + "," +
                fb.max_stay_order_num.ToString() + "," + fb.max_stay_future.ToString() + ",'" + fb.enable_trade_type.ToString() + "'," + fb.enable_buy_type.ToString() +
                "," + "0" + ",'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + fb.stop_trade_precent.ToString() + "," + fb.trade_precent.ToString() + "," + fb.auto_sell_precent.ToString() +
                "," + fb.sell_wait_time.ToString() + "," + fb.lowest_price.ToString() + "," + "0" + "," + fb.future_price.ToString());
            }
        }


        class DoubleBufferListView : ListView
        {
            public DoubleBufferListView()
            {
                SetStyle(ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
                UpdateStyles();
            }
        }


        public void F_Change_OE_is_today()
        {
            ConnectDatabase CDB = new ConnectDatabase();

            CDB.F_SQL_command("update order_entrust  set  is_today = if((day(now())!=day(order_time)),0,1) where is_today = 1");
        }

        public void sendstock()
        {
            Random rand = new Random();
            int id = rand.Next(1, 200);
            int currentPrice = rand.Next(200,20000);
            Double changeRate =  Convert.ToDouble(rand.NextDouble().ToString("f" + 2));
            broadCast(id + "," + "STOCK" + id + "," + currentPrice + "," + changeRate + '\n');
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            sendstock();
        }


        //public void WriteOSDataFile(String line)
        //{
        //    String[] arr = line.Split(',');
        //    String time = String.Format("\\{0:yyyy}\\{0:MM}\\{0:dd}", DateTime.Now); ;
        //    String Filename = arr[1];
        //    String FilePath = @"C:\\OSdata" + time + "\\" + Filename + ".txt";

        //    FileInfo finfo = new FileInfo(FilePath);
        //    if (finfo.Directory.Exists == false)
        //    {
        //        finfo.Directory.Create();
        //    }
        //    File.AppendAllText(FilePath, line + "\r\n");
        //}

    }

}
