using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Kant.Wpf.Controls.Chart
{
    public class SankeyLink
    {
        public SankeyLink()
        {
        }

        public SankeyLink(SankeyNode fromNode, SankeyNode toNode, Path shape, double weight, Brush originalShapeBrush)
        {
            FromNode = fromNode;
            ToNode = toNode;
            Shape = shape;
            Weight = weight;
            OriginalShapBrush = originalShapeBrush;
        }

        public SankeyNode FromNode { get; set; }

        public SankeyNode ToNode { get; set; }

        public Path Shape { get; set; }

        public double FromPosition { get; set; }

        public double ToPosition { get; set; }

        public double Weight { get; set; }

        public double Width { get; set; }

        public bool IsHighlight { get; set; }

        public Brush OriginalShapBrush { get; set; }
    }
}
