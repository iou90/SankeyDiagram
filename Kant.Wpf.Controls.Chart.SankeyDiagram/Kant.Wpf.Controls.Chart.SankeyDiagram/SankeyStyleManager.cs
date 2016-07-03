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
            var labelStye = new Style(typeof(TextBlock));
            labelStye.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(2)));
            diagram.LabelStyle = labelStye;
            diagram.UsePallette = SankeyPalette.NodesLinks;
            DefaultNodeLinksPalette = GetNodeLinksPalette(opacity);
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
                var bezier = (BezierSegment)figure.Segments[0];
                var fromPoint = figure.StartPoint;
                var toPoint = bezier.Point3;
                var bezierControlPoint1 = new Point();
                var bezierCOntrolPoint2 = new Point();

                if (diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                {
                    var length = toPoint.Y - fromPoint.Y;
                    bezierControlPoint1.X = fromPoint.X;
                    bezierControlPoint1.Y = length * diagram.LinkCurvature + fromPoint.Y;
                    bezierCOntrolPoint2.X = toPoint.X;
                    bezierCOntrolPoint2.Y = length * (1 - diagram.LinkCurvature) + fromPoint.Y;
                }
                else
                {
                    var length = toPoint.X - fromPoint.X;
                    bezierControlPoint1.Y = fromPoint.Y;
                    bezierControlPoint1.X = length * diagram.LinkCurvature + fromPoint.X;
                    bezierCOntrolPoint2.Y = toPoint.Y;
                    bezierCOntrolPoint2.X = length * (1 - diagram.LinkCurvature) + fromPoint.X;
                }

                bezier.Point1 = bezierControlPoint1;
                bezier.Point2 = bezierCOntrolPoint2;
            }
        }

        public void UpdateNodeBrushes(Dictionary<string, Brush> newBrushes, Dictionary<int, List<SankeyNode>> nodes, List<SankeyLink> links)
        {
            if (diagram == null || newBrushes == null || nodes == null || nodes.Count() < 2)
            {
                return;
            }

            ResetHighlight();
            var brushChangedNodes = new List<string>();

            foreach (var levelNodes in nodes.Values)
            {
                foreach (var node in levelNodes)
                {
                    var key = node.Label.Text;

                    if (newBrushes.Keys.Contains(key))
                    {
                        brushChangedNodes.Add(key);
                        var brush = newBrushes[node.Label.Text];
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
                    if (brushChangedNodes.Contains(link.FromNode.Label.Text))
                    {
                        var brush = link.FromNode.Shape.Fill;
                        link.Shape.Stroke = brush.CloneCurrentValue();
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

            ResetHighlight();

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
            ResetHighlight();
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
                if ((from node in nodes.Values.SelectMany(n => n) where node.Label.Text == highlightNode select node).Count() == 1)
                {
                    resetBrushes = false;
                };
            }

            // reset highlight if highlighting the same node twice
            if (highlightNode == diagram.HighlightNode && (from node in nodes.Values.SelectMany(n => n) where node.Label.Text == diagram.HighlightNode & node.IsHighlight select node).Count() == 1)
            {
                RecoverFromHighlights(links);

                return;
            }

            // for node, link highlighting switching
            diagram.HighlightLink = null;

            // increasing opacity of the correlated element while lower the others  
            Highlighting(links, resetBrushes, diagram.HighlightOpacity, diagram.LoweredOpacity, highlightNodes, minimizeNodes, new Func<SankeyLink, bool>(link => { return link.FromNode.Label.Text == highlightNode || link.ToNode.Label.Text == highlightNode; }));
        }

        public void HighlightingLink(SankeyLinkFinder linkStyleFinder, Dictionary<int, List<SankeyNode>> nodes, List<SankeyLink> links)
        {
            if ((linkStyleFinder == null && diagram.HighlightLink == null || nodes == null || nodes.Count < 2 || links == null || links.Count == 0))
            {
                return;
            }

            // reset each element's style first
            RecoverFromHighlights(links, false);

            var resetBrushes = true;
            var highlightNodes = new List<string>();
            var minimizeNodes = new List<string>();

            // check whether highlight node exist
            if (linkStyleFinder != null)
            {
                var fromNode = (from node in nodes.Values.SelectMany(n => n) where node.Label.Text == linkStyleFinder.From select node).FirstOrDefault();
                var toNode = (from node in nodes.Values.SelectMany(n => n) where node.Label.Text == linkStyleFinder.To select node).FirstOrDefault();

                if (fromNode != null && toNode != null)
                {
                    if ((from link in links where link.FromNode.Label.Text == linkStyleFinder.From & link.ToNode.Label.Text == linkStyleFinder.To select link).Count() > 0)
                    {
                        resetBrushes = false;
                    }
                };
            }

            // reset highlight if highlighting the same link twice
            if (diagram.HighlightLink != null && linkStyleFinder != null)
            {
                if ((diagram.HighlightLink.From == linkStyleFinder.From && diagram.HighlightLink.To == linkStyleFinder.To) && (from link in links where (link.FromNode.Label.Text == diagram.HighlightLink.From && link.ToNode.Label.Text == diagram.HighlightLink.To) & link.IsHighlight select link).Count() == 1)
                {
                    RecoverFromHighlights(links);

                    return;
                }
            }

            // for node, link highlighting switching
            diagram.HighlightNode = null;

            // increasing opacity of the correlated element while lower the others  
            Highlighting(links, resetBrushes, diagram.HighlightOpacity, diagram.LoweredOpacity, highlightNodes, minimizeNodes, new Func<SankeyLink, bool>(link => { return link.FromNode.Label.Text == linkStyleFinder.From && link.ToNode.Label.Text == linkStyleFinder.To; }));
        }

        private void Highlighting(List<SankeyLink> links, bool resetBrushes, double highlightOpacity, double loweredOpacity, List<string> highlightNodes, List<string> minimizeNodes, Func<SankeyLink, bool> check)
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
                    if (check(link))
                    {
                        link.FromNode.Shape.Fill.Opacity = link.ToNode.Shape.Fill.Opacity = link.Shape.Stroke.Opacity = highlightOpacity;
                        link.IsHighlight = true;
                        link.FromNode.IsHighlight = true;
                        link.ToNode.IsHighlight = true;
                        highlightNodes.Add(link.FromNode.Label.Text);
                        highlightNodes.Add(link.ToNode.Label.Text);
                        link.ToNode.Label.Opacity = link.FromNode.Label.Opacity = highlightOpacity;

                        if (diagram.HighlightBrush != null)
                        {
                            link.Shape.Stroke = diagram.HighlightBrush.CloneCurrentValue();
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
                        var minimizeOpacity = link.Shape.Stroke.Opacity - loweredOpacity < 0 ? 0 : link.Shape.Stroke.Opacity - loweredOpacity;
                        link.Shape.Stroke.Opacity = minimizeOpacity;
                        link.IsHighlight = false;

                        // prevent changing node's brush again
                        if (!highlightNodes.Exists(n => n == link.FromNode.Label.Text) && !minimizeNodes.Exists(n => n == link.FromNode.Label.Text))
                        {
                            MinimizeNode(link.FromNode, minimizeOpacity, minimizeNodes);
                        }

                        if (!highlightNodes.Exists(n => n == link.ToNode.Label.Text) && !minimizeNodes.Exists(n => n == link.ToNode.Label.Text))
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
            link.Shape.Stroke = link.OriginalShapBrush.CloneCurrentValue();
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
            minimizeNodes.Add(node.Label.Text);
            node.Label.Opacity = minimizeOpacity;
        }

        private void ResetHighlight()
        {
            diagram.SetCurrentValue(SankeyDiagram.HighlightNodeProperty, null);
            diagram.SetCurrentValue(SankeyDiagram.HighlightLinkProperty, null);
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

        public List<Brush> DefaultNodeLinksPalette { get; set; }

        public double OriginalLabelOpacity { get; set; }

        private SankeyDiagram diagram;

        #endregion
    }
}
