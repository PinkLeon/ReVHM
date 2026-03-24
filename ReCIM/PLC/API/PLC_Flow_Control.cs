using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC.API
{
   public class PLC_Flow_Control
    {
       public PLCOperation PLCCore;
        int DM1000 = 0;
        public PLC_Flow_Control(PLCOperation _PLCCore)
        {
            this.PLCCore = _PLCCore;
        }
      
        public async Task<int> ReadSignal(string RunPoint,string PCCheckPoint,string PLCCheckPoint)
        {
            DM1000 = 0;
            try
            {
                if (await ReadMRs(PLCCheckPoint, true))
                {
                    DM1000 = await ReadDMs(RunPoint, 1);

                    await PLCCore.WriteMR(PCCheckPoint, true);
                    if (await ReadMRs(PLCCheckPoint, false))
                    {
                        await PLCCore.WriteMR(PCCheckPoint, false);
                    }
                }
            }
            catch
            {
                DM1000 = 0;
            }
            
            return DM1000;

        }

        public async Task<bool> ReadMRs(string Point,bool Target,int time=1000)
        {
            bool OutData = false ;
            var st = System.Environment.TickCount;
            string TT = Target ? "1" : "0";
            Func<Task<bool>> Myfunction = async () =>
            {

                while (true)
                {
                    var answer= await PLCCore.ReadMRs(Point, 1);
                    if (answer.ToList().First()== TT)
                    {
                       return  OutData = Target;
                       
                       
                    }
                    if (System.Environment.TickCount - st > time)
                    {
                       return  OutData =! Target;
                       
                    }
                }
            };
            await Myfunction();
            return OutData;
        }


        public async Task<int> ReadDMs(string Point,int count, int time = 500)
        {
            int OutData = 0;
            var st = System.Environment.TickCount;
            Func<Task<int>> Myfunction = async () =>
            {

                while (true)
                {
                    var answer = await PLCCore.ReadDMs(Point, count);
                    if (int.Parse(answer.ToList().First()) >0)
                    {
                       return  OutData = int.Parse(answer.ToList().First());

                    }
                    if (System.Environment.TickCount - st > time)
                    {
                        return OutData = 0;
                    }
                }
            };
            await Myfunction();
            return OutData;
        }
        public async Task PC_To_PLC(string Target,string PCCheckPoint,string PLCCheckPoint, List<string> DM1110ToDM1115 )
        {
            await PLCCore.WriteDMs(Target, DM1110ToDM1115, DM1110ToDM1115.Count);
            await PLCCore.WriteMR(PCCheckPoint, true);

            if (await ReadMRs(PLCCheckPoint,true))
            {
                await PLCCore.WriteMR(PCCheckPoint, false);
            }
        }
    }

}
