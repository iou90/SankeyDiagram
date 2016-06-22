using System;
using System.Collections.Generic;
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
            if (!(diagram.NodesPanel == null || diagram.NodesPanel.Children == null || diagram.NodesPanel.Children.Count == 0))
            {
                RemoveElementEventHandlers();
                diagram.LinksContainer.Children.Clear();
                diagram.NodesPanel.Children.Clear();
                CurrentNodes.Clear();
                CurrentLabels.Clear();
                CurrentLinks.Clear();
                styleManager.ClearHighlight();
            }

            if (datas == null || datas.Count() == 0)
            {
                return;
            }

            // create nodes, dictionary key means col/row index
            var nodes = ComputeNodeDepth(datas, new Dictionary<int, List<SankeyNode>>(), 0);

            // calculate node length & set node shape brush
            CurrentNodes = ShapeNodes(datas, nodes);

            CurrentLabels = new List<TextBlock>();

            // create links, has to be putted after ShapingNodes method
            CurrentLinks = ProduceLinks(datas, nodes);

            // drawing...
            if (diagram.IsDiagramCreated)
            {
                CreateDiagram();
            }
        }

        public void CreateDiagram()
        {
            if (diagram.NodesPanel.ActualHeight <= 0 || diagram.NodesPanel.ActualWidth <= 0 || CurrentNodes == null || CurrentNodes.Count < 2 || CurrentLinks == null || CurrentLinks.Count == 0)
            {
                return;
            }

            #region preparing...

            var panelLength = 0.0;
            var linkLength = 0.0;

            if (diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
            {
                diagram.NodesPanel.Orientation = Orientation.Vertical;
                panelLength = diagram.NodesPanel.ActualWidth;
                linkLength = diagram.LinkAeraLength > 0 ? diagram.LinkAeraLength : (diagram.NodesPanel.ActualHeight - CurrentNodes.Count * diagram.NodeThickness) / (CurrentNodes.Count - 1);
            }
            else
            {
                diagram.NodesPanel.Orientation = Orientation.Horizontal;
                panelLength = diagram.NodesPanel.ActualHeight;
                linkLength = diagram.LinkAeraLength > 0 ? diagram.LinkAeraLength : (diagram.NodesPanel.ActualWidth - CurrentNodes.Count * diagram.NodeThickness) / (CurrentNodes.Count - 1);
            }

            var nodesGroupContainerStyle = new Style();
            var margin = diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom ? new Thickness(0, 0, diagram.NodeIntervalSpace, 0) : new Thickness(0, diagram.NodeIntervalSpace, 0, 0);
            nodesGroupContainerStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, margin));
            var maxGroupLength = 0.0;
            var maxGroupCount = 0;

            foreach (var levelNodes in CurrentNodes.Values)
            {
                var tempGroupLength = 0.0;

                foreach (var node in levelNodes)
                {
                    if (diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                    {
                        tempGroupLength += node.Shape.Width;
                    }
                    else
                    {
                        tempGroupLength += node.Shape.Height;
                    }
                }

                if (tempGroupLength > maxGroupLength)
                {
                    maxGroupLength = tempGroupLength;
                    maxGroupCount = levelNodes.Count;
                }
            }

            // - 15 means you have to remain some margin to calculate node's position, or a wrong position of the top node
            var nodesOverallLength = panelLength - (maxGroupCount * diagram.NodeIntervalSpace) - 15;

            if (nodesOverallLength <= 0)
            {
                diagram.NodesPanel.Children.Add(new TextBlock() { Text = "diagram panel length is not enough" });

                return;
            }

            var unitLength = nodesOverallLength / maxGroupLength;
            var labelContainers = new List<Canvas>();

            #endregion

            #region add nodes

            for (var index = 0; index < CurrentNodes.Count; index++)
            {
                var nodesGroup = new ItemsControl();
                var nodesGroupWidth = 0.0;
                var diagramVerticalMargin = 0.0;
                nodesGroup.ItemContainerStyle = nodesGroupContainerStyle;

                if (diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                {
                    nodesGroup.HorizontalAlignment = HorizontalAlignment.Center;
                    var factory = new FrameworkElementFactory(typeof(StackPanel));
                    factory.Name = "StackPanel";
                    factory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
                    nodesGroup.ItemsPanel = new ItemsPanelTemplate(factory);
                }
                else
                {
                    nodesGroup.VerticalAlignment = VerticalAlignment.Bottom;
                }

                for (var nIndex = 0; nIndex < CurrentNodes[index].Count; nIndex++)
                {
                    if (diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                    {
                        CurrentNodes[index][nIndex].Shape.Width = CurrentNodes[index][nIndex].Shape.Width * unitLength;
                        nodesGroupWidth += CurrentNodes[index][nIndex].Shape.Width;
                    }
                    else
                    {
                        CurrentNodes[index][nIndex].Shape.Height = CurrentNodes[index][nIndex].Shape.Height * unitLength;
                    }

                    nodesGroup.Items.Add(CurrentNodes[index][nIndex].Shape);
                }

                diagram.NodesPanel.Children.Add(nodesGroup);

                if (index != CurrentNodes.Count - 1)
                {
                    var canvas = new Canvas();

                    if (diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                    {
                        canvas.Height = linkLength;
                    }
                    else
                    {
                        canvas.Width = linkLength;
                    }

                    labelContainers.Add(canvas);
                    diagram.NodesPanel.Children.Add(canvas);
                }
            }

            #endregion

            #region add links & labels

            // prepare for translatepoint method
            diagram.UpdateLayout();

            var setNodePostion = new Action<SankeyNode>(node =>
            {
                if (node.Position != null)
                {
                    node.Position = node.Shape.TranslatePoint(new Point(), diagram.DiagramGrid);
                }
            });

            foreach (var link in CurrentLinks)
            {
                setNodePostion(link.FromNode);
                setNodePostion(link.ToNode);
                diagram.LinksContainer.Children.Add(DrawLink(link, unitLength).Shape);
            }

            var needAddLabels = CurrentLabels.Count == 0 ? true : false;

            if (needAddLabels)
            {
                styleManager.OriginalLabelOpacity = this.CurrentNodes[0][0].Label.Opacity;

                for (var index = 0; index < labelContainers.Count; index++)
                {
                    // add last & last but one group node labels in last container
                    if (index == labelContainers.Count - 1)
                    {
                        AddLabels(labelContainers[index], CurrentNodes, index);
                        AddLabels(labelContainers[index], CurrentNodes, index + 1);
                    }
                    // add from nodes labels
                    else
                    {
                        AddLabels(labelContainers[index], CurrentNodes, index);
                    }
                }
            }

            styleManager.ChangeLabelsVisibility(diagram.ShowLabels, CurrentLabels);

            #endregion
        }

        private Dictionary<int, List<SankeyNode>> ComputeNodeDepth(IEnumerable<SankeyDataRow> datas, Dictionary<int, List<SankeyNode>> nodes, int levelIndex)
        {
            var isDatasUpdated = false;
            var tempDatas = datas.ToList();

            foreach (var data in datas)
            {
                // if a node name only equals From property, it'll be in the first col/row
                if (levelIndex == 0 && tempDatas.Exists(d => d.To == data.From))
                {
                    continue;
                }

                if (levelIndex > 0)
                {
                    var node = nodes[levelIndex - 1].Find(findNode => findNode.Label.Text == data.From);

                    if (node != null)
                    {
                        var previousLevelNodes = tempDatas.FindAll(d => d.From == node.Label.Text);

                        if (previousLevelNodes.Count == 0)
                        {
                            break;
                        }

                        foreach (var pNode in previousLevelNodes)
                        {
                            //var checkNodes = from d in datas where d.To == pNode.To & !nodes.Values.SelectMany(n => n).ToList().Exists(existNode => existNode.Label.Text == d.From) select d;

                            if (pNode.To == data.To)
                            {
                                var fromNodesIsSubsetOfPreviousLevelsNodes = !datas.Where(d => d.To == pNode.To).Select(d => d.From).Distinct().Except(nodes.Where(n => n.Key < levelIndex).ToDictionary(x => x.Key, x => x.Value).Values.SelectMany(n => n).Select(n => n.Label.Text)).Any();

                                if (fromNodesIsSubsetOfPreviousLevelsNodes)
                                {
                                    nodes = FeedNodes(nodes, levelIndex, data, data.To);
                                    isDatasUpdated = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        continue;
                    }

                    var isDataDuplicate = false;

                    for (var i = 1; i < levelIndex + 1; i++)
                    {
                        if (nodes[levelIndex - i].Exists(findNode => findNode.Label.Text == data.From))
                        {
                            isDataDuplicate = true;

                            break;
                        }
                    }

                    if (isDataDuplicate)
                    {
                        continue;
                    }
                }

                isDatasUpdated = true;

                // if a node name only equals To property, it'll be in the last col/row
                var label = !tempDatas.Exists(d => d.From == data.From) ? data.To : data.From;

                nodes = FeedNodes(nodes, levelIndex, data, label);
            }

            if (isDatasUpdated)
            {
                levelIndex++;

                return ComputeNodeDepth(datas, nodes, levelIndex);
            }
            else
            {
                return nodes;
            }
        }

        private Dictionary<int, List<SankeyNode>> FeedNodes(Dictionary<int, List<SankeyNode>> nodes, int index, SankeyDataRow data, string label)
        {
            if (nodes.Keys.Contains(index))
            {
                if (nodes[index].Count(findNode => label == findNode.Label.Text) == 0)
                {
                    nodes[index].Add(CreateNode(data, label));
                }
            }
            else
            {
                nodes.Add(index, new List<SankeyNode>() { CreateNode(data, label) });
            }

            return nodes;
        }

        private SankeyNode CreateNode(SankeyDataRow data, string label)
        {
            var l = new TextBlock()
            {
                Text = label,
                Style = diagram.LabelStyle
            };

            var shape = new Rectangle();
            shape.Tag = label;

            // for highlighting or other actions
            shape.MouseEnter += NodeMouseEnter;
            shape.MouseLeave += NodeMouseLeave;
            shape.MouseLeftButtonUp += NodeMouseLeftButtonUp;

            if (diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
            {
                shape.Height = diagram.NodeThickness;
            }
            else
            {
                shape.Width = diagram.NodeThickness;
            }

            return new SankeyNode(shape, l);
        }

        private Dictionary<int, List<SankeyNode>> ShapeNodes(IEnumerable<SankeyDataRow> datas, Dictionary<int, List<SankeyNode>> nodes)
        {
            var nodeFromLengthDictionary = new Dictionary<string, double>();
            var nodeToLengthDictionary = new Dictionary<string, double>();

            var feedLengthDictionary = new Action<Dictionary<string, double>, string, double>((lengthDictionary, key, length) =>
            {
                if(lengthDictionary.Keys.Contains(key))
                {
                    lengthDictionary[key] += length;
                }
                else
                {
                    lengthDictionary.Add(key, length);
                }
            });

            foreach (var data in datas)
            {
                feedLengthDictionary(nodeFromLengthDictionary, data.From, data.Weight);
                feedLengthDictionary(nodeToLengthDictionary, data.To, data.Weight);
            }

            var setLengh = new Action<SankeyNode, double>((node, length) =>
            {
                if (diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                {
                    node.Shape.Width = length;
                }
                else
                {
                    node.Shape.Height = length;
                }
            });

            var setBrush = new Action<SankeyNode>(node =>
            {
                var brushCheck = diagram.NodeBrushes != null && diagram.NodeBrushes.Keys.Contains(node.Label.Text);

                if (brushCheck)
                {
                    node.Shape.Fill = diagram.NodeBrushes[node.Label.Text].CloneCurrentValue();
                }
                else
                {
                    if (diagram.UseNodeLinksPalette)
                    {
                        node.Shape.Fill = styleManager.DefaultNodeLinksPalette[styleManager.DefaultNodeLinksPaletteIndex].CloneCurrentValue();
                        styleManager.DefaultNodeLinksPaletteIndex++;

                        if (styleManager.DefaultNodeLinksPaletteIndex >= styleManager.DefaultNodeLinksPalette.Count)
                        {
                            styleManager.DefaultNodeLinksPaletteIndex = 0;
                        }
                    }
                    else
                    {
                        node.Shape.Fill = diagram.NodeBrush.CloneCurrentValue();
                    }
                }

                node.OriginalShapBrush = node.Shape.Fill.CloneCurrentValue();
            });

            styleManager.DefaultNodeLinksPaletteIndex = 0;

            for (var index = 0; index < nodes.Count; index++)
            {
                if (index == nodes.Count - 1)
                {
                    foreach (var node in nodes[index])
                    {
                        setLengh(node, nodeToLengthDictionary[node.Label.Text]);
                        setBrush(node);
                    }

                    continue;
                }

                if (index == 0)
                {
                    foreach (var node in nodes[index])
                    {
                        setLengh(node, nodeFromLengthDictionary[node.Label.Text]);
                        setBrush(node);
                    }

                    continue;
                }

                foreach (var node in nodes[index])
                {
                    var fromLength = nodeFromLengthDictionary[node.Label.Text];
                    var toLength = nodeToLengthDictionary[node.Label.Text];
                    setLengh(node, fromLength > toLength ? fromLength : toLength);
                    setBrush(node);
                }
            }

            return nodes;
        }

        private List<SankeyLink> ProduceLinks(IEnumerable<SankeyDataRow> datas, Dictionary<int, List<SankeyNode>> nodes)
        {
            var links = new List<SankeyLink>();
            var tempDatas = datas.ToList();

            foreach (var data in tempDatas)
            {
                for (var fCount = 0; fCount < nodes.Count; fCount++)
                {
                    var fromNode = nodes[fCount].Find(findNode => findNode.Label.Text == data.From);

                    // if from node not exists, continue
                    if (fromNode != null)
                    {
                        var toNode = (from node in nodes.Values.SelectMany(n => n) where node.Label.Text == data.To select node).FirstOrDefault();

                        // if to node not exists, continue
                        if (toNode == null)
                        {
                            continue;
                        }

                        // merge links which has the same from & to
                        if (links != null)
                        {
                            var previousLink = (from findLink in links where findLink.FromNode.Label.Text == fromNode.Label.Text && findLink.ToNode.Label.Text == toNode.Label.Text select findLink).FirstOrDefault();

                            if (previousLink != null)
                            {
                                previousLink.Shape.StrokeThickness += data.Weight;

                                continue;
                            }
                        }

                        var shape = new Path();
                        shape.MouseEnter += LinkMouseEnter;
                        shape.MouseLeave += LinkMouseLeave;
                        shape.MouseLeftButtonUp += LinkMouseLeftButtonUp;
                        shape.Tag = new SankeyLinkFinder(data.From, data.To);
                        shape.Stroke = diagram.UseNodeLinksPalette ? fromNode.Shape.Fill.CloneCurrentValue() : data.LinkBrush == null ? styleManager.DefaultLinkBrush.CloneCurrentValue() : data.LinkBrush.CloneCurrentValue();
                        shape.StrokeThickness = data.Weight;
                        var link = new SankeyLink(fromNode, toNode, shape, shape.Stroke.CloneCurrentValue());
                        links.Add(link);
                    }
                }
            }

            return links;
        }

        private void LinkMouseEnter(object sender, MouseEventArgs e)
        {
            if (diagram.HighlightMode == SankeyHighlightMode.MouseEnter)
            {
                diagram.SetCurrentValue(SankeyDiagram.HighlightLinkProperty, (SankeyLinkFinder)((Path)e.OriginalSource).Tag);
            }
        }

        private void LinkMouseLeave(object sender, MouseEventArgs e)
        {
            if (diagram.HighlightMode == SankeyHighlightMode.MouseEnter)
            {
                diagram.SetCurrentValue(SankeyDiagram.HighlightLinkProperty, (SankeyLinkFinder)((Path)e.OriginalSource).Tag);
            }
        }

        private void LinkMouseLeftButtonUp(object sender, MouseEventArgs e)
        {
            if (diagram.HighlightMode == SankeyHighlightMode.MouseLeftButtonUp)
            {
                diagram.SetCurrentValue(SankeyDiagram.HighlightLinkProperty, (SankeyLinkFinder)((Path)e.OriginalSource).Tag);
            }
        }

        private void NodeMouseEnter(object sender, MouseEventArgs e)
        {
            if (diagram.HighlightMode == SankeyHighlightMode.MouseEnter)
            {
                diagram.SetCurrentValue(SankeyDiagram.HighlightNodeProperty, ((Rectangle)e.OriginalSource).Tag as string);
            }
        }

        private void NodeMouseLeave(object sender, MouseEventArgs e)
        {
            if (diagram.HighlightMode == SankeyHighlightMode.MouseEnter)
            {
                diagram.SetCurrentValue(SankeyDiagram.HighlightNodeProperty, ((Rectangle)e.OriginalSource).Tag as string);
            }
        }

        private void NodeMouseLeftButtonUp(object sender, MouseEventArgs e)
        {
            if (diagram.HighlightMode == SankeyHighlightMode.MouseLeftButtonUp)
            {
                diagram.SetCurrentValue(SankeyDiagram.HighlightNodeProperty, ((Rectangle)e.OriginalSource).Tag as string);
            }
        }

        private SankeyLink DrawLink(SankeyLink link, double unitLength)
        {
            if (diagram.LinkPoint1Curveless <= 0 || diagram.LinkPoint1Curveless > 1)
            {
                throw new ArgumentOutOfRangeException("curveless should be between 0 and 1.");
            }

            if (diagram.LinkPoint2Curveless <= 0 || diagram.LinkPoint2Curveless > 1)
            {
                throw new ArgumentOutOfRangeException("curveless should be between 0 and 1.");
            }

            link.Shape.StrokeThickness = link.Shape.StrokeThickness * unitLength;
            var fromPoint = new Point();
            var toPoint = new Point();
            var bezierControlPoint1 = new Point();
            var bezierCOntrolPoint2 = new Point();

            if (diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
            {
                fromPoint.X = link.FromNode.Position.X + link.FromNode.NextOccupiedLength + link.Shape.StrokeThickness / 2;
                fromPoint.Y = link.FromNode.Position.Y + link.FromNode.Shape.Height;
                toPoint.X = link.ToNode.Position.X + link.ToNode.PreviousOccupiedLength + link.Shape.StrokeThickness / 2;
                toPoint.Y = link.ToNode.Position.Y;
                var length = toPoint.Y - fromPoint.Y;
                bezierControlPoint1.X = fromPoint.X;
                bezierControlPoint1.Y = length * diagram.LinkPoint1Curveless + fromPoint.Y;
                bezierCOntrolPoint2.X = toPoint.X;
                bezierCOntrolPoint2.Y = length * diagram.LinkPoint2Curveless + fromPoint.Y;
            }
            else
            {
                fromPoint.Y = link.FromNode.Position.Y + link.FromNode.NextOccupiedLength + link.Shape.StrokeThickness / 2;
                fromPoint.X = link.FromNode.Position.X + link.FromNode.Shape.Width;
                toPoint.Y = link.ToNode.Position.Y + link.ToNode.PreviousOccupiedLength + link.Shape.StrokeThickness / 2;
                toPoint.X = link.ToNode.Position.X;
                var length = toPoint.X - fromPoint.X;
                bezierControlPoint1.Y = fromPoint.Y;
                bezierControlPoint1.X = length * diagram.LinkPoint1Curveless + fromPoint.X;
                bezierCOntrolPoint2.Y = toPoint.Y;
                bezierCOntrolPoint2.X = length * diagram.LinkPoint2Curveless + fromPoint.X;
            }

            var geometry = new PathGeometry()
            {
                Figures = new PathFigureCollection()
                {
                    new PathFigure()
                    {
                        StartPoint = fromPoint,

                        Segments = new PathSegmentCollection()
                        {
                            new BezierSegment()
                            {
                                Point1 = bezierControlPoint1,
                                Point2 = bezierCOntrolPoint2,
                                Point3 = toPoint
                            }
                        }
                    }
                }
            };

            geometry.Freeze();
            link.Shape.Data = geometry;
            link.FromNode.NextOccupiedLength += link.Shape.StrokeThickness;
            link.ToNode.PreviousOccupiedLength += link.Shape.StrokeThickness;

            return link;
        }

        private void AddLabels(Canvas container, Dictionary<int, List<SankeyNode>> nodes, int index)
        {
            foreach (var node in nodes[index])
            {
                node.Label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                if (diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                {
                    Canvas.SetLeft(node.Label, node.Position.X + (node.Shape.Width / 2) - (node.Label.DesiredSize.Width / 2));

                    if (index == nodes.Count - 1)
                    {
                        Canvas.SetBottom(node.Label, 0);
                    }
                }
                else
                {
                    Canvas.SetTop(node.Label, node.Position.Y + (node.Shape.Height / 2) - (node.Label.DesiredSize.Height / 2));

                    if (index == nodes.Count - 1)
                    {
                        Canvas.SetRight(node.Label, 0);
                    }
                }

                CurrentLabels.Add(node.Label);
                container.Children.Add(node.Label);
            }
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

        public Dictionary<int, List<SankeyNode>> CurrentNodes { get; private set; }

        public List<SankeyLink> CurrentLinks { get; set; }

        private SankeyDiagram diagram;

        private SankeyStyleManager styleManager;

        #endregion
    }
}
