using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TFSHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            TFSManager tfsManager = new TFSManager(args);

            switch (tfsManager.Mode)
            { 
                case Operation.LIST:
                    tfsManager.List();
                    break;
                case Operation.BUILDCOPY:
                    tfsManager.BuildCopy();
                    break;
                default:
                    tfsManager.WriteHelp();
                    break;
            }
        }
    }
}
