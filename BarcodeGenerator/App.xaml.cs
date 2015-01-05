using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BarcodeGenerator
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Utility.DeleteWorkingFolder();
            Utility.WriteExecuteResult(false);
            if(e.Args.Length >0)
            {
                Utility.WriteExecuteResult(false);
                IBarcodeGenerator barcodeGenerator = BarcodeFactory.CreateGenerator();
                string barcode = barcodeGenerator.Generate(e.Args[0],-1,-1);
                barcodeGenerator.Save();
                string sPlateBarcodeFile = Utility.GetBarcodesFolder() + "PlateBarcode.txt";
                File.WriteAllText(sPlateBarcodeFile, barcode);
                Utility.Move2WorkingFolder();
                Utility.WriteExecuteResult(true);
            }
            else
            {
                MainWindow mainWindow = new MainWindow();
                mainWindow.ShowDialog();
            }
            Application.Current.Shutdown();
            
        }
    }
}
