using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace alt_trg_ime
{
    public partial class InvisibleComponent : Component
    {
        private MenuItem quitButton = new MenuItem("Exit");
        public InvisibleComponent()
        {
            KeyboardHook.Start();
            InitializeComponent();

            quitButton.Click += Application_Exit;

            var menu = new ContextMenu();
            menu.MenuItems.Add(quitButton);
            residentIcon.ContextMenu = menu;
        }

        private void Application_Exit(object sender, EventArgs e)
        {
            KeyboardHook.Stop();
            quitButton.Click -= Application_Exit;
            Application.Exit();
        }
    }
}
