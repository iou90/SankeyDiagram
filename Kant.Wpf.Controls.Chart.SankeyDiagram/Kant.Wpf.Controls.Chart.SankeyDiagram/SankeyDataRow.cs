using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kant.Wpf.Controls.Chart
{
    public class SankeyDataRow
    {
        public string From { get; set; }

        public string To { get; set; }

        public double Weight { get; set; }
    }
}
