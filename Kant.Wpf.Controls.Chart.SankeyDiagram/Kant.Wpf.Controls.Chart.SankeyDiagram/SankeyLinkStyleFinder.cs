using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Kant.Wpf.Controls.Chart
{
    public class SankeyLinkStyleFinder
    {
        public SankeyLinkStyleFinder(string from, string to)
        {
            From = from;
            To = to;
        }

        public string From { get; }

        public string To { get; }

        public Brush Brush { get; set; }
    }
}
