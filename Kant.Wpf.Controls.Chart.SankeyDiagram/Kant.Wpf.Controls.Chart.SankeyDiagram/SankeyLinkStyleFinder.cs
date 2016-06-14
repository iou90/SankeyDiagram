using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Kant.Wpf.Controls.Chart
{
    public class SankeyLinkStyleFinder : SankeyLinkFinder
    {
        public SankeyLinkStyleFinder(string from, string to) : base(from, to)
        {
        }

        public Brush Brush { get; set; }
    }
}
