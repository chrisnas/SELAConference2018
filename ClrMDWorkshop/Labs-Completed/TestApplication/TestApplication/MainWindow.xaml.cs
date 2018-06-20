using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;

namespace TestApplication
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, List<string>> _strings;

        public MainWindow()
        {
            InitializeComponent();

            Title = $"{Process.GetCurrentProcess().Id.ToString()} - {Title}";
        }

        private void btnStringDuplicates_Click(object sender, RoutedEventArgs e)
        {
            _strings = new Dictionary<string, List<string>>();
            _strings["0123456789"] = new List<string>(GetStringList(128));
            _strings["_123456789"] = new List<string>(GetStringList("_123456789", 128));
        }

        private string GetNewString()
        {
            var copy = new StringBuilder();
            for (int i = 0; i < 10; i++)
            {
                copy.Append(i.ToString());
            }
            return copy.ToString();
        }

        private List<string> GetStringList(string s, int count)
        {
            var strings = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                strings.Add(s);
            }

            return strings;
        }
        private List<string> GetStringList(int count)
        {
            var strings = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                strings.Add(GetNewString());
            }

            return strings;
        }
    }
}
