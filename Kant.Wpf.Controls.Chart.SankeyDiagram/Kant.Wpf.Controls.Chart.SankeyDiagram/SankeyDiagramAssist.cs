using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Kant.Wpf.Controls.Chart
{
    public class SankeyDiagramAssist
    {
        #region Constructor

        public SankeyDiagramAssist(SankeyDiagram diagram, SankeyStyleManager styleManager)
        {
            this.diagram = diagram;
            this.styleManager = styleManager;
        }

        #endregion

        #region Methods

        public void DiagramCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(!diagram.IsDiagramCreated)
            {
                return;
            }

            // avoid reduplicative layout calculating
            if (isCreatingDiagram)
            {
                isCreatingDiagram = false;

                return;
            }

            CreateDiagram();
        }

        public void UpdateDiagram(IEnumerable<SankeyDataRow> datas)
        {
            // clear diagram first
            ClearDiagram();

            if (datas == null || datas.Count() == 0)
            {
                return;
            }

            CurrentLabels = new List<TextBlock>();
            CreateNodesAndLinks(datas);

            // drawing...
            if (diagram.IsDiagramCreated)
            {
                CreateDiagram();
            }
        }

        private void CreateNodesAndLinks(IEnumerable<SankeyDataRow> datas)
        {
            currentSliceNodes = new List<SankeyNode>();
            CurrentLinks = new List<SankeyLink>();
            styleManager.DefaultNodeLinksPaletteIndex = 0;

            foreach (var data in datas)
            {
                if (!currentSliceNodes.Exists(n => n.Name == data.From))
                {
                    currentSliceNodes.Add(CreateNode(data, data.From));
                }

                if (!currentSliceNodes.Exists(n => n.Name == data.To))
                {
                    currentSliceNodes.Add(CreateNode(data, data.To));
                }

                var fromNode = currentSliceNodes.Find(findNode => findNode.Name == data.From);

                if (fromNode != null)
                {
                    var toNode = currentSliceNodes.Find(findNode => findNode.Name == data.To);

                    if (toNode != null)
                    {
                        // merge links which has the same from & to
                        if (CurrentLinks != null)
                        {
                            var previousLink = CurrentLinks.Find(findLink => findLink.FromNode.Name == fromNode.Name && findLink.ToNode.Name == toNode.Name);

                            if (previousLink != null)
                            {
                                previousLink.Weight += data.Weight;

                                continue;
                            }
                        }

                        // create link 
                        var shape = new Path();
                        shape.MouseEnter += LinkMouseEnter;
                        shape.MouseLeave += LinkMouseLeave;
                        shape.MouseLeftButtonUp += LinkMouseLeftButtonUp;
                        shape.Tag = new SankeyLinkFinder(data.From, data.To);
                        shape.Fill = diagram.UsePallette != SankeyPalette.None ? fromNode.Shape.Fill.CloneCurrentValue() : data.LinkBrush == null ? styleManager.DefaultLinkBrush.CloneCurrentValue() : data.LinkBrush.CloneCurrentValue();
                        var link = new SankeyLink(fromNode, toNode, shape, data.Weight, shape.Fill.CloneCurrentValue());
                        fromNode.OutLinks.Add(link);
                        toNode.InLinks.Add(link);
                        CurrentLinks.Add(link);
                    }
                }
            }
        }

        private SankeyNode CreateNode(SankeyDataRow data, string name)
        {
            var text = new TextBlock() { Text = name };
            var shape = new Rectangle();
            shape.Tag = name;

            // for highlighting or other actions
            shape.MouseEnter += NodeMouseEnter;
            shape.MouseLeave += NodeMouseLeave;
            shape.MouseLeftButtonUp += NodeMouseLeftButtonUp;

            if (diagram.SankeyFlowDirection == FlowDirection.TopToBottom)
            {
                shape.Height = diagram.NodeThickness;
            }
            else
            {
                shape.Width = diagram.NodeThickness;
            }

            var node = new SankeyNode(shape, text);
            styleManager.SetNodeBrush(node);
            node.Name = name;

            return node;
        }

        public void ClearDiagram()
        {
            if (!(DiagramCanvas == null || DiagramCanvas.Children == null || DiagramCanvas.Children.Count == 0))
            {
                RemoveElementEventHandlers();
                ClearDiagramLabelMeasuredValue();
                ClearDiagramCanvasChilds();
                CurrentNodes.Clear();
                currentSliceNodes.Clear();
                CurrentLinks.Clear();
                styleManager.ClearHighlight();
            }
        }

        public void ClearDiagramCanvasChilds()
        {
            if (DiagramCanvas == null || DiagramCanvas.Children == null || CurrentLabels == null)
            {
                return;
            }

            DiagramCanvas.Children.Clear();
            CurrentLabels.Clear();
        }

        public void ClearDiagramLabelMeasuredValue()
        {
            measuredFirstLevelLabelWidth = measuredLastLevelLabelWidth = measuredLabelHeight = 0;

            if(currentSliceNodes != null)
            {
                foreach(var node in currentSliceNodes)
                {
                    node.IsLabelSizeMeasured = false;
                }
            }
        }

        public void CreateDiagram()
        {
            ClearDiagramCanvasChilds();

            if (DiagramCanvas.ActualHeight <= 0 || DiagramCanvas.ActualWidth <= 0)
            {
                return;
            }

            isCreatingDiagram = true;

            #region preparing...

            var nodes = CalculateNodeLayout(currentSliceNodes, diagram.NodeThickness);

            if (nodes == null)
            {
                return;
            }
            else
            {
                CurrentNodes = nodes;
            }

            var panelLength = diagram.SankeyFlowDirection == FlowDirection.TopToBottom ? DiagramCanvas.ActualWidth : DiagramCanvas.ActualHeight;
            var unitLength = panelLength;
            var maxNodeCountInOneLevel = 0;

            foreach (var levelNodes in CurrentNodes.Values)
            {
                if (levelNodes.Count > maxNodeCountInOneLevel)
                {
                    maxNodeCountInOneLevel = levelNodes.Count;
                }

                var sum = 0.0;

                if (diagram.SankeyFlowDirection == FlowDirection.TopToBottom)
                {
                    sum = levelNodes.Sum(node => node.Shape.Width);
                }
                else
                {
                    sum = levelNodes.Sum(node => node.Shape.Height);
                }

                var length = (panelLength - (levelNodes.Count - 1) * diagram.NodeGap) / sum;

                if (length < unitLength)
                {
                    unitLength = length;
                }

                if (!(diagram.UsePallette == SankeyPalette.NodesLinks || diagram.NodeBrushes != null))
                {
                    foreach (var node in levelNodes)
                    {
                        node.Shape.Fill = diagram.NodeBrush.CloneCurrentValue();
                    }
                }
            }

            // 15 means you have to remain some margin to calculate node's position, or a wrong position of the top node
            var nodesOverallLength = panelLength - (maxNodeCountInOneLevel * diagram.NodeGap) - 15;

            if (nodesOverallLength <= 0)
            {
                DiagramCanvas.Children.Add(new TextBlock() { Text = "diagram area insufficient" });

                return;
            }

            var labelContainers = new List<Canvas>();
            var relaxation = new SankeyIterativeRelaxation();
            CurrentNodes = relaxation.Calculate(diagram.SankeyFlowDirection, CurrentNodes, CurrentLinks, panelLength, diagram.NodeGap, unitLength, 32);

            #endregion

            #region add nodes

            foreach (var node in currentSliceNodes)
            {
                Canvas.SetTop(node.Shape, node.Y);
                Canvas.SetLeft(node.Shape, node.X);
                DiagramCanvas.Children.Add(node.Shape);

                if (diagram.SankeyFlowDirection == FlowDirection.TopToBottom)
                {
                    node.OutLinks.Sort((l1, l2) => { return (int)(l1.ToNode.X - l2.ToNode.X); });
                    node.InLinks.Sort((l1, l2) => { return (int)(l1.FromNode.X - l2.FromNode.X); });
                }
                else
                {
                    node.OutLinks.Sort((l1, l2) => { return (int)(l1.ToNode.Y - l2.ToNode.Y); });
                    node.InLinks.Sort((l1, l2) => { return (int)(l1.FromNode.Y - l2.FromNode.Y); });
                }

                var fromPosition = 0.0;
                var toPosition = 0.0;

                foreach (var outLink in node.OutLinks)
                {
                    outLink.FromPosition = fromPosition;
                    outLink.Width = outLink.Weight * unitLength;
                    fromPosition += outLink.Width;
                }

                foreach (var inLink in node.InLinks)
                {
                    inLink.ToPosition = toPosition;
                    inLink.Width = inLink.Weight * unitLength;
                    toPosition += inLink.Width;
                }
            }

            #endregion

            #region add links & labels

            foreach (var link in CurrentLinks)
            {
                // make link under the node
                Panel.SetZIndex(link.Shape, -1);
                DiagramCanvas.Children.Add(DrawLink(link).Shape);
            }

            styleManager.OriginalLabelOpacity = this.CurrentNodes[0][0].Label.Opacity;

            for (var index = 0; index < CurrentNodes.Count; index++)
            {
                foreach (var node in CurrentNodes[index])
                {
                    if (!node.IsLabelSizeMeasured)
                    {
                        node.Label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        node.LabelHeight = node.Label.DesiredSize.Height;
                        node.LabelWidth = node.Label.DesiredSize.Width;
                        node.IsLabelSizeMeasured = true;
                    }

                    // restore default position
                    Canvas.SetLeft(node.Label, double.NaN);
                    Canvas.SetRight(node.Label, double.NaN);
                    Canvas.SetTop(node.Label, double.NaN);
                    Canvas.SetBottom(node.Label, double.NaN);

                    if (diagram.SankeyFlowDirection == FlowDirection.TopToBottom)
                    {
                        Canvas.SetLeft(node.Label, node.X + (node.Shape.Width / 2) - (node.LabelWidth / 2));

                        if (diagram.FirstAndLastLabelPosition == FirstAndLastLabelPosition.Inward)
                        {
                            if (index == CurrentNodes.Count - 1)
                            {
                                Canvas.SetBottom(node.Label, node.Shape.Height);
                            }
                            else
                            {
                                Canvas.SetTop(node.Label, node.Y + node.Shape.Height);
                            }
                        }
                        else
                        {
                            if (index == 0)
                            {
                                Canvas.SetTop(node.Label, -node.LabelHeight);
                            }
                            else if (index == CurrentNodes.Count - 1)
                            {
                                Canvas.SetBottom(node.Label, -node.LabelHeight);
                            }
                            else
                            {
                                Canvas.SetTop(node.Label, node.Y + node.Shape.Height);
                            }
                        }
                    }
                    else
                    {
                        Canvas.SetTop(node.Label, node.Y + (node.Shape.Height / 2) - (node.LabelHeight / 2));

                        if (diagram.FirstAndLastLabelPosition == FirstAndLastLabelPosition.Inward)
                        {
                            if (index == CurrentNodes.Count - 1)
                            {
                                Canvas.SetRight(node.Label, node.Shape.Width);
                            }
                            else
                            {
                                Canvas.SetLeft(node.Label, node.X + node.Shape.Width);
                            }
                        }
                        else
                        {
                            if (index == 0)
                            {
                                Canvas.SetLeft(node.Label, -node.LabelWidth);
                            }
                            else if (index == CurrentNodes.Count - 1)
                            {
                                Canvas.SetRight(node.Label, -node.LabelWidth);
                            }
                            else
                            {
                                Canvas.SetLeft(node.Label, node.X + node.Shape.Width);
                            }
                        }
                    }

                    CurrentLabels.Add(node.Label);
                    DiagramCanvas.Children.Add(node.Label);
                }
            }

            styleManager.ChangeLabelsVisibility(diagram.ShowLabels, CurrentLabels);

            #endregion

            isCreatingDiagram = false;
        }

        private Dictionary<int, List<SankeyNode>> CalculateNodeLayout(List<SankeyNode> nodes, double nodeThickness)
        {
            var tempNodes = new Dictionary<double, List<SankeyNode>>();
            var remainNodes = nodes;
            var nextNodes = new List<SankeyNode>();
            var length = 0.0;
            var levelIndex = 0;
            var linkLength = 0.0;

            while(remainNodes.Count > 0)
            {
                nextNodes = new List<SankeyNode>();
                var nodeIndex = 0;
                var nodeCount = remainNodes.Count;

                for(; nodeIndex < nodeCount; nodeIndex++)
                {
                    var node = remainNodes[nodeIndex];

                    if (diagram.SankeyFlowDirection == FlowDirection.TopToBottom)
                    {
                        node.Y = levelIndex;
                    }
                    else
                    {
                        node.X = levelIndex;
                    }

                    var linkIndex = 0;
                    var linkCount = node.OutLinks.Count;

                    for(; linkIndex < linkCount; linkIndex++)
                    {
                        nextNodes.Add(node.OutLinks[linkIndex].ToNode);
                    }
                }

                remainNodes = nextNodes;
                levelIndex++;
            }

            // move all the node without outLinks to end
            foreach(var node in nodes)
            {
                if(node.OutLinks.Count == 0)
                {
                    if (diagram.SankeyFlowDirection == FlowDirection.TopToBottom)
                    {
                        node.Y = levelIndex - 1;
                    }
                    else
                    {
                        node.X = levelIndex - 1;
                    }
                }
            }

            if (diagram.FirstAndLastLabelPosition == FirstAndLastLabelPosition.Outward)
            {
                if (diagram.SankeyFlowDirection == FlowDirection.TopToBottom)
                {
                    if (!(measuredLabelHeight > 0))
                    {
                        nodes[0].Label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        measuredLabelHeight = nodes[0].Label.DesiredSize.Height;
                    }

                    var newHeight = DiagramCanvas.ActualHeight - measuredLabelHeight * 2;

                    if (newHeight <= 0)
                    {
                        DiagramCanvas.Children.Add(new TextBlock() { Text = "diagram area insufficient" });

                        return null;
                    }

                    DiagramCanvas.Margin = new Thickness(0, measuredLabelHeight, 0, measuredLabelHeight);
                }
                else
                {
                    if (!(measuredFirstLevelLabelWidth > 0))
                    {
                        measuredFirstLevelLabelWidth = GetMaxLabelWidth(nodes.FindAll(n => n.X == 0));
                    }

                    if(!(measuredLastLevelLabelWidth > 0))
                    {
                        measuredLastLevelLabelWidth = GetMaxLabelWidth(nodes.FindAll(n => n.X == levelIndex - 1));
                    }

                    var newWidth = diagram.ActualWidth - (measuredLastLevelLabelWidth + measuredFirstLevelLabelWidth);

                    if (newWidth <= 0)
                    {
                        DiagramCanvas.Children.Add(new TextBlock() { Text = "diagram area insufficient" });

                        return null;
                    }

                    DiagramCanvas.Margin = new Thickness(measuredFirstLevelLabelWidth, 0, measuredLastLevelLabelWidth, 0);
                }

                diagram.UpdateLayout();
            }
            else
            {
                DiagramCanvas.Margin = new Thickness(0);
            }

            if (diagram.SankeyFlowDirection == FlowDirection.TopToBottom)
            {
                length = DiagramCanvas.ActualHeight;
            }
            else
            {
                length = DiagramCanvas.ActualWidth;
            }

            linkLength = (length - nodeThickness) / (levelIndex - 1);

            foreach(var node in nodes)
            {
                var max = Math.Max(node.InLinks.Sum(l => l.Weight), node.OutLinks.Sum(l => l.Weight));

                if (diagram.SankeyFlowDirection == FlowDirection.TopToBottom)
                {
                    if (tempNodes.Keys.Contains(node.Y))
                    {
                        tempNodes[node.Y].Add(node);
                    }
                    else
                    {
                        tempNodes.Add(node.Y, new List<SankeyNode>() { node });
                    }

                    node.Y *= linkLength;
                    node.Shape.Width = max;
                    node.Shape.Height = diagram.NodeThickness;
                }
                else
                {
                    if (tempNodes.Keys.Contains(node.X))
                    {
                        tempNodes[node.X].Add(node);
                    }
                    else
                    {
                        tempNodes.Add(node.X, new List<SankeyNode>() { node });
                    }

                    node.X *= linkLength;
                    node.Shape.Height = max;
                    node.Shape.Width = diagram.NodeThickness;
                }
            }

            return tempNodes.OrderBy(n => n.Key).ToDictionary(item => (int)item.Key, item => item.Value);
        }

        private double GetMaxLabelWidth(List<SankeyNode> nodes)
        {
            var max = 0.0;

            foreach(var node in nodes)
            {
                node.Label.Style = diagram.LabelStyle;
                node.Label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                max = node.Label.DesiredSize.Width > max ? node.Label.DesiredSize.Width : max;
            }

            return max;
        }

        private void LinkMouseEnter(object sender, MouseEventArgs e)
        {
            if (diagram.HighlightMode == HighlightMode.MouseEnter)
            {
                diagram.SetCurrentValue(SankeyDiagram.HighlightLinkProperty, (SankeyLinkFinder)((Path)e.OriginalSource).Tag);
            }
        }

        private void LinkMouseLeave(object sender, MouseEventArgs e)
        {
            if (diagram.HighlightMode == HighlightMode.MouseEnter)
            {
                diagram.SetCurrentValue(SankeyDiagram.HighlightLinkProperty, (SankeyLinkFinder)((Path)e.OriginalSource).Tag);
            }
        }

        private void LinkMouseLeftButtonUp(object sender, MouseEventArgs e)
        {
            if (diagram.HighlightMode == HighlightMode.MouseLeftButtonUp)
            {
                diagram.SetCurrentValue(SankeyDiagram.HighlightLinkProperty, (SankeyLinkFinder)((Path)e.OriginalSource).Tag);
            }
        }

        private void NodeMouseEnter(object sender, MouseEventArgs e)
        {
            if (diagram.HighlightMode == HighlightMode.MouseEnter)
            {
                diagram.SetCurrentValue(SankeyDiagram.HighlightNodeProperty, ((Rectangle)e.OriginalSource).Tag as string);
            }
        }

        private void NodeMouseLeave(object sender, MouseEventArgs e)
        {
            if (diagram.HighlightMode == HighlightMode.MouseEnter)
            {
                diagram.SetCurrentValue(SankeyDiagram.HighlightNodeProperty, ((Rectangle)e.OriginalSource).Tag as string);
            }
        }

        private void NodeMouseLeftButtonUp(object sender, MouseEventArgs e)
        {
            if (diagram.HighlightMode == HighlightMode.MouseLeftButtonUp)
            {
                diagram.SetCurrentValue(SankeyDiagram.HighlightNodeProperty, ((Rectangle)e.OriginalSource).Tag as string);
            }
        }

        private SankeyLink DrawLink(SankeyLink link)
        {
            var startPoint = new Point();
            var line1EndPoint = new Point();
            var bezier1ControlPoint1 = new Point();
            var bezier1ControlPoint2 = new Point();
            var bezier1EndPoint = new Point();
            var line2EndPoint = new Point();
            var bezier2ControlPoint1 = new Point();
            var bezier2ControlPoint2 = new Point();

            if(diagram.SankeyFlowDirection == FlowDirection.TopToBottom)
            {
                line1EndPoint.Y = startPoint.Y = link.FromNode.Y + link.FromNode.Shape.Height;
                bezier2ControlPoint2.X = startPoint.X = link.FromNode.X + link.FromPosition;
                bezier1ControlPoint1.X = line1EndPoint.X = startPoint.X + link.Width;
                line2EndPoint.Y = bezier1EndPoint.Y = link.ToNode.Y;
                bezier2ControlPoint1.X = line2EndPoint.X = link.ToNode.X + link.ToPosition;
                bezier1ControlPoint2.X = bezier1EndPoint.X = line2EndPoint.X + link.Width;
                var length = line2EndPoint.Y - line1EndPoint.Y;
                bezier2ControlPoint2.Y = bezier1ControlPoint1.Y = diagram.LinkCurvature * length + startPoint.Y;
                bezier2ControlPoint1.Y = bezier1ControlPoint2.Y = (1 - diagram.LinkCurvature) * length + startPoint.Y;
            }
            else
            {
                line1EndPoint.X = startPoint.X = link.FromNode.X + link.FromNode.Shape.Width;
                bezier2ControlPoint2.Y = startPoint.Y = link.FromNode.Y + link.FromPosition;
                bezier1ControlPoint1.Y = line1EndPoint.Y = startPoint.Y + link.Width;
                line2EndPoint.X = bezier1EndPoint.X = link.ToNode.X;
                bezier2ControlPoint1.Y = line2EndPoint.Y = link.ToNode.Y + link.ToPosition;
                bezier1ControlPoint2.Y = bezier1EndPoint.Y = line2EndPoint.Y + link.Width;
                var length = line2EndPoint.X - line1EndPoint.X;
                bezier2ControlPoint2.X = bezier1ControlPoint1.X = diagram.LinkCurvature * length + startPoint.X; 
                bezier2ControlPoint1.X = bezier1ControlPoint2.X = (1 - diagram.LinkCurvature) * length + startPoint.X; 
            }

            var geometry = new PathGeometry()
            {
                Figures = new PathFigureCollection()
                {
                    new PathFigure()
                    {
                        StartPoint = startPoint,

                        Segments = new PathSegmentCollection()
                        {
                            new LineSegment() { Point = line1EndPoint },

                            new BezierSegment()
                            {
                                Point1 = bezier1ControlPoint1,
                                Point2 = bezier1ControlPoint2,
                                Point3 = bezier1EndPoint
                            },

                            new LineSegment() { Point = line2EndPoint },

                            new BezierSegment()
                            {
                                Point1 = bezier2ControlPoint1,
                                Point2 = bezier2ControlPoint2,
                                Point3 = startPoint
                            }
                        }
                    },
                }
            };

            link.Shape.Data = geometry;

            return link;
        }

        private void RemoveElementEventHandlers()
        {
            if (CurrentLinks != null && CurrentNodes != null && DiagramCanvas != null)
            {
                DiagramCanvas.SizeChanged -= DiagramCanvasSizeChanged;

                foreach(var levelNodes in CurrentNodes.Values)
                {
                    foreach(var node in levelNodes)
                    {
                        node.Shape.MouseEnter -= NodeMouseEnter;
                        node.Shape.MouseLeave -= NodeMouseLeave;
                        node.Shape.MouseLeftButtonUp -= NodeMouseLeftButtonUp;
                    }
                }

                foreach (var link in CurrentLinks)
                {
                    link.Shape.MouseEnter -= LinkMouseEnter;
                    link.Shape.MouseLeave -= LinkMouseLeave;
                    link.Shape.MouseLeftButtonUp -= LinkMouseLeftButtonUp;
                }
            }
        }

        #endregion

        #region Fields & Properties

        public List<TextBlock> CurrentLabels { get; private set; }

        /// <summary>
        /// key means depth
        /// </summary>
        public Dictionary<int, List<SankeyNode>> CurrentNodes { get; private set; }

        public List<SankeyLink> CurrentLinks { get; private set; }

        public Canvas DiagramCanvas { get; set; }

        private bool isCreatingDiagram;

        private double measuredLabelHeight;

        private double measuredFirstLevelLabelWidth;

        private double measuredLastLevelLabelWidth;

        private List<SankeyNode> currentSliceNodes;

        private SankeyDiagram diagram;

        private SankeyStyleManager styleManager;

        #endregion
    }
}
