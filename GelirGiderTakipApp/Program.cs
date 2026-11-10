using System;
using System.Windows.Forms;

namespace GelirGiderTakip
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1()); // Form1 burada çağrılır
        }
    }
}