using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PLC.Model
{
    public class DM
    {
        public string Result { get; set; } = "fail";
        public List<int> DMdata = new List<int>();
    }
}
