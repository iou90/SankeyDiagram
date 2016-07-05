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

        public void UpdateDiagram(IEnumerable<SankeyDataRow> datas)
        {
            // clear diagram
            if (!(diagram.DiagramCanvas == null || diagram.DiagramCanvas.Children == null || diagram.DiagramCanvas.Children.Count == 0))
            {
                RemoveElementEventHandlers();
                diagram.DiagramCanvas.Children.Clear();
                CurrentNodes.Clear();
                CurrentLabels.Clear();
                CurrentLinks.Clear();
                styleManager.ClearHighlight();
            }

            if (datas == null || datas.Count() == 0)
            {
                return;
            }

            CurrentLabels = new List<TextBlock>();
            styleManager.DefaultNodeLinksPaletteIndex = 0;

            // drawing...
            if (diagram.IsDiagramCreated)
            {
                CreateDiagram(datas);
            }
        }
        
        public void CreateDiagram(IEnumerable<SankeyDataRow> datas)
        {
            if (datas == null || datas.Count() == 0 || diagram.DiagramCanvas.ActualHeight <= 0 || diagram.DiagramCanvas.ActualWidth <= 0)
            {
                return;
            }

            #region preparing...

            var nodes = CreateNodes(datas);
            var panelLength = diagram.SankeyFlowDirection == FlowDirection.TopToBottom ? diagram.DiagramCanvas.Width : diagram.DiagramCanvas.Height;
            var unitLength = panelLength;
            var maxNodeCountInOneLevel = 0;

            foreach (var levelNodes in CurrentNodes.Values)
            {
                if(levelNodes.Count > maxNodeCountInOneLevel)
                {
                    maxNodeCountInOneLevel = levelNodes.Count;
                }

                var sum = 0.0;

                if(diagram.SankeyFlowDirection == FlowDirection.TopToBottom)
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
                diagram.DiagramCanvas.Children.Add(new TextBlock() { Text = "diagram panel length is not enough" });

                return;
            }

            var labelContainers = new List<Canvas>();
            var relaxation = new SankeyIterativeRelaxation();
            CurrentNodes = relaxation.Calculate(diagram.SankeyFlowDirection, CurrentNodes, CurrentLinks, panelLength, diagram.NodeGap, unitLength, 32);

            #endregion
 
            #region add nodes

            foreach (var node in nodes)
            {
                Canvas.SetTop(node.Shape, node.Y);
                Canvas.SetLeft(node.Shape, node.X);
                diagram.DiagramCanvas.Children.Add(node.Shape);

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
                diagram.DiagramCanvas.Children.Add(DrawLink(link).Shape);
            }

            var needAddLabels = CurrentLabels.Count == 0 ? true : false;

            if (needAddLabels)
            {
                styleManager.OriginalLabelOpacity = this.CurrentNodes[0][0].Label.Opacity;

                for(var index = 0; index < CurrentNodes.Count; index++)
                {
                    foreach(var node in CurrentNodes[index])
                    {
                        node.Label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                        if (diagram.SankeyFlowDirection == FlowDirection.TopToBottom)
                        {
                            Canvas.SetLeft(node.Label, node.X + (node.Shape.Width / 2) - (node.Label.DesiredSize.Width / 2));

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
                                if(index == 0)
                                {
                                    Canvas.SetTop(node.Label, - node.Label.DesiredSize.Height);
                                }
                                else if (index == CurrentNodes.Count - 1)
                                {
                                    Canvas.SetBottom(node.Label, -node.Label.DesiredSize.Height);
                                }
                                else
                                {
                                    Canvas.SetTop(node.Label, node.Y + node.Shape.Height);
                                }
                            }
                        }
                        else
                        {
                            Canvas.SetTop(node.Label, node.Y + (node.Shape.Height / 2) - (node.Label.DesiredSize.Height / 2));

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
                                    Canvas.SetLeft(node.Label, -node.Label.DesiredSize.Width);
                                }
                                else if (index == CurrentNodes.Count - 1)
                                {
                                    Canvas.SetRight(node.Label, -node.Label.DesiredSize.Width);
                                }
                                else
                                {
                                    Canvas.SetLeft(node.Label, node.X + node.Shape.Width);
                                }
                            }
                        }

                        CurrentLabels.Add(node.Label);
                        diagram.DiagramCanvas.Children.Add(node.Label);
                    }
                }
            }

            styleManager.ChangeLabelsVisibility(diagram.ShowLabels, CurrentLabels);

            #endregion
        }

        private List<SankeyNode> CreateNodes(IEnumerable<SankeyDataRow> datas)
        {
            var nodes = new List<SankeyNode>();
            var links = new List<SankeyLink>();
            styleManager.DefaultNodeLinksPaletteIndex = 0;

            foreach(var data in datas)
            {
                if(!nodes.Exists(n => n.Name == data.From))
                {
                    nodes.Add(CreateNode(data, data.From));
                }

                if (!nodes.Exists(n => n.Name == data.To))
                {
                    nodes.Add(CreateNode(data, data.To));
                }

                var fromNode = nodes.Find(findNode => findNode.Name == data.From);

                if(fromNode != null)
                {
                    var toNode = nodes.Find(findNode => findNode.Name == data.To);

                    if(toNode != null)
                    {
                        // merge links which has the same from & to
                        if (links != null)
                        {
                            var previousLink = links.Find(findLink => findLink.FromNode.Name == fromNode.Name && findLink.ToNode.Name == toNode.Name);

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
                        links.Add(link);
                    }
                }
            }

            // recalculate canvas size
            if (diagram.FirstAndLastLabelPosition == FirstAndLastLabelPosition.Outward)
            {
                if (diagram.SankeyFlowDirection == FlowDirection.TopToBottom)
                {
                    nodes[0].Label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    var labelHeight = nodes[0].Label.DesiredSize.Height;
                    diagram.DiagramCanvas.Height = diagram.DiagramCanvas.ActualHeight - labelHeight * 2;
                    diagram.DiagramCanvas.Width = diagram.DiagramCanvas.ActualWidth;
                }
            }
            else
            {
                diagram.DiagramCanvas.Height = diagram.DiagramCanvas.ActualHeight;
                diagram.DiagramCanvas.Width = diagram.DiagramCanvas.ActualWidth;
            }

            CurrentLinks = links;
            CurrentNodes = CalculateNodeLevel(nodes, diagram.NodeThickness);

            return nodes;
        }

        private Dictionary<int, List<SankeyNode>> CalculateNodeLevel(List<SankeyNode> nodes, double nodeThickness)
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
                if (diagram.SankeyFlowDirection == FlowDirection.LeftToRight)
                {
                    // in progress
                }
            }

            if (diagram.SankeyFlowDirection == FlowDirection.TopToBottom)
            {
                length = diagram.DiagramCanvas.Height;
            }
            else
            {
                length = diagram.DiagramCanvas.Width;
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
                }
            }

            var nodesByLevel = tempNodes.OrderBy(n => n.Key).ToDictionary(item => (int)item.Key, item => item.Value);

            return nodesByLevel;
        }

        private SankeyNode CreateNode(SankeyDataRow data, string name)
        {
            var text = new TextBlock()
            {
                Text = name,
                Style = diagram.LabelStyle
            };

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
            SetNodeBrush(node);
            node.Name = name;

            return node;
        }

        private void SetNodeBrush(SankeyNode node)
        {
            var brushCheck = diagram.NodeBrushes != null && diagram.NodeBrushes.Keys.Contains(node.Label.Text);

            if (brushCheck)
            {
                node.Shape.Fill = diagram.NodeBrushes[node.Label.Text].CloneCurrentValue();
            }
            else
            {
                if (diagram.UsePallette != SankeyPalette.None)
                {
                    node.Shape.Fill = styleManager.DefaultNodeLinksPalette[styleManager.DefaultNodeLinksPaletteIndex].CloneCurrentValue();
                    styleManager.DefaultNodeLinksPaletteIndex++;

                    if (styleManager.DefaultNodeLinksPaletteIndex >= styleManager.DefaultNodeLinksPalette.Count)
                    {
                        styleManager.DefaultNodeLinksPaletteIndex = 0;
                    }
                }
            }

            node.OriginalShapBrush = node.Shape.Fill.CloneCurrentValue();
        }

        private double GetMaxLabelWidth(List<SankeyNode> nodes)
        {
            var max = 0.0;

            foreach(var node in nodes)
            {
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
            if (CurrentLinks != null && CurrentNodes != null)
            {
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

        public List<SankeyLink> CurrentLinks { get; set; }

        private SankeyDiagram diagram;

        private SankeyStyleManager styleManager;

        #endregion
    }
}
