using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PLC.API
{
    public class Trans
    {
        public string Covert_Hex_String(string hex)
        {
            byte[] data = FromHex(hex);
            return Encoding.ASCII.GetString(data);
        }
        public byte[] FromHex(string hex)
        {
            hex = hex.Replace("-", "");
            byte[] raw = new byte[hex.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return raw;
        }
        //public DMS DM_Value_TO_String(double value, uint Precision)
        //{
        //    DMS outdata = new DMS();
        //    if (value < 0)
        //    {
        //        return outdata;
        //    }
        //    else
        //    {
        //        UInt32 intputdata = Convert.ToUInt32(Math.Abs(value) * Math.Pow(10, Precision));
        //        UInt32 Fist = 4294967295;
        //        outdata.SecondDM = ((Fist - intputdata) / 65536).ToString();
        //        outdata.FirstDM = (Fist - intputdata) % 65536 > 65535 ? 65535.ToString() : ((Fist - intputdata) % 65536).ToString();
        //    }
        //    return outdata;
        //}
    }
}
