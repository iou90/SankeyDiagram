using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace Kant.Wpf.Controls.Chart
{
    public class SankeyNode
    {
        public Rectangle Shape { get; set; }

        public TextBlock Label { get; set; }

        public IEnumerable<SankeyLink> Links { get; set; }
    }
}
