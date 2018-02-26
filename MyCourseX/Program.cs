using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyCourseX
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {

            Cef.EnableHighDPISupport();
            var settings = new CefSettings();
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WebForm());
        }
    }
}
