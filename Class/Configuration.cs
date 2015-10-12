using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TFSHelper
{

    public enum Operation
    {
        BUILDDUMP,
        LIST,
        BUILDCOPY,
        NONE
    }

    public class Configuration
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public string ProjectSource { get; set; }
        public string ProjectDestination { get; set; }
        public Operation? Mode { get; set; }
    }
}
