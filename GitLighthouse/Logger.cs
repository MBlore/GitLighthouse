using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lighthouse
{
    public class Logger
    {
        public static void Log(string text)
        {
            try
            {
                File.AppendAllText(
                "Log.txt",
                "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + "]: " + text + "\r\n");
            }
            catch
            {
            }
        }
    }
}
