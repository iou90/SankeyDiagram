using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Kant.Wpf.Controls.Chart
{
    public class SankeyNode
    {
        public SankeyNode()
        {
        }

        public SankeyNode(Rectangle shape, TextBlock label)
        {
            Shape = shape;
            Label = label;
            InLinks = new List<SankeyLink>();
            OutLinks = new List<SankeyLink>();
        }

        public List<SankeyLink> InLinks { get; set; }

        public List<SankeyLink> OutLinks { get; set; }

        public Rectangle Shape { get; set; }

        public TextBlock Label { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// top to bottom: x coordinate
        /// left to right: y coordinate
        /// </summary>
        public double Position1 { get; set; }

        /// <summary>
        /// top to bottom: y coordinate
        /// left to right: x coordinate
        /// </summary>
        public double Position2 { get; set; }

        public bool IsHighlight { get; set; }

        public Brush OriginalShapBrush { get; set; }
        }
}
