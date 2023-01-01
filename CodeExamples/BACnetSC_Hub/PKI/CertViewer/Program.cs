using System;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

namespace CertViewer
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Load Embbeded dll
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args2) =>
            {

                Assembly thisAssembly = Assembly.GetExecutingAssembly();

                //Get the Name of the AssemblyFile
                var name = args2.Name.Substring(0, args2.Name.IndexOf(',')) + ".dll";

                //Load form Embedded Resources - This Function is not called if the Assembly is in the Application Folder
                var resources = thisAssembly.GetManifestResourceNames().Where(s => s.EndsWith(name));
                if (resources.Count() > 0)
                {
                    var resourceName = resources.First();
                    using (Stream stream = thisAssembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null) return null;
                        var block = new byte[stream.Length];
                        stream.Read(block, 0, block.Length);
                        return Assembly.Load(block);
                    }
                }
                return null;
            };
            
            if ((args==null)||(args.Length==0))
                Application.Run(new Form1(null, null));
            else 
                if (args.Length==1)
                    Application.Run(new Form1(args[0], null));
                else
                    Application.Run(new Form1(args[0], args[1]));
        }
    }
}
