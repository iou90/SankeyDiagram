using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace Kant.Wpf.Controls.Chart
{
    public class SankeyLink
    {
        public SankeyLink()
        {
        }

        public SankeyLink(SankeyNode fromNode, SankeyNode toNode, Path shape)
        {
            FromNode = fromNode;
            ToNode = toNode;
            Shape = shape;
        }

        public SankeyNode FromNode { get; set; }

        public SankeyNode ToNode { get; set; }

        public Path Shape { get; set; }
    }
}
