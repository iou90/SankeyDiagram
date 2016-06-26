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

        /// <summary>
        /// for iterative relaxation 
        /// </summary>
        public double CalculatingCoordinate { get; set; }

        #region for caculating link position

        public Point Position { get; set; }

        public double PreviousOccupiedLength { get; set; }

        public double NextOccupiedLength { get; set; }

        #endregion

        public bool IsHighlight { get; set; }

        public Brush OriginalShapBrush { get; set; }
    }
}
