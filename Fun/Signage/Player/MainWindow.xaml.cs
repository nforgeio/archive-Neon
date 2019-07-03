using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Couchbase.Lite;

using Neon.Stack.Common;

namespace SignagePlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // ForestDB works only for 64-bit builds.

            Manager.SharedInstance.StorageType = NeonHelper.Is64Bit ? StorageEngineTypes.ForestDB : StorageEngineTypes.SQLite;

            var db = Manager.SharedInstance.GetDatabase("test2");
            var doc = db.GetDocument("1234");

            doc.PutProperties(
                new Dictionary<string, object>()
                {
                    { "hello", "world" }
                });

            doc = db.GetDocument("1234");

            var props = doc.Properties;
        }
    }
}
