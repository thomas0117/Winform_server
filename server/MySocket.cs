using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace server
{
    public delegate String StrHandler(String str);
    public delegate void NoReturn_Str(String s);
    class MySocket
    {
        public Socket socket;
        public StreamReader reader;
        public StreamWriter writer;
        public NetworkStream stream;
        public bool isdead = false;
        public StrHandler strhandler;
        public StrHandler chathandler;
        public StrHandler getbest5;
        public NoReturn_Str logout;
        public Thread listenthread;
        public String UserID = "";
        public EndPoint remoteEndPoint;
        public bool logsuccess = false;
        public String best5_id="";   

        public MySocket(Socket s)
        {
            socket = s;
            stream = new NetworkStream(s);
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);
            writer.AutoFlush = true;
            remoteEndPoint = socket.RemoteEndPoint;
        }

        public void listener(StrHandler phandler, StrHandler shandler, StrHandler fgetbest5)
        {
            strhandler = phandler;                              //處理帳號密碼
            chathandler = shandler;                             //處理訊息
            getbest5 = fgetbest5;

            listenthread = new Thread(listen);
            listenthread.Start();
        }
       
        public void listen()
        {
            ConnectDatabase CDB = new ConnectDatabase();
            try
            {
                while (true)
                {
                    String line = receive();
                    //MessageBox.Show(line);
                    if (line.IndexOf('$') == -1)                //沒有$表示不是帳密 做訊息處理
                    {
                        String[] arr = line.Split(',');

                        //if (arr[0] == "Request_Best5")
                        //{
                        //    best5_id=getbest5(arr[1]);
                        //}
                        //else
                        //{
                            chathandler(line);
                        //}
                    }
                    else
                    {
                        UserID = strhandler(line);
                        if (UserID == "")                      //如果傳回的ID為""則登入失敗
                        {
                            send("connect_fail");
                            myLog.Write(remoteEndPoint + " 登入失敗");
                        }
                        else
                        {
                            chathandler(UserID + "登入成功");
                            myLog.Write(line + remoteEndPoint + " 登入成功");
                            F_UpDate_UserData();
                            logsuccess = true;
                        }
                    }
                }
            }
            catch (Exception ee)
            {
                chathandler("IP:" + remoteEndPoint + " 使用者:" + UserID + "已經離線");
                myLog.Write("IP:" + remoteEndPoint + " 使用者:" + UserID + "已經離線");
                CDB.F_SQL_command("Update  user_acc  SET  is_online =0");
                isdead = true;
            }

        }

        public void send(String msg)
        {
            try
            {
                 //writer.WriteLine(msg);
                 //writer.Flush();

                 byte[] line = Encoding.UTF8.GetBytes(msg);

                 socket.Send(line, SocketFlags.None);
            }
            catch(Exception ee)
            {
                myLog.Write(ee.ToString());
            }

        }

        //把讀到的資料轉成字串
        public string receive()
        {
            byte[] buffer = new byte[64];
            int rev = socket.Receive(buffer, SocketFlags.None);
            String str = Encoding.UTF8.GetString(buffer);
            if (str.IndexOf('\r') != -1)
            {
                str = str.Remove(str.IndexOf('\r'));                //去除\r以後的字元
            }
            if (str.IndexOf('\0') != -1)
            {
                str = str.Remove(str.IndexOf('\0'));                //去除\0以後的字元
            }
            if (str == "")
            {
                isdead = true;
                socket.Close();
            }
            return str;
        }

        private void F_UpDate_UserData()
        {
            ConnectDatabase CDB = new ConnectDatabase();
            String time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            CDB.F_SQL_command("Update  user_acc  SET  login_ip  ='" + remoteEndPoint.ToString() + "', login_time ='" + time + "',is_online =1   WHERE  sys_name  ='" + UserID + "'");

        }



    }
}
