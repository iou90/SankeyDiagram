using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Kant.Wpf.Controls.Chart
{
    public class SankeyDataRow
    {
        public SankeyDataRow(string from, string to, double weight, Brush linkBrush = null)
        {
            if(string.IsNullOrEmpty(from))
            {
                throw new ArgumentOutOfRangeException("from node name is null");
            }

            if(string.IsNullOrEmpty(to))
            {
                throw new ArgumentOutOfRangeException("to node name is null");
            }

            From = from;
            To = to;
            Weight = weight;

            if(linkBrush != null)
            {
                LinkBrush = linkBrush;
            }
        }

        public string From { get; }

        public string To { get;}

        public double Weight { get; }

        public Brush LinkBrush { get; }
    }
}
