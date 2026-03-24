using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RemoteConnection
{
    public class Operation
    {
        TcpClient tcpClient;
        IPEndPoint ipe;
        bool closed = false;
        byte[] testByte = new byte[1];
        string IP;
        int Port;
        public Operation(string IP, int port)
        {
            this.IP = IP;
            this.Port = port;
            //先建立一個TcpClient;
            IPAddress ipa = IPAddress.Parse(IP);
            ipe = new IPEndPoint(ipa, port);
            tcpClient = new TcpClient();
            //
        }
        public bool Connect()
        {
            try
            {
                IPAddress ipa = IPAddress.Parse(IP);
                ipe = new IPEndPoint(ipa, this.Port);
                //先建立一個TcpClient;
                tcpClient = null;
                tcpClient = new TcpClient();
                tcpClient.Connect(ipe);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }

        }
        private bool Alive_Check()
        {
            try
            {
                //使用Peek測試連線是否仍存在
                if (tcpClient.Connected
               && tcpClient.Client.Poll(0, SelectMode.SelectRead))
                    closed =
                tcpClient.Client.Receive(testByte, SocketFlags.Peek) == 0;
            }
            catch (SocketException se)
            {
                closed = true;
            }
            return closed;
        }
        public async Task<bool> SendMsg(string msg)
        {
            Func<Task<bool>> M1 = async () =>
            {
                try
                {
                    NetworkStream ns = tcpClient.GetStream();
                    ns.WriteTimeout = 200;
                    ns.ReadTimeout = 200;
                    if (ns.CanWrite)
                    {
                        byte[] newbyte = Encoding.Default.GetBytes(msg);
                        ns.Write(newbyte, 0, newbyte.Length);
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
                
            };
            await M1();
            return false;   
              
        }
        public async Task<string> ReadMsg()
        {
            string receiveMsg = string.Empty;

            Func<Task<string>> M1 = async () =>
            {
                try
                {
                    byte[] receiveBytes = new byte[tcpClient.ReceiveBufferSize];
                    int numberOfBytesRead = 0;
                    NetworkStream ns = tcpClient.GetStream();
                    ns.WriteTimeout = 200;
                    ns.ReadTimeout = 200;

                    if (ns.CanRead)
                    {
                        numberOfBytesRead = ns.Read(receiveBytes, 0, tcpClient.ReceiveBufferSize);
                        return receiveMsg = Encoding.Default.GetString(receiveBytes, 0, numberOfBytesRead);
                    }
                }
                catch
                {

                }
                return receiveMsg;

            };
            await M1();
            return receiveMsg;

        }
        public void Close()
        {
            tcpClient.Close();
        }


    }
}
