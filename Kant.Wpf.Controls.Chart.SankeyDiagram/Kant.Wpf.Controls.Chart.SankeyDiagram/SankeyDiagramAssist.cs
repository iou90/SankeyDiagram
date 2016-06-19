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

        public void UpdateDiagram(IEnumerable<SankeyDataRow> newDatas, IEnumerable<SankeyDataRow> oldDatas)
        {
            if (newDatas == oldDatas)
            {
                return;
            }

            // clean panel
            if (!(diagram.DiagramPanel == null || diagram.DiagramPanel.Children == null || diagram.DiagramPanel.Children.Count == 0))
            {
                RemoveElementEventHandlers();
                diagram.DiagramPanel.Children.Clear();
                currentNodes.Clear();
                currentLinks.Clear();
                ResetHighlight();
            }

            if (newDatas == null || newDatas.Count() == 0)
            {
                return;
            }

            // create nodes, dictionary key means col/row index
            var nodes = ProduceNodes(newDatas, new Dictionary<int, List<SankeyNode>>(), 0);

            // calculate node length
            currentNodes = CalculateNodesLength(newDatas, nodes);

            // create links
            currentLinks = ProduceLinks(newDatas, nodes);

            // drawing...
            if (diagram.IsDiagramCreated)
            {
                CreateDiagram();
            }
        }

        public void UpdateNodeBrushes(Dictionary<string, Brush> newBrushes, Dictionary<string, Brush> oldBrushes)
        {
            if (newBrushes == null || newBrushes == oldBrushes || currentNodes == null || currentNodes.Count() == 0)
            {
                return;
            }

            ResetHighlight();

            foreach (var levelNodes in currentNodes.Values)
            {
                foreach (var node in levelNodes)
                {
                    if (newBrushes.Keys.Contains(node.Label.Text))
                    {
                        var brush = newBrushes[node.Label.Text];

                        if (brush != node.Shape.Fill)
                        {
                            node.Shape.Fill = brush.CloneCurrentValue();
                            styleManager.ResettedHighlightNodeBrushes.Add(node.Label.Text, brush.CloneCurrentValue());
                        }
                    }
                }
            }
        }

        public void HighlightingNode(string highlightNode)
        {
            if ((string.IsNullOrEmpty(highlightNode) && string.IsNullOrEmpty(diagram.HighlightNode) || currentNodes == null || currentNodes.Count < 2 || currentLinks == null || currentLinks.Count == 0))
            {
                return;
            }

            // reset each element's style first
            styleManager.ResetHighlights(currentLinks, false);

            var resetBrushes = true;
            var highlightNodes = new List<string>();
            var minimizeNodes = new List<string>();

            // check whether highlight node exists
            if (!string.IsNullOrEmpty(highlightNode))
            {
                if ((from node in currentNodes.Values.SelectMany(n => n) where node.Label.Text == highlightNode select node).Count() == 1)
                {
                    resetBrushes = false;
                };
            }

            // reset highlight if highlighting the same node twice
            if (highlightNode == diagram.HighlightNode && (from node in currentNodes.Values.SelectMany(n => n) where node.Label.Text == diagram.HighlightNode & node.IsHighlight select node).Count() == 1)
            {
                styleManager.ResetHighlights(currentLinks);

                return;
            }

            // for node, link highlighting switching
            diagram.HighlightLink = null;

            // increasing opacity of the correlated element while lower the others  
            styleManager.Highlighting(currentLinks, resetBrushes, diagram.HighlightOpacity, diagram.LoweredOpacity, highlightNodes, minimizeNodes, new Func<SankeyLink, bool>(link => { return link.FromNode.Label.Text == highlightNode || link.ToNode.Label.Text == highlightNode; }));
        }

        public void HighlightingLink(SankeyLinkFinder linkStyleFinder)
        {
            if ((linkStyleFinder == null && diagram.HighlightLink == null || currentNodes == null || currentNodes.Count < 2 || currentLinks == null || currentLinks.Count == 0))
            {
                return;
            }

            // reset each element's style first
            styleManager.ResetHighlights(currentLinks, false);

            var resetBrushes = true;
            var highlightNodes = new List<string>();
            var minimizeNodes = new List<string>();

            // check whether highlight node exist
            if (linkStyleFinder != null)
            {
                var fromNode = (from node in currentNodes.Values.SelectMany(n => n) where node.Label.Text == linkStyleFinder.From select node).FirstOrDefault();
                var toNode = (from node in currentNodes.Values.SelectMany(n => n) where node.Label.Text == linkStyleFinder.To select node).FirstOrDefault();

                if (fromNode != null && toNode != null)
                {
                    if ((from link in currentLinks.Values.SelectMany(l => l) where link.FromNode.Label.Text == linkStyleFinder.From & link.ToNode.Label.Text == linkStyleFinder.To select link).Count() > 0)
                    {
                        resetBrushes = false;
                    }
                };
            }

            // reset highlight if highlighting the same link twice
            if (diagram.HighlightLink != null && linkStyleFinder != null)
            {
                if ((diagram.HighlightLink.From == linkStyleFinder.From && diagram.HighlightLink.To == linkStyleFinder.To) && (from link in currentLinks.Values.SelectMany(l => l) where (link.FromNode.Label.Text == diagram.HighlightLink.From && link.ToNode.Label.Text == diagram.HighlightLink.To) & link.IsHighlight select link).Count() == 1)
                {
                    styleManager.ResetHighlights(currentLinks);

                    return;
                }
            }

            // for node, link highlighting switching
            diagram.HighlightNode = null;

            // increasing opacity of the correlated element while lower the others  
            styleManager.Highlighting(currentLinks, resetBrushes, diagram.HighlightOpacity, diagram.LoweredOpacity, highlightNodes, minimizeNodes, new Func<SankeyLink, bool>(link => { return link.FromNode.Label.Text == linkStyleFinder.From && link.ToNode.Label.Text == linkStyleFinder.To; }));
        }

        public void CreateDiagram()
        {
            if (diagram.DiagramPanel.ActualHeight <= 0 || diagram.DiagramPanel.ActualWidth <= 0 || currentNodes == null || currentNodes.Count < 2 || currentLinks == null || currentLinks.Count == 0)
            {
                return;
            }

            #region preparing...

            var panelLength = 0.0;
            var linkLength = 0.0;

            if (diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
            {
                panelLength = diagram.DiagramPanel.ActualWidth;
                linkLength = diagram.LinkAeraLength > 0 ? diagram.LinkAeraLength : (diagram.DiagramPanel.ActualHeight - currentNodes.Count * diagram.NodeThickness) / currentLinks.Count;
            }
            else
            {
                diagram.DiagramPanel.Orientation = Orientation.Horizontal;
                panelLength = diagram.DiagramPanel.ActualHeight;
                linkLength = diagram.LinkAeraLength > 0 ? diagram.LinkAeraLength : (diagram.DiagramPanel.ActualWidth - currentNodes.Count * diagram.NodeThickness) / currentLinks.Count;
            }

            var nodesGroupContainerStyle = new Style();
            var margin = diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom ? new Thickness(0, 0, diagram.NodeIntervalSpace, 0) : new Thickness(0, diagram.NodeIntervalSpace, 0, 0);
            nodesGroupContainerStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, margin));
            var maxGroupLength = 0.0;
            var maxGroupCount = 0;

            foreach (var levelNodes in currentNodes.Values)
            {
                var tempGroupLength = 0.0;

                foreach (var node in levelNodes)
                {
                    // if using node-links palette and no node brushes then using default palette
                    if (diagram.UseNodeLinksPalette)
                    {
                        if (diagram.NodeBrushes == null || !diagram.NodeBrushes.Keys.Contains(node.Label.Text))
                        {
                            node.Shape.Fill = styleManager.DefaultNodeLinksPalette[styleManager.DefaultNodeLinksPaletteIndex].CloneCurrentValue();
                            styleManager.DefaultNodeLinksPaletteIndex++;

                            if (styleManager.DefaultNodeLinksPaletteIndex >= styleManager.DefaultNodeLinksPalette.Count)
                            {
                                styleManager.DefaultNodeLinksPaletteIndex = 0;
                            }
                        }
                    }

                    // save node fill
                    styleManager.ResettedHighlightNodeBrushes.Add(node.Label.Text, node.Shape.Fill.CloneCurrentValue());

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
                diagram.DiagramPanel.Children.Add(new TextBlock() { Text = "diagram panel length is not enough" });

                return;
            }

            var unitLength = nodesOverallLength / maxGroupLength;
            var linkContainers = new List<Canvas>();

            #endregion

            #region add nodes

            for (var index = 0; index < currentNodes.Count; index++)
            {
                var nodesGroup = new ItemsControl();
                var nodesGroupWidth = 0.0;
                var diagramVerticalMargin = 0.0;
                nodesGroup.ItemContainerStyle = nodesGroupContainerStyle;

                if (diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                {
                    nodesGroup.HorizontalAlignment = HorizontalAlignment.Left;
                    var factory = new FrameworkElementFactory(typeof(StackPanel));
                    factory.Name = "StackPanel";
                    factory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
                    nodesGroup.ItemsPanel = new ItemsPanelTemplate(factory);
                }
                else
                {
                    nodesGroup.VerticalAlignment = VerticalAlignment.Bottom;
                }

                for (var nIndex = 0; nIndex < currentNodes[index].Count; nIndex++)
                {
                    if (diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                    {
                        currentNodes[index][nIndex].Shape.Width = currentNodes[index][nIndex].Shape.Width * unitLength;
                        nodesGroupWidth += currentNodes[index][nIndex].Shape.Width;
                    }
                    else
                    {
                        currentNodes[index][nIndex].Shape.Height = currentNodes[index][nIndex].Shape.Height * unitLength;
                    }

                    nodesGroup.Items.Add(currentNodes[index][nIndex].Shape);
                }

                // make HorizontalAlignment center manully because HorizontalAlignment.Center can not keep diagram's position  while panel size changing
                if (diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                {
                    diagramVerticalMargin = (panelLength - nodesGroupWidth - ((currentNodes[index].Count - 1) * diagram.NodeIntervalSpace)) / 2;
                    nodesGroup.Margin = new Thickness(diagramVerticalMargin, 0, 0, 0);
                }

                diagram.DiagramPanel.Children.Add(nodesGroup);

                if (index != currentNodes.Count - 1)
                {
                    var canvas = new Canvas();
                    canvas.ClipToBounds = true;

                    if (diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                    {
                        canvas.Height = linkLength;
                    }
                    else
                    {
                        canvas.Width = linkLength;
                    }

                    linkContainers.Add(canvas);
                    diagram.DiagramPanel.Children.Add(canvas);
                }
            }

            #endregion

            #region add links & labels

            // prepare for translatepoint method
            diagram.UpdateLayout();

            var setNodePostion = new Action<SankeyNode>(node =>
            {
                if (node.Position == null)
                {
                    if (diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                    {
                        node.Position = node.Shape.TranslatePoint(new Point(), diagram.DiagramPanel).X;
                    }
                    else
                    {
                        node.Position = node.Shape.TranslatePoint(new Point(), diagram.DiagramPanel).Y;
                    }
                }
            });

            for (var index = 0; index < linkContainers.Count; index++)
            {
                for (var lIndex = 0; lIndex < currentLinks[index].Count; lIndex++)
                {
                    var link = currentLinks[index][lIndex];
                    setNodePostion(link.FromNode);
                    setNodePostion(link.ToNode);
                    linkContainers[index].Children.Add(DrawLink(link, linkLength, unitLength).Shape);
                    styleManager.ResettedHighlightLinkBrushes.Add(new SankeyLinkStyleFinder(link.FromNode.Label.Text, link.ToNode.Label.Text) { Brush = link.Shape.Stroke.CloneCurrentValue() });
                }

                if (diagram.ShowLabels)
                {
                    styleManager.ResettedLabelOpacity = this.currentNodes[0][0].Label.Opacity;

                    // add last & last but one group node labels in last container
                    if (index == linkContainers.Count - 1)
                    {
                        AddLabels(linkContainers[index], currentNodes, index);
                        AddLabels(linkContainers[index], currentNodes, index + 1);
                    }
                    // add from nodes labels
                    else
                    {
                        AddLabels(linkContainers[index], currentNodes, index);
                    }
                }
            }

            #endregion
        }

        private Dictionary<int, List<SankeyNode>> ProduceNodes(IEnumerable<SankeyDataRow> datas, Dictionary<int, List<SankeyNode>> nodes, int levelIndex)
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
                            if (pNode.To == data.To)
                            {
                                isDatasUpdated = true;
                                nodes = FeedNodes(nodes, levelIndex, data, data.To);
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

                return ProduceNodes(datas, nodes, levelIndex);
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
                var n = nodes[index].Find(findNode => label == findNode.Label.Text);

                if (n == null)
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

            if (diagram.HighlightMode == SankeyHighlightMode.MouseEnter)
            {
                shape.MouseEnter += NodeMouseEnter;
                shape.MouseLeave += NodeMouseLeave;
            }

            // hightlight or prepare for custom action
            shape.MouseLeftButtonUp += NodeMouseLeftButtonUp;

            shape.Tag = label;

            if (diagram.NodeBrushes != null && diagram.NodeBrushes.Keys.Contains(label))
            {
                shape.Fill = diagram.NodeBrushes[label].CloneCurrentValue();
            }
            else
            {
                shape.Fill = diagram.NodeBrush.CloneCurrentValue();
            }

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

        private Dictionary<int, List<SankeyNode>> CalculateNodesLength(IEnumerable<SankeyDataRow> datas, Dictionary<int, List<SankeyNode>> nodes)
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

            for (var index = 0; index < nodes.Count; index++)
            {
                if (index == nodes.Count - 1)
                {
                    foreach (var node in nodes[index])
                    {
                        setLengh(node, nodeToLengthDictionary[node.Label.Text]);
                    }

                    continue;
                }

                if (index == 0)
                {
                    foreach (var node in nodes[index])
                    {
                        setLengh(node, nodeFromLengthDictionary[node.Label.Text]);
                    }

                    continue;
                }

                foreach (var node in nodes[index])
                {
                    var fromLength = nodeFromLengthDictionary[node.Label.Text];
                    var toLength = nodeToLengthDictionary[node.Label.Text];
                    setLengh(node, fromLength > toLength ? fromLength : toLength);
                }
            }

            return nodes;
        }

        private Dictionary<int, List<SankeyLink>> ProduceLinks(IEnumerable<SankeyDataRow> datas, Dictionary<int, List<SankeyNode>> nodes)
        {
            var linkDictionary = new Dictionary<int, List<SankeyLink>>();
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
                        if (linkDictionary != null)
                        {
                            var previousLink = (from findLink in linkDictionary.Values.SelectMany(l => l) where findLink.FromNode.Label.Text == fromNode.Label.Text && findLink.ToNode.Label.Text == toNode.Label.Text select findLink).FirstOrDefault();

                            if (previousLink != null)
                            {
                                previousLink.Shape.StrokeThickness += data.Weight;

                                continue;
                            }
                        }

                        var link = new SankeyLink()
                        {
                            FromNode = fromNode,
                            ToNode = toNode
                        };

                        var shape = new Path();

                        if (diagram.HighlightMode == SankeyHighlightMode.MouseEnter)
                        {
                            shape.MouseEnter += LinkMouseEnter;
                            shape.MouseLeave += LinkMouseLeave;
                        }

                        // hightlight or prepare for custom action
                        shape.MouseLeftButtonUp += LinkMouseLeftButtonUp;

                        shape.Tag = new SankeyLinkFinder(data.From, data.To);
                        shape.Stroke = data.LinkStroke == null ? styleManager.DefaultLinkBrush.CloneCurrentValue() : data.LinkStroke.CloneCurrentValue();
                        shape.StrokeThickness = data.Weight;
                        link.Shape = shape;

                        if (linkDictionary.Keys.Contains(fCount))
                        {
                            linkDictionary[fCount].Add(link);
                        }
                        else
                        {
                            linkDictionary.Add(fCount, new List<SankeyLink>() { link });
                        }
                    }
                }
            }

            return linkDictionary;
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

        private void AddLabels(Canvas container, Dictionary<int, List<SankeyNode>> nodes, int index)
        {
            foreach (var node in nodes[index])
            {
                node.Label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                if (diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                {
                    Canvas.SetLeft(node.Label, node.Position.Value + (node.Shape.Width / 2) - (node.Label.DesiredSize.Width / 2));

                    if (index == nodes.Count - 1)
                    {
                        Canvas.SetBottom(node.Label, 0);
                    }
                }
                else
                {
                    Canvas.SetTop(node.Label, node.Position.Value + (node.Shape.Height / 2) - (node.Label.DesiredSize.Height / 2));

                    if (index == nodes.Count - 1)
                    {
                        Canvas.SetRight(node.Label, 0);
                    }
                }

                container.Children.Add(node.Label);
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

        private SankeyLink DrawLink(SankeyLink link, double linkLength, double unitLength)
        {
            if (diagram.LinkPoint1Curveless <= 0 || diagram.LinkPoint1Curveless > 1)
            {
                throw new ArgumentOutOfRangeException("curveless should be between 0 and 1.");
            }

            if (diagram.LinkPoint2Curveless <= 0 || diagram.LinkPoint2Curveless > 1)
            {
                throw new ArgumentOutOfRangeException("curveless should be between 0 and 1.");
            }

            if (diagram.UseNodeLinksPalette)
            {
                link.Shape.Stroke = link.FromNode.Shape.Fill.CloneCurrentValue();
            }

            link.Shape.StrokeThickness = link.Shape.StrokeThickness * unitLength;
            var fromPoint = new Point();
            var toPoint = new Point();
            var bezierControlPoint1 = new Point();
            var bezierCOntrolPoint2 = new Point();

            if (diagram.SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
            {
                fromPoint.X = link.FromNode.Position.Value + link.FromNode.NextOccupiedLength + link.Shape.StrokeThickness / 2;
                toPoint.X = link.ToNode.Position.Value + link.ToNode.PreviousOccupiedLength + link.Shape.StrokeThickness / 2;
                toPoint.Y = linkLength;
                bezierControlPoint1.X = fromPoint.X;
                bezierControlPoint1.Y = linkLength * diagram.LinkPoint1Curveless;
                bezierCOntrolPoint2.X = toPoint.X;
                bezierCOntrolPoint2.Y = linkLength * diagram.LinkPoint2Curveless;
            }
            else
            {
                fromPoint.Y = link.FromNode.Position.Value + link.FromNode.NextOccupiedLength + link.Shape.StrokeThickness / 2;
                toPoint.Y = link.ToNode.Position.Value + link.ToNode.PreviousOccupiedLength + link.Shape.StrokeThickness / 2;
                toPoint.X = linkLength;
                bezierControlPoint1.Y = fromPoint.Y;
                bezierControlPoint1.X = linkLength * diagram.LinkPoint1Curveless;
                bezierCOntrolPoint2.Y = toPoint.Y;
                bezierCOntrolPoint2.X = linkLength * diagram.LinkPoint2Curveless;
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

        private void RemoveElementEventHandlers()
        {
            if (currentLinks != null && currentNodes != null)
            {
                (currentNodes.Values.SelectMany(n => n)).Select(findNode =>
                {
                    switch (diagram.HighlightMode)
                    {
                        case SankeyHighlightMode.MouseEnter:
                            findNode.Shape.MouseEnter -= NodeMouseEnter;
                            findNode.Shape.MouseLeave -= NodeMouseLeave;

                            break;
                        default:
                            break;
                    }

                    findNode.Shape.MouseLeftButtonUp -= NodeMouseLeftButtonUp;

                    return findNode;
                });

                (currentLinks.Values.SelectMany(l => l)).Select(findLink =>
                {
                    switch (diagram.HighlightMode)
                    {
                        case SankeyHighlightMode.MouseEnter:
                            findLink.Shape.MouseEnter -= LinkMouseEnter;
                            findLink.Shape.MouseLeave -= LinkMouseLeave;

                            break;
                        default:
                            break;
                    }

                    findLink.Shape.MouseLeftButtonUp -= LinkMouseLeftButtonUp;

                    return findLink;
                });
            }
        }

        private void ResetHighlight()
        {
            styleManager.ResettedHighlightNodeBrushes.Clear();
            styleManager.ResettedHighlightLinkBrushes.Clear();
            diagram.SetCurrentValue(SankeyDiagram.HighlightNodeProperty, null);
            diagram.SetCurrentValue(SankeyDiagram.HighlightLinkProperty, null);
        }

        #endregion

        #region Fields & Properties

        private SankeyDiagram diagram;

        private SankeyStyleManager styleManager;

        private Dictionary<int, List<SankeyNode>> currentNodes;

        private Dictionary<int, List<SankeyLink>> currentLinks;

        #endregion
    }
}
