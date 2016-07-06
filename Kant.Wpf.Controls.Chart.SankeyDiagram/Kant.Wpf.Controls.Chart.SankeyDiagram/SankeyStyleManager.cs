using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Kant.Wpf.Controls.Chart
{
    public class SankeyStyleManager
    {
        #region Constructor

        public SankeyStyleManager(SankeyDiagram diagram)
        {
            this.diagram = diagram;
            SetDefaultStyles();
        }

        #endregion

        #region Methods

        public void SetDefaultStyles()
        {
            var opacity = 0.55;
            diagram.NodeGap = 5;
            diagram.NodeThickness = 10;
            diagram.HighlightOpacity = 1.0;
            diagram.LoweredOpacity = 0.25;
            diagram.UsePallette = SankeyPalette.NodesLinks;
            defaultNodeLinksPalette = GetNodeLinksPalette(opacity);
            DefaultLinkBrush = new SolidColorBrush(Colors.Gray) { Opacity = opacity };
        }

        public void UpdateLinkCurvature(double curvature, List<SankeyLink> links)
        {
            if(!(curvature > 0 && curvature <= 1) || links == null || links.Count == 0)
            {
                return;
            }

            foreach(var link in links)
            {
                if(link.Shape.Data == null)
                {
                    continue;
                }

                var figure = ((PathGeometry)(link.Shape.Data)).Figures[0];
                var startPoint = figure.StartPoint;
                var line1EndPoint = ((LineSegment)figure.Segments[0]).Point;
                var line2EndPoint = ((LineSegment)figure.Segments[2]).Point;
                var bezier1 = (BezierSegment)figure.Segments[1];
                var bezier2 = (BezierSegment)figure.Segments[3];
                var bezier1ControlPoint1 = new Point(); 
                var bezier1ControlPoint2 = new Point();
                var bezier2ControlPoint1 = new Point();
                var bezier2ControlPoint2 = new Point();

                if (diagram.SankeyFlowDirection == FlowDirection.TopToBottom)
                {
                    var length = line2EndPoint.Y - line1EndPoint.Y;
                    bezier2ControlPoint2.X = startPoint.X;
                    bezier1ControlPoint1.X = line1EndPoint.X;
                    bezier2ControlPoint1.X = line2EndPoint.X;
                    bezier1ControlPoint2.X = line2EndPoint.X + link.Width;
                    bezier2ControlPoint2.Y = bezier1ControlPoint1.Y = curvature * length + startPoint.Y;
                    bezier2ControlPoint1.Y = bezier1ControlPoint2.Y = (1 - curvature) * length + startPoint.Y;
                }
                else
                {
                    var length = line2EndPoint.X - line1EndPoint.X;
                    bezier2ControlPoint2.Y = startPoint.Y;
                    bezier1ControlPoint1.Y = line1EndPoint.Y;
                    bezier2ControlPoint1.Y = line2EndPoint.Y;
                    bezier1ControlPoint2.Y = line2EndPoint.Y + link.Width;
                    bezier2ControlPoint2.X = bezier1ControlPoint1.X = curvature * length + startPoint.X;
                    bezier2ControlPoint1.X = bezier1ControlPoint2.X = (1 - curvature) * length + startPoint.X;
                }

                bezier1.Point1 = bezier1ControlPoint1;
                bezier1.Point2 = bezier1ControlPoint2;
                bezier2.Point1 = bezier2ControlPoint1;
                bezier2.Point2 = bezier2ControlPoint2;
            }
        }

        public void SetNodeBrush(SankeyNode node)
        {
            var brushCheck = diagram.NodeBrushes != null && diagram.NodeBrushes.Keys.Contains(node.Name);

            if (brushCheck)
            {
                node.Shape.Fill = diagram.NodeBrushes[node.Name].CloneCurrentValue();
            }
            else
            {
                if (diagram.UsePallette != SankeyPalette.None)
                {
                    node.Shape.Fill = defaultNodeLinksPalette[DefaultNodeLinksPaletteIndex].CloneCurrentValue();
                    DefaultNodeLinksPaletteIndex++;

                    if (DefaultNodeLinksPaletteIndex >= defaultNodeLinksPalette.Count)
                    {
                        DefaultNodeLinksPaletteIndex = 0;
                    }
                }
            }

            node.OriginalShapBrush = node.Shape.Fill.CloneCurrentValue();
        }

        public void UpdateNodeBrushes(Dictionary<string, Brush> newBrushes, Dictionary<int, List<SankeyNode>> nodes, List<SankeyLink> links)
        {
            if (diagram == null || newBrushes == null || nodes == null || nodes.Count() < 2)
            {
                return;
            }

            ClearHighlight();
            var brushChangedNodes = new List<string>();

            foreach (var levelNodes in nodes.Values)
            {
                foreach (var node in levelNodes)
                {
                    var key = node.Name;

                    if (newBrushes.Keys.Contains(key))
                    {
                        brushChangedNodes.Add(key);
                        var brush = newBrushes[node.Name];
                        node.Shape.Fill = brush.CloneCurrentValue();
                        node.OriginalShapBrush = brush.CloneCurrentValue();
                    }
                }
            }

            if(brushChangedNodes.Count == 0)
            {
                return;
            }

            if (diagram.UsePallette != SankeyPalette.None)
            {
                if (links == null || links.Count == 0)
                {
                    return;
                }

                foreach (var link in links)
                {
                    if (brushChangedNodes.Contains(link.FromNode.Name))
                    {
                        var brush = link.FromNode.Shape.Fill;
                        link.Shape.Fill = brush.CloneCurrentValue();
                        link.OriginalShapBrush = brush.CloneCurrentValue();
                    }
                }
            }
        }

        public void UpdateNodeBrushes(Brush brush, Dictionary<int, List<SankeyNode>> nodes)
        {
            if(diagram == null || brush == null || nodes == null || nodes.Count < 2)
            {
                return;
            }

            if(diagram.UsePallette == SankeyPalette.NodesLinks)
            {
                return;
            }

            ClearHighlight();

            foreach(var levelNodes in nodes.Values)
            {
                foreach(var node in levelNodes)
                {
                    node.Shape.Fill = brush.CloneCurrentValue();
                }
            }
        }

        public void ClearHighlight()
        {
            diagram.SetCurrentValue(SankeyDiagram.HighlightNodeProperty, null);
            diagram.SetCurrentValue(SankeyDiagram.HighlightLinkProperty, null);
        }

        public void ChangeLabelsVisibility(bool showLabels, List<TextBlock> labels)
        {
            if(labels == null)
            {
                return;
            }

            foreach (var label in labels)
            {
                label.Visibility = showLabels ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void HighlightingNode(string highlightNode, Dictionary<int, List<SankeyNode>> nodes, List<SankeyLink> links)
        {
            if ((string.IsNullOrEmpty(highlightNode) && string.IsNullOrEmpty(diagram.HighlightNode) || nodes == null || nodes.Count < 2 || links == null || links.Count == 0))
            {
                return;
            }

            // reset each element's style first
            RecoverFromHighlights(links, false);

            var resetBrushes = true;
            var highlightNodes = new List<string>();
            var minimizeNodes = new List<string>();

            // check whether highlight node exists
            if (!string.IsNullOrEmpty(highlightNode))
            {
                if ((from node in nodes.Values.SelectMany(n => n) where node.Name == highlightNode select node).Count() == 1)
                {
                    resetBrushes = false;
                };
            }

            // reset highlight if highlighting the same node twice
            if (highlightNode == diagram.HighlightNode && (from node in nodes.Values.SelectMany(n => n) where node.Name == diagram.HighlightNode & node.IsHighlight select node).Count() == 1)
            {
                RecoverFromHighlights(links);

                return;
            }

            // for node, link highlighting switching
            diagram.HighlightLink = null;

            // increasing opacity of the correlated element while lower the others  
            Highlighting(links, resetBrushes, diagram.HighlightOpacity, diagram.LoweredOpacity, highlightNodes, minimizeNodes, new Func<SankeyLink, bool>(link => { return link.FromNode.Name == highlightNode || link.ToNode.Name == highlightNode; }));
        }

        public void HighlightingLink(SankeyLinkFinder linkFinder, Dictionary<int, List<SankeyNode>> nodes, List<SankeyLink> links)
        {
            if ((linkFinder == null && diagram.HighlightLink == null || nodes == null || nodes.Count < 2 || links == null || links.Count == 0))
            {
                return;
            }

            // reset each element's style first
            RecoverFromHighlights(links, false);

            var resetBrushes = true;
            var highlightNodes = new List<string>();
            var minimizeNodes = new List<string>();

            // check whether highlight node exist
            if (linkFinder != null)
            {
                var fromNode = (from node in nodes.Values.SelectMany(n => n) where node.Name == linkFinder.From select node).FirstOrDefault();
                var toNode = (from node in nodes.Values.SelectMany(n => n) where node.Name == linkFinder.To select node).FirstOrDefault();

                if (fromNode != null && toNode != null)
                {
                    if ((from link in links where link.FromNode.Name == linkFinder.From & link.ToNode.Name == linkFinder.To select link).Count() > 0)
                    {
                        resetBrushes = false;
                    }
                };
            }

            // reset highlight if highlighting the same link twice
            if (diagram.HighlightLink != null && linkFinder != null)
            {
                if ((diagram.HighlightLink.From == linkFinder.From && diagram.HighlightLink.To == linkFinder.To) && (from link in links where (link.FromNode.Name == diagram.HighlightLink.From && link.ToNode.Name == diagram.HighlightLink.To) & link.IsHighlight select link).Count() == 1)
                {
                    RecoverFromHighlights(links);

                    return;
                }
            }

            // for node, link highlighting switching
            diagram.HighlightNode = null;

            // increasing opacity of the correlated element while lower the others  
            Highlighting(links, resetBrushes, diagram.HighlightOpacity, diagram.LoweredOpacity, highlightNodes, minimizeNodes, new Func<SankeyLink, bool>(link => { return link.FromNode.Name == linkFinder.From && link.ToNode.Name == linkFinder.To; }));

            //// tooltip
            //var highlightLink = links.Find(l => l.FromNode.Name == linkFinder.From && l.ToNode.Name == linkFinder.To);

            //if(highlightLink != null)
            //{
            //    var toolTip = new ToolTip();
            //    //toolTip.te
            //    //highlightLink.Shape.ToolTip = ;
            //}
        }

        private void Highlighting(List<SankeyLink> links, bool resetBrushes, double highlightOpacity, double loweredOpacity, List<string> highlightNodes, List<string> minimizeNodes, Func<SankeyLink, bool> isHighlightedElement)
        {
            var setStyle = new Action<FrameworkElement, Style>((e, style) =>
            {
                if (style != null)
                {
                    e.Style = style;
                }
            });

            foreach (var link in links)
            {
                if (!resetBrushes)
                {
                    if (isHighlightedElement(link))
                    {
                        link.FromNode.Shape.Fill.Opacity = link.ToNode.Shape.Fill.Opacity = link.Shape.Fill.Opacity = highlightOpacity;
                        link.IsHighlight = true;
                        link.FromNode.IsHighlight = true;
                        link.ToNode.IsHighlight = true;
                        highlightNodes.Add(link.FromNode.Name);
                        highlightNodes.Add(link.ToNode.Name);
                        link.ToNode.Label.Opacity = link.FromNode.Label.Opacity = highlightOpacity;

                        if (diagram.HighlightBrush != null)
                        {
                            link.Shape.Fill = diagram.HighlightBrush.CloneCurrentValue();
                            link.FromNode.Shape.Fill = diagram.HighlightBrush.CloneCurrentValue();
                            link.ToNode.Shape.Fill = diagram.HighlightBrush.CloneCurrentValue();
                        }

                        if (diagram.HighlightLabelStyle != null)
                        {
                            link.ToNode.Label.Style = link.FromNode.Label.Style = diagram.HighlightLabelStyle;
                        }
                    }
                    else
                    {
                        var minimizeOpacity = link.Shape.Fill.Opacity - loweredOpacity < 0 ? 0 : link.Shape.Fill.Opacity - loweredOpacity;
                        link.Shape.Fill.Opacity = minimizeOpacity;
                        link.IsHighlight = false;

                        // prevent changing node's brush again
                        if (!highlightNodes.Exists(n => n == link.FromNode.Name) && !minimizeNodes.Exists(n => n == link.FromNode.Name))
                        {
                            MinimizeNode(link.FromNode, minimizeOpacity, minimizeNodes);
                        }

                        if (!highlightNodes.Exists(n => n == link.ToNode.Name) && !minimizeNodes.Exists(n => n == link.ToNode.Name))
                        {
                            MinimizeNode(link.ToNode, minimizeOpacity, minimizeNodes);
                        }
                    }
                }
                else
                {
                    RecoverFromHighlights(link);
                }
            }
        }

        public void RecoverFromHighlights(List<SankeyLink> links, bool resetHighlightStatus = true)
        {
            foreach (var link in links)
            {
                RecoverFromHighlights(link, resetHighlightStatus);
            }
        }

        private void RecoverFromHighlights(SankeyLink link, bool resetHighlightStatus = true)
        {
            link.Shape.Fill = link.OriginalShapBrush.CloneCurrentValue();
            link.FromNode.Shape.Fill = link.FromNode.OriginalShapBrush.CloneCurrentValue();
            link.ToNode.Shape.Fill = link.ToNode.OriginalShapBrush.CloneCurrentValue();
            link.ToNode.Label.Style = link.FromNode.Label.Style = diagram.LabelStyle;
            link.ToNode.Label.Opacity = link.FromNode.Label.Opacity = OriginalLabelOpacity;

            if (resetHighlightStatus)
            {
                link.IsHighlight = false;
                link.FromNode.IsHighlight = false;
                link.ToNode.IsHighlight = false;
            }
        }

        private void MinimizeNode(SankeyNode node, double minimizeOpacity, List<string> minimizeNodes)
        {
            node.Shape.Fill.Opacity = minimizeOpacity;
            node.IsHighlight = false;
            minimizeNodes.Add(node.Name);
            node.Label.Opacity = minimizeOpacity;
        }

        private List<Brush> GetNodeLinksPalette(double opacity)
        {
            return new List<Brush>()
            {
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0095fb")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff0000")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffa200")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00f2c8")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7373ff")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#91bc61")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc89d9")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fff100")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#44c5f1")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#85e91f")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00b192")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1cbe65")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#278bcc")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#954ab3")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f3bc00")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e47403")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ce3e29")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d8dddf")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60e8a4")) { Opacity = opacity },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffb5ff")) { Opacity = opacity }
            };
        }

        #endregion

        #region Fields & Properties

        public int DefaultNodeLinksPaletteIndex { get; set; }

        public Brush DefaultLinkBrush { get; set; }

        public double OriginalLabelOpacity { get; set; }

        private SankeyDiagram diagram;

        private List<Brush> defaultNodeLinksPalette;

        #endregion
    }
}
