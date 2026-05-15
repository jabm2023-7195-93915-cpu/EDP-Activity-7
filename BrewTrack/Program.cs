using System;
using System.Windows.Forms;
using OfficeOpenXml;

namespace BrewTrack
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ExcelPackage.License.SetNonCommercialPersonal("Julie Ana Merle");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LoginForm());
        }
    }
}   