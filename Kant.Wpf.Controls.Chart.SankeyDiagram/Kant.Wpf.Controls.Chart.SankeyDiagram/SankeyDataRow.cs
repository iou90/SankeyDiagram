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
        public SankeyDataRow(string from, string to, double weight, Brush linkFill = null)
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

            if(linkFill != null)
            {
                LinkFill = linkFill;
            }
        }

        public string From { get; }

        public string To { get;}

        public double Weight { get; }

        public Brush LinkFill { get; }
    }
}
