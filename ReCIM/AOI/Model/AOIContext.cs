using VHM.Model;

namespace AOI.Model
{
    internal class AOIContext
    {
        public FrontResult frontResult { get; set; }

        //public BackResult backResult { get; set; }

        public FrontInspection frontInspection { get; set; }

        public FrontMeasurement frontMeasurement { get; set; }
    }
}
