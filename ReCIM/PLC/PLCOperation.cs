using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PLC.API;
using PLC.Model;
using RemoteConnection;

namespace PLC
{
    
    public class PLCOperation
    {
        public Operation OP;
        private Trans TT = new Trans();
        private string Mode = "";
        private bool Connected = false;
        private List<string> Command = new List<string>();
        public PLC_Flow_Control MyFLowControl;
        public  PLCOperation(string IP,int port)
        {
            OP = new Operation(IP, port);
            MyFLowControl = new PLC_Flow_Control(this);
            //PLCConnect();
        }

        public void ClosePlcConnection()
        {
            if(OP != null)
            {
                OP.Close();
            }
        }

        public bool PLCConnect()
        {
           return Connected=OP.Connect();
        }
        public async Task<string> CheckConnection()
        {
            await OP.SendMsg(TT.Covert_Hex_String("3F4B0D"));
            string Message = await OP.ReadMsg();
            return Message;

        }
        public async Task<string> WriteDM(string Point, string Value)
        {
            string data = string.Format("WR{0}DM{1}{2}{3}{4}", TT.Covert_Hex_String("20"), Point, TT.Covert_Hex_String("20"), Value, TT.Covert_Hex_String("0D"));
            string Message = await CheckCommandOK(data);
            return Message;
        }

        public async Task<string> WriteDMs(string Point, List<string> ListValue, int number)
        {
            try
            {
                if (ListValue.Count != number)
                {
                    return "Error";
                }
                string Datavalue = "";
                foreach (var L in ListValue)
                {
                    Datavalue += TT.Covert_Hex_String("20") + L + "";
                }

                string data = string.Format("WRS{0}DM{1}{2}{3}{4}{5}", TT.Covert_Hex_String("20"), Point, TT.Covert_Hex_String("20"), number, Datavalue, TT.Covert_Hex_String("0D"));
                string Message= await CheckCommandOK(data);
                return Message;
            }
            catch(Exception ex)
            {
                return "";
            }
        }

        public async Task<string> ReadDM(string Point)
        {
            try
            {
                string data = string.Format("RD{0}DM{1}{2}", TT.Covert_Hex_String("20"), Point, TT.Covert_Hex_String("0D"));
                await OP.SendMsg(data);
                string Message = await OP.ReadMsg();
                string Outdata = int.Parse(Message.Substring(0, 5)).ToString();
                return Outdata;
            }
            catch(Exception ex)
            {
                return "";
            }
        }

        public async Task<string[]> ReadDMs(string Point, int number)
        {
            List<string> Templist = new List<string>();
            try
            {
                string data = string.Format("RDS{0}DM{1}{2}{3}\r", TT.Covert_Hex_String("20"), Point, TT.Covert_Hex_String("20"), number, TT.Covert_Hex_String("0D"));
                await OP.SendMsg(data);
                var Newdata = await OP.ReadMsg();
               
                if (Newdata == "" && Newdata.Length > 8)
                {
                    Templist.Add("0");
                    return Templist.ToArray();
                }
                for (int i = 0; i < number; i++)
                {
                    var d = int.Parse(Newdata.Substring(i * 6, 5)).ToString();
                    Templist.Add(d);
                }


            }
            catch(Exception ex)
            {

            }
            return Templist.ToArray();
        }

        public async Task<string[]> ReadDMsbynum(string Point, int number)
        {
            string data = string.Format("RDS{0}DM{1}{2}{3}\r", TT.Covert_Hex_String("20"), Point, TT.Covert_Hex_String("20"), number, TT.Covert_Hex_String("0D"));
            await OP.SendMsg(data);
            var Newdata =await OP.ReadMsg();
            List<string> Templist = new List<string>();
            if (Newdata == "" && Newdata.Length > 8)
            {
                Templist.Add("0");
                return Templist.ToArray();
            }
            try
            {
                Newdata.Replace(" ", "");
                for (int i = 0; i < number; i++)
                {
                    var t1 = Newdata.Substring(i * 6, 5).ToString();
                    Templist.Add(t1);
                }
            }
            catch
            {

            }

            return Templist.ToArray();
        }

        public string DMs_To_String(List<string> _dms)
        {
            string DMstring = "";

            foreach (var d in _dms)
                DMstring += d;

            return DMstring;
        }

        private string ConvertHex(String hexString)
        {
            try
            {
                string ascii = string.Empty;

                for (int i = 0; i < hexString.Length; i += 2)
                {
                    String hs = string.Empty;
                    hs = hexString.Substring(i, 2);
                    uint decval = System.Convert.ToUInt32(hs, 16);
                    char character = System.Convert.ToChar(decval);
                    ascii += character;
                }
                return ascii;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            return string.Empty;
        }
        /// <summary>
        /// 寫入MR
        /// </summary>
        /// <param name="Point"></param>
        /// <param name="Reset"></param>
        /// <returns></returns>
        public async Task<string> WriteMR(string Point, bool Reset)
        {
            try
            {
                string data = string.Format("{0}{1}MR{2}{3}", Reset ? "ST" : "RS", TT.Covert_Hex_String("20"), Point, TT.Covert_Hex_String("0D"));
                string Message =await CheckCommandOK(data);
                return Message;
            }
            catch(Exception ex)
            {
                return ex.ToString();
            }
        }
        public async Task<string> ReadMR(string Point)
        {

            string Message = "";
            Func<Task<string>> Myfunction = async () =>
            {
                string data = string.Format("RD{0}MR{1}{2}", TT.Covert_Hex_String("20"), Point, TT.Covert_Hex_String("0D"));
                await OP.SendMsg(data);
                return Message=OP.ReadMsg().Result.Replace(System.Environment.NewLine, string.Empty);
            };
            await Myfunction();
            return Message;


        }
        
        /// <summary>
        /// 讀取複數MR
        /// </summary>
        /// <param name="Point"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        public async Task<string[]> ReadMRs(string Point, int number)
        {
            
            try
            {
                string data = string.Format("RDS{0}MR{1}{2}{3}\r", TT.Covert_Hex_String("20"), Point, TT.Covert_Hex_String("20"), number, TT.Covert_Hex_String("0D"));
                await OP.SendMsg(data);
                var Newdata =await  OP.ReadMsg();
                List<string> Templist = new List<string>();
                if (Newdata.Length > 0)
                {
                    for (int i = 0; i < number; i++)
                    {
                        Templist.Add(Newdata.Substring(i * 2, 1));
                    }
                    return Templist.ToArray();
                }
                return Templist.ToArray();
            }
            catch(Exception ex)
            {
                return new List<string>().ToArray();
            }

        }
         private async Task<string> CheckCommandOK(string CommandString)
        {
            try
            {
                string Data = string.Empty;
                await OP.SendMsg(CommandString);
                var Message = await OP.ReadMsg();
                if (Message.Substring(0, 2) == "OK")
                {
                    return "OK";
                }
                return Data;
            }
            catch(Exception ex)
            {
                return "Connection Fail";
            }

           
        }

    }
}
