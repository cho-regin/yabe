using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Yabe;

namespace GlobalCommander
{
    public class Plugin : IYabePlugin
    {
        private YabeMainDialog _yabeFrm;

        public void Init(YabeMainDialog yabeFrm) // This is the unique mandatory method for a Yabe plugin 
        {
            this._yabeFrm = yabeFrm;

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {

                Assembly thisAssembly = Assembly.GetExecutingAssembly();

                //Get the Name of the AssemblyFile
                var name = args.Name.Substring(0, args.Name.IndexOf(',')) + ".dll";

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

            // Creates the Menu Item
            ToolStripMenuItem MenuItem = new ToolStripMenuItem();
            MenuItem.Text = "Global Commander";
            MenuItem.Click += new EventHandler(MenuItem_Click);

            // Add It as a sub menu (pluginsToolStripMenuItem is the only public Menu member)
            yabeFrm.pluginsToolStripMenuItem.DropDownItems.Add(MenuItem);

        }

        public void MenuItem_Click(object sender, EventArgs e)
        {
            try // try catch all to avoid Yabe crach
            {
                Trace.WriteLine("Loading Global Commander window...");
                GlobalCommander frm = new GlobalCommander(this._yabeFrm);
                frm.Show();
            }
            catch
            {
                Cursor.Current = Cursors.Default;
                Trace.Fail("Failed to load the Global Commander window.");
            }
        }
    }
}
