using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Security.Cryptography.X509Certificates;

namespace CertViewer
{
    static class Program
    {
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if ((args==null)||(args.Length==0))
                Application.Run(new Form1(null));
            else
                Application.Run(new Form1(args[0]));
        }
    }
}
