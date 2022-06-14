using System;
using System.Net;

namespace server
{
    class isconnect
    {

        public static bool connect()
        {
            if (isconnect.isConnectHinet())
            {
                return true;
            }
            else if (isconnect.isConnectGoogle())
            {
                return true;
            }
            else if (isconnect.isConnectBaidu())
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        private static bool isConnectHinet()
        {
            try
            {

                IPHostEntry host = Dns.GetHostEntry("168.95.1.1");
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private static bool isConnectGoogle()
        {
            try
            {

                IPHostEntry host = Dns.GetHostEntry("8.8.8.8");
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private static bool isConnectBaidu()
        {
            try
            {

                IPHostEntry host = Dns.GetHostEntry("61.144.56.100");
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

    }
}
