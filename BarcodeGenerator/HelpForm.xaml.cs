using BarcodeGenerator.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BarcodeGenerator
{
    /// <summary>
    /// HelpForm.xaml 的交互逻辑
    /// </summary>
    public partial class HelpForm : Window
    {
        public HelpForm()
        {
            InitializeComponent();
            lblDescription.Content = stringRes.name + "版本号：" + stringRes.version;
        }
    }
}
