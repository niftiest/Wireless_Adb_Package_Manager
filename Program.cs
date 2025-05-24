using WirelessAdbPackageManager.Handlers; // For AppManager

namespace WirelessAdbPackageManager
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            
            AppManager appManager = new AppManager();
            Form1 mainForm = new Form1(appManager); // UIManager.Form replaced
            
            Application.Run(mainForm); // UIManager.Form replaced
        }
    }
}