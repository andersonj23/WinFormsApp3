using System;
using System.Windows.Forms;

namespace SanguigoreRPG
{

    static class Program
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern bool AllocConsole();
        [STAThread]
        static void Main()

        {
            AllocConsole();
            // Set up for a Windows Forms application with visual styles enabled
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Start the application with the MainMenuForm
            MainMenuForm mainMenu = new MainMenuForm();
            Application.Run(mainMenu);  // This runs the main menu and keeps the app alive
        }
    }
}
