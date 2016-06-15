using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Kant.Wpf.Controls.Chart
{
    [TemplatePart(Name = "PartDiagramPanel", Type = typeof(StackPanel))]
    public class SankeyDiagram : Control
    {
        #region Constructor

        static SankeyDiagram()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SankeyDiagram), new FrameworkPropertyMetadata(typeof(SankeyDiagram)));
        }

        public SankeyDiagram()
        {
            // set default values
            NodeIntervalSpace = 5;
            NodeThickness = 10;
            NodeBrush = new SolidColorBrush(Colors.Black);
            defaultLinkBrush = new SolidColorBrush(Colors.Gray) { Opacity = 0.55 };
            LinkPoint1Curveless = 0.4;
            LinkPoint2Curveless = 0.6;
            var labelStye = new Style(typeof(TextBlock));
            labelStye.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(2)));
            LabelStyle = labelStye;
            ShowLabels = true;

            defaultNodeLinksPalette = new List<Brush>()
            {
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0095fb")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff0000")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffa200")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00f2c8")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7373ff")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#91bc61")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc89d9")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fff100")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#44c5f1")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#85e91f")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00b192")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1cbe65")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#278bcc")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#954ab3")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f3bc00")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e47403")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ce3e29")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d8dddf")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#60e8a4")) { Opacity = 0.55 },
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffb5ff")) { Opacity = 0.55 }
            };

            defaultNodeLinksPaletteIndex = 0;
            resetHighlightNodeBrushes = new Dictionary<string, Brush>();
            resetHighlightNodeStyles = new Dictionary<string, Style>();
            resetHighlightLinkBrushes = new List<SankeyLinkStyleFinder>();

            Loaded += (s, e) =>
            {
                if (isDiagramLoaded)
                {
                    return;
                }
                
                CreateDiagram(currentNodes, currentLinks);
                isDiagramLoaded = true;
            };
        }

        #endregion

        #region Methods

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var panel = GetTemplateChild("PartDiagramPanel");

            if (panel == null)
            {
                throw new MissingMemberException("can not find template child PartDiagramPanel.");
            }
            else
            {
                DiagramPanel = (StackPanel)panel;
            }
        }

        #region dependency property methods

        private static void OnDatasSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((SankeyDiagram)o).OnDatasChanged(e.NewValue as IEnumerable<SankeyDataRow>, e.OldValue as IEnumerable<SankeyDataRow>);
        }

        private void OnDatasChanged(IEnumerable<SankeyDataRow> newDatas, IEnumerable<SankeyDataRow> oldDatas)
        {
            if(newDatas == oldDatas)
            {
                return;
            }

            // clean panel
            if (!(DiagramPanel == null || DiagramPanel.Children == null || DiagramPanel.Children.Count == 0))
            {
                DiagramPanel.Children.Clear();
                currentNodes.Clear();
                currentLinks.Clear();
                ClearHighlightStyle();
            }

            if (newDatas == null || newDatas.Count() == 0)
            {
                return;
            }
            
            // create nodes, dictionary key means col/row index
            var nodeDictionary = ProduceNodes(newDatas, new Dictionary<int, List<SankeyNode>>(), 0);

            // calculate node length
            currentNodes = CalculateNodesLength(newDatas, nodeDictionary);

            // create links
            currentLinks = ProduceLinks(newDatas, nodeDictionary);

            // drawing...
            if (isDiagramLoaded)
            {
                CreateDiagram(currentNodes, currentLinks);
            }
        }

        private static void OnNodeBrushesSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((SankeyDiagram)o).OnNodeBrushesChanged(e.NewValue as Dictionary<string, Brush>, e.OldValue as Dictionary<string, Brush>);
        }

        private void OnNodeBrushesChanged(Dictionary<string, Brush> newBrushes, Dictionary<string, Brush> oldBrushes)
        {
            if(newBrushes == null || newBrushes == oldBrushes || currentNodes == null || currentNodes.Count() == 0)
            {
                return;
            }

            ClearHighlightStyle();

            foreach (var levelNodes in currentNodes.Values)
            {
                foreach (var node in levelNodes)
                {
                    if(newBrushes.Keys.Contains(node.Label.Text))
                    {
                        var brush = newBrushes[node.Label.Text];

                        if (brush != node.Shape.Fill)
                        {
                            node.Shape.Fill = brush.CloneCurrentValue();
                            resetHighlightNodeBrushes.Add(node.Label.Text, brush.CloneCurrentValue());
                        }                               
                    }
                } 
            }
        }

        private static object HighlightNodeValueCallback(DependencyObject o, object value)
        {
            ((SankeyDiagram)o).HighlightingNode(value as string);

            return value;
        }

        private void HighlightingNode(string highlightNode)
        {
            if ((string.IsNullOrEmpty(highlightNode) && string.IsNullOrEmpty(HighlightNode) || currentNodes == null || currentNodes.Count < 2 || currentLinks == null || currentLinks.Count == 0))
            {
                return;
            }

            // reset each element's style first
            ResetHighlights(false);

            var minOpacity = 0.15;
            var loweredOpacity = 0.25;
            var increasedOpacity = 1;
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

            if((from node in currentNodes.Values.SelectMany(n => n) where node.Label.Text == HighlightNode & node.IsHighlight select node).Count() == 1)
            {
                ResetHighlights();

                return;
            }

            // increasing opacity of the correlated element while lower the others  
            Highlighting(resetBrushes, increasedOpacity, minOpacity, loweredOpacity, highlightNodes, minimizeNodes, new Func<SankeyLink, bool>(link => { return link.FromNode.Label.Text == highlightNode || link.ToNode.Label.Text == highlightNode; }));
        }

        private static object HighlightLinkSourceValueCallback(DependencyObject o, object value)
        {
            ((SankeyDiagram)o).HighlightingLink(value as SankeyLinkFinder);

            return value;
        }

        private void HighlightingLink(SankeyLinkFinder linkStyleFinder)
        {
            if ((linkStyleFinder == null && HighlightLink == null || currentNodes == null || currentNodes.Count < 2 || currentLinks == null || currentLinks.Count == 0))
            {
                return;
            }

            // reset each element's style first
            ResetHighlights(false);

            var minOpacity = 0.15;
            var loweredOpacity = 0.25;
            var increasedOpacity = 1;
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
                    if((from link in currentLinks.Values.SelectMany(l => l) where link.FromNode.Label.Text == linkStyleFinder.From & link.ToNode.Label.Text == linkStyleFinder.To select link).Count() > 0)
                    {
                        resetBrushes = false;
                    }
                };
            }

            if (HighlightLink != null)
            {
                if ((from link in currentLinks.Values.SelectMany(l => l) where (link.FromNode.Label.Text == HighlightLink.From && link.ToNode.Label.Text == HighlightLink.To) & link.IsHighlight select link).Count() == 1)
                {
                    ResetHighlights();

                    return;
                }
            }

            // increasing opacity of the correlated element while lower the others  
            Highlighting(resetBrushes, increasedOpacity, minOpacity, loweredOpacity, highlightNodes, minimizeNodes, new Func<SankeyLink, bool>(link => { return link.FromNode.Label.Text == linkStyleFinder.From && link.ToNode.Label.Text == linkStyleFinder.To; }));
        }

        #endregion

        private void CreateDiagram(Dictionary<int, List<SankeyNode>> nodes, Dictionary<int, List<SankeyLink>> links)
        {
            if(DiagramPanel.ActualHeight <= 0 || DiagramPanel.ActualWidth <= 0 || nodes == null || nodes.Count < 2 || links == null || links.Count == 0)
            {
                return;
            }

            var panelLength = 0.0;
            var linkLength = 0.0;

            if(SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
            {
                panelLength = DiagramPanel.ActualWidth;
                linkLength = LinkLength > 0 ? LinkLength : (DiagramPanel.ActualHeight - nodes.Count * NodeThickness) / links.Count;
            }
            else
            {
                DiagramPanel.Orientation = Orientation.Horizontal;
                panelLength = DiagramPanel.ActualHeight;
                linkLength = LinkLength > 0 ? LinkLength : (DiagramPanel.ActualWidth - nodes.Count * NodeThickness) / links.Count;
            }

            var nodesGroupContainerStyle = new Style();
            var margin = SankeyFlowDirection == SankeyFlowDirection.TopToBottom ? new Thickness(0, 0, NodeIntervalSpace, 0) : new Thickness(0, NodeIntervalSpace, 0, 0);
            nodesGroupContainerStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, margin));
            var maxGroupLength = 0.0;
            var maxGroupCount = 0;

            foreach(var levelNodes in nodes.Values)
            {
                var tempGroupLength = 0.0;

                foreach(var node in levelNodes)
                {
                    // if use node-links color range and node colors property has no value then use default color range
                    if (UseNodeLinksPalette)
                    {
                        if (NodeBrushes == null || !NodeBrushes.Keys.Contains(node.Label.Text))
                        {
                            node.Shape.Fill = defaultNodeLinksPalette[defaultNodeLinksPaletteIndex].CloneCurrentValue();
                            defaultNodeLinksPaletteIndex++;

                            if (defaultNodeLinksPaletteIndex >= defaultNodeLinksPalette.Count)
                            {
                                defaultNodeLinksPaletteIndex = 0;
                            }
                        }
                    }

                    // save node fill
                    resetHighlightNodeBrushes.Add(node.Label.Text, node.Shape.Fill.CloneCurrentValue());

                    if (SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                    {
                        tempGroupLength += node.Shape.Width;
                    }
                    else
                    {
                        tempGroupLength += node.Shape.Height;
                    }
                }

                if(tempGroupLength > maxGroupLength)
                {
                    maxGroupLength = tempGroupLength;
                    maxGroupCount = levelNodes.Count;
                }
            }

            // - 15 means you have to remain some margin to calculate node's position, or a wrong position of the top node
            var nodesOverallLength = panelLength - (maxGroupCount * NodeIntervalSpace) - 15;

            if(nodesOverallLength <= 0)
            {
                DiagramPanel.Children.Add(new TextBlock() { Text = "diagram panel length is not enough" } );

                return;
            }

            var unitLength = nodesOverallLength / maxGroupLength;
            var linkContainers = new List<Canvas>();

            for (var index = 0; index < nodes.Count; index++)
            {
                var nodesGroup = new ItemsControl();
                var nodesGroupWidth = 0.0;
                var diagramVerticalMargin = 0.0;
                nodesGroup.SnapsToDevicePixels = true;
                nodesGroup.ItemContainerStyle = nodesGroupContainerStyle;

                if(SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
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

                for (var nIndex = 0; nIndex < nodes[index].Count; nIndex++)
                {
                    if (SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                    {
                        nodes[index][nIndex].Shape.Width = nodes[index][nIndex].Shape.Width * unitLength;
                        nodesGroupWidth += nodes[index][nIndex].Shape.Width;
                    }
                    else
                    {
                        nodes[index][nIndex].Shape.Height = nodes[index][nIndex].Shape.Height * unitLength;
                    }

                    nodesGroup.Items.Add(nodes[index][nIndex].Shape);
                }

                // make HorizontalAlignment center manully because HorizontalAlignment.Center has deviation when calculating element's position 
                if (SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                {
                    diagramVerticalMargin = (panelLength - nodesGroupWidth - ((nodes[index].Count - 1) * NodeIntervalSpace)) / 2;
                    nodesGroup.Margin = new Thickness(diagramVerticalMargin, 0, 0, 0);
                }

                DiagramPanel.Children.Add(nodesGroup);

                if (index != nodes.Count - 1)
                {
                    var canvas = new Canvas();
                    canvas.SnapsToDevicePixels = true;
                    canvas.ClipToBounds = true;

                    if(SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                    {
                        canvas.Height = linkLength;
                    }
                    else
                    {
                        canvas.Width = linkLength;
                    }

                    linkContainers.Add(canvas);
                    DiagramPanel.Children.Add(canvas);
                }
            }

            // prepare for translatepoint method
            UpdateLayout();

            foreach(var levelNodes in nodes.Values)
            {
                foreach (var node in levelNodes)
                {
                    // add style here cause of the style of node is created completely
                    resetHighlightNodeStyles.Add(node.Label.Text, new Style(typeof(Rectangle), node.Shape.Style));

                    if (SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                    {
                        node.Position = node.Shape.TranslatePoint(new Point(), DiagramPanel).X;
                        node.Position = node.Shape.TranslatePoint(new Point(), DiagramPanel).X;
                    }
                    else
                    {
                        node.Position = node.Shape.TranslatePoint(new Point(), DiagramPanel).Y;
                        node.Position = node.Shape.TranslatePoint(new Point(), DiagramPanel).Y;
                    }
                }
            }

            for (var index = 0; index < linkContainers.Count; index++)
            {
                for (var lIndex = 0; lIndex < links[index].Count; lIndex++)
                {
                    var link = links[index][lIndex];
                    linkContainers[index].Children.Add(DrawLink(link, linkLength, unitLength).Shape);
                    resetHighlightLinkBrushes.Add(new SankeyLinkStyleFinder(link.FromNode.Label.Text, link.ToNode.Label.Text) { Brush = link.Shape.Stroke.CloneCurrentValue() });
                }

                if (ShowLabels)
                {
                    // add last & last but one group node labels in last container
                    if (index == linkContainers.Count - 1)
                    {
                        AddLabels(linkContainers[index], nodes, index);
                        AddLabels(linkContainers[index], nodes, index + 1);
                    }
                    // add from nodes labels
                    else
                    {
                        AddLabels(linkContainers[index], nodes, index);
                    }
                }
            }
        }

        private Dictionary<int, List<SankeyNode>> ProduceNodes(IEnumerable<SankeyDataRow> datas, Dictionary<int, List<SankeyNode>> nodes, int levelIndex)
        {
            var isDatasUpdated = false;
            var tempDatas = datas.ToList();

            foreach(var data in datas)
            {
                // if a node name only exists in From property, it'll be in the first col/row
                if (levelIndex == 0 && tempDatas.Exists(d => d.To == data.From))
                {
                    continue;
                }

                if(levelIndex > 0)
                {
                    var node = nodes[levelIndex - 1].Find(findNode => findNode.Label.Text == data.From);

                    if(node != null)
                    { 
                        var previousLevelNodes = tempDatas.FindAll(d => d.From == node.Label.Text);

                        if (previousLevelNodes.Count == 0)
                        {
                            break;
                        }

                        foreach(var pNode in previousLevelNodes)
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

                    if(isDataDuplicate)
                    {
                        continue;
                    }
                }

                isDatasUpdated = true;

                // if a node name only exists in To property, it'll be in the last col/row
                var label = !tempDatas.Exists(d => d.From == data.From) ? data.To : data.From;

                nodes = FeedNodes(nodes, levelIndex, data, label);
            }

            if(isDatasUpdated)
            {
                levelIndex++;

                return ProduceNodes(datas, nodes, levelIndex);
            }
            else
            {
                return nodes;
            }
        }

        private Dictionary<int, List<SankeyNode>> CalculateNodesLength(IEnumerable<SankeyDataRow> datas, Dictionary<int, List<SankeyNode>> nodes)
        {
            var nodeFromLengthDictionary = new Dictionary<string, double>();
            var nodeToLengthDictionary = new Dictionary<string, double>();

            foreach(var data in datas)
            {
                var length = data.Weight;

                if (nodeFromLengthDictionary.Keys.Contains(data.From))
                {
                    nodeFromLengthDictionary[data.From] += length;
                }
                else
                {
                    nodeFromLengthDictionary.Add(data.From, length);
                }

                if (nodeToLengthDictionary.Keys.Contains(data.To))
                {
                    nodeToLengthDictionary[data.To] += length;
                }
                else
                {
                    nodeToLengthDictionary.Add(data.To, length);
                }
            }

            for (var index = 0; index < nodes.Count; index++)
            {
                if (index == nodes.Count - 1)
                {
                    foreach (var node in nodes[index])
                    {
                        if (SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                        {
                            node.Shape.Width = nodeToLengthDictionary[node.Label.Text];
                        }
                        else
                        {
                            node.Shape.Height = nodeToLengthDictionary[node.Label.Text];
                        }
                    }

                    continue;
                }

                if (index == 0)
                {
                    foreach (var node in nodes[index])
                    {
                        if (SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                        {
                            node.Shape.Width = nodeFromLengthDictionary[node.Label.Text];
                        }
                        else
                        {
                            node.Shape.Height = nodeFromLengthDictionary[node.Label.Text];
                        }
                    }

                    continue;
                }

                foreach (var node in nodes[index])
                {
                    var fromLength = nodeFromLengthDictionary[node.Label.Text];
                    var toLength = nodeToLengthDictionary[node.Label.Text];

                    if (SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                    {
                        node.Shape.Width = fromLength > toLength ? fromLength : toLength;
                    }
                    else
                    {
                        node.Shape.Height = fromLength > toLength ? fromLength : toLength;
                    }
                }
            }

            return nodes;
        }

        private Dictionary<int, List<SankeyLink>> ProduceLinks(IEnumerable<SankeyDataRow> datas, Dictionary<int, List<SankeyNode>> nodes)
        {
            var linkDictionary = new Dictionary<int, List<SankeyLink>>();
            var tempDatas = datas.ToList();

            foreach(var data in tempDatas)
            {
                for (var fCount = 0; fCount < nodes.Count; fCount++)
                {
                    var fromNode = nodes[fCount].Find(findNode => findNode.Label.Text == data.From);

                    if (fromNode != null)
                    {
                        var link = new SankeyLink()
                        {
                            FromNode = fromNode
                        };

                        foreach(var levelNodes in nodes.Values)
                        {
                            var toNode = levelNodes.Find(findNode => findNode.Label.Text == data.To);

                            if (toNode != null)
                            {
                                link.ToNode = toNode;

                                break;
                            }
                        }

                        var shape = new Path();
                        shape.SnapsToDevicePixels = true;
                        shape.Stroke = data.LinkStroke == null ? defaultLinkBrush.CloneCurrentValue() : data.LinkStroke.CloneCurrentValue();
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

        private void AddLabels(Canvas container, Dictionary<int, List<SankeyNode>> nodes, int index)
        {
            foreach (var node in nodes[index])
            {
                node.Label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                if (SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
                {
                    Canvas.SetLeft(node.Label, NodeIntervalSpace + node.Position + (node.Shape.Width / 2) - (node.Label.DesiredSize.Width / 2));

                    if (index == nodes.Count - 1)
                    {
                        Canvas.SetBottom(node.Label, 0);
                    }
                }
                else
                {
                    Canvas.SetTop(node.Label, node.Position + (node.Shape.Height / 2) - (node.Label.DesiredSize.Height / 2));

                    if (index == nodes.Count - 1)
                    {
                        Canvas.SetRight(node.Label, 0);
                    }
                }

                container.Children.Add(node.Label);
            }
        }

        private SankeyNode CreateNode(SankeyDataRow data, string label)
        {
            var l = new TextBlock()
            {
                Text = label,
                Style = LabelStyle
            };

            var shape = new Rectangle();
            shape.SnapsToDevicePixels = true;

            if (NodeBrushes != null && NodeBrushes.Keys.Contains(label))
            {
                shape.Fill = NodeBrushes[label].CloneCurrentValue();
            }
            else
            {
                shape.Fill = NodeBrush.CloneCurrentValue();
            } 

            if(SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
            {
                shape.Height = NodeThickness;
            }
           else
            {
                shape.Width = NodeThickness;
            }

            return new SankeyNode(shape, l);
        }

        private SankeyLink DrawLink(SankeyLink link, double linkLength, double unitLength)
        {
            if(LinkPoint1Curveless <= 0 || LinkPoint1Curveless > 1)
            {
                throw new ArgumentOutOfRangeException("curveless should be between 0 and 1.");
            }

            if (LinkPoint2Curveless <= 0 || LinkPoint2Curveless > 1)
            {
                throw new ArgumentOutOfRangeException("curveless should be between 0 and 1.");
            }

            if(UseNodeLinksPalette)
            {
                link.Shape.Stroke = link.FromNode.Shape.Fill.CloneCurrentValue();
            }

            link.Shape.StrokeThickness = link.Shape.StrokeThickness * unitLength;
            var fromPoint = new Point();
            var toPoint = new Point();
            var bezierControlPoint1 = new Point();
            var bezierCOntrolPoint2 = new Point();

            if(SankeyFlowDirection == SankeyFlowDirection.TopToBottom)
            {
                fromPoint.X = link.FromNode.Position + link.FromNode.NextOccupiedLength + link.Shape.StrokeThickness / 2;
                toPoint.X = link.ToNode.Position + link.ToNode.PreviousOccupiedLength + link.Shape.StrokeThickness / 2;
                toPoint.Y = linkLength;
                bezierControlPoint1.X = fromPoint.X;
                bezierControlPoint1.Y = linkLength * LinkPoint1Curveless;
                bezierCOntrolPoint2.X = toPoint.X;
                bezierCOntrolPoint2.Y = linkLength * LinkPoint2Curveless;
            }
            else
            {
                fromPoint.Y = link.FromNode.Position + link.FromNode.NextOccupiedLength + link.Shape.StrokeThickness / 2;
                toPoint.Y = link.ToNode.Position + link.ToNode.PreviousOccupiedLength + link.Shape.StrokeThickness / 2;
                toPoint.X = linkLength;
                bezierControlPoint1.Y = fromPoint.Y;
                bezierControlPoint1.X = linkLength * LinkPoint1Curveless;
                bezierCOntrolPoint2.Y = toPoint.Y;
                bezierCOntrolPoint2.X = linkLength * LinkPoint2Curveless;
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

        private void ClearHighlightStyle()
        {
            //nodeHighlightCount = 0;
            //linkHightlightCount = 0;
            //isHighlightNodeResetted = false;
            //isHighlightLinkResetted = false;
            resetHighlightNodeBrushes.Clear();
            resetHighlightNodeStyles.Clear();
            resetHighlightLinkBrushes.Clear();
            SetCurrentValue(HighlightNodeProperty, null);
            SetCurrentValue(HighlightLinkProperty, null);
        }

        private double CalculateOpacity(double originOpacity, double minOpacity, double loweredOpacity)
        {
            var opacity = originOpacity;

            if ((originOpacity - loweredOpacity) < minOpacity)
            {
                opacity = minOpacity;
            }
            else
            {
                opacity -= loweredOpacity;
            }

            return opacity;
        }

        private void Highlighting(bool resetBrushes, double increasedOpacity, double minOpacity, double loweredOpacity, List<string> highlightNodes, List<string> minimizeNodes, Func<SankeyLink, bool> check)
        {
            foreach (var levelLinks in currentLinks.Values)
            {
                foreach (var link in levelLinks)
                {
                    if (!resetBrushes)
                    {
                        if (check(link))
                        {
                            link.Shape.Stroke.Opacity = increasedOpacity;
                            link.IsHighlight = true;
                            link.FromNode.Shape.Fill.Opacity = increasedOpacity;
                            link.FromNode.IsHighlight = true;
                            link.ToNode.Shape.Fill.Opacity = increasedOpacity;
                            link.ToNode.IsHighlight = true;
                            highlightNodes.Add(link.FromNode.Label.Text);
                            highlightNodes.Add(link.ToNode.Label.Text);
                        }
                        else
                        {
                            link.Shape.Stroke.Opacity = CalculateOpacity(link.Shape.Stroke.Opacity, minOpacity, loweredOpacity);
                            link.IsHighlight = false;

                            // prevent changing node's brush again
                            if (!highlightNodes.Exists(n => n == link.FromNode.Label.Text) && !minimizeNodes.Exists(n => n == link.FromNode.Label.Text))
                            {
                                link.FromNode.Shape.Fill.Opacity = CalculateOpacity(link.FromNode.Shape.Fill.Opacity, minOpacity, loweredOpacity);
                                link.FromNode.IsHighlight = false;
                                minimizeNodes.Add(link.FromNode.Label.Text);
                            }

                            if (!highlightNodes.Exists(n => n == link.ToNode.Label.Text) && !minimizeNodes.Exists(n => n == link.ToNode.Label.Text))
                            {
                                link.ToNode.Shape.Fill.Opacity = CalculateOpacity(link.ToNode.Shape.Fill.Opacity, minOpacity, loweredOpacity);
                                link.ToNode.IsHighlight = false;
                                minimizeNodes.Add(link.ToNode.Label.Text);
                            }
                        }
                    }
                    else
                    {
                        ResetHighlights(link);
                    }
                }
            }
        }

        private void ResetHighlights(bool resetHighlight = true)
        {
            foreach (var levelLinks in currentLinks.Values)
            {
                foreach (var link in levelLinks)
                {
                    ResetHighlights(link, resetHighlight);
                }
            }
        }

        private void ResetHighlights(SankeyLink link, bool resetHighlight = true)
        {
            link.Shape.Stroke = resetHighlightLinkBrushes.Find(l => l.From == link.FromNode.Label.Text && l.To == link.ToNode.Label.Text).Brush.CloneCurrentValue();
            link.FromNode.Shape.Fill = resetHighlightNodeBrushes[link.FromNode.Label.Text].CloneCurrentValue();
            link.ToNode.Shape.Fill = resetHighlightNodeBrushes[link.ToNode.Label.Text].CloneCurrentValue();

            if(resetHighlight)
            {
                link.IsHighlight = false;
                link.FromNode.IsHighlight = false;
                link.ToNode.IsHighlight = false;
            }
        }

        #endregion

        #region Fields & Properties

        #region dependency properties

        public IEnumerable<SankeyDataRow> Datas
        {
            get { return (IEnumerable<SankeyDataRow>)GetValue(DatasProperty); }
            set { SetValue(DatasProperty, value); }
        }

        public static readonly DependencyProperty DatasProperty = DependencyProperty.Register("Datas", typeof(IEnumerable<SankeyDataRow>), typeof(SankeyDiagram), new PropertyMetadata(new List<SankeyDataRow>(), OnDatasSourceChanged));

        public Dictionary<string, Brush> NodeBrushes
        {
            get { return (Dictionary<string, Brush>)GetValue(NodeBrushesProperty); }
            set { SetValue(NodeBrushesProperty, value); }
        }

        public static readonly DependencyProperty NodeBrushesProperty = DependencyProperty.Register("NodeBrushes", typeof(Dictionary<string, Brush>), typeof(SankeyDiagram), new PropertyMetadata(OnNodeBrushesSourceChanged));

        public string HighlightNode
        {
            get { return (string)GetValue(HighlightNodeProperty); }
            set { SetValue(HighlightNodeProperty, value); }
        }

        public static readonly DependencyProperty HighlightNodeProperty = DependencyProperty.Register("HighlightNode", typeof(string), typeof(SankeyDiagram), new PropertyMetadata(null, null, HighlightNodeValueCallback));

        public SankeyLinkFinder HighlightLink
        {
            get { return (SankeyLinkFinder)GetValue(HighlightLinkProperty); }
            set { SetValue(HighlightLinkProperty, value); }
        }

        public static readonly DependencyProperty HighlightLinkProperty = DependencyProperty.Register("HighlightLink", typeof(SankeyLinkFinder), typeof(SankeyDiagram), new PropertyMetadata(null, null, HighlightLinkSourceValueCallback));

        #endregion

        #region diagram initial settings

        // you can custom the style of diagram panel with this property
        public StackPanel DiagramPanel { get; set; }

        // left to right by default
        public SankeyFlowDirection SankeyFlowDirection { get; set; }

        public double NodeThickness { get; set; }

        public double NodeIntervalSpace { get; set; }

        public double LinkLength { get; set; }

        // bezier curve control point1's position (point.X or point.Y)
        public double LinkPoint1Curveless { get; set; }

        // bezier curve control point2's position (point.X or point.Y)
        public double LinkPoint2Curveless { get; set; }

        /// <summary>
        /// using node links palette means coloring your link with fromNode's brush
        /// if NodeBrushes exist, nodes & links will use it, if not, use default palette
        /// </summary>
        public bool UseNodeLinksPalette { get; set; }

        /// <summary>
        /// brush applying on all nodes
        /// it does not work if you set UseNodeLinksPalette to true
        /// </summary>
        public Brush NodeBrush { get; set; }

        public Style LabelStyle { get; set; }

        public bool ShowLabels { get; set; }

        #endregion

        private Dictionary<int, List<SankeyNode>> currentNodes;

        private Dictionary<int, List<SankeyLink>> currentLinks;

        private Brush defaultLinkBrush;

        private int defaultNodeLinksPaletteIndex;

        private List<Brush> defaultNodeLinksPalette;

        private Dictionary<string, Brush> resetHighlightNodeBrushes;

        private Dictionary<string, Style> resetHighlightNodeStyles;

        private List<SankeyLinkStyleFinder> resetHighlightLinkBrushes;

        private bool isDiagramLoaded;

        #endregion
    }
}
