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
            NodeLength = 10;
            NodeFill = new SolidColorBrush(Colors.Black);
            defaultLinkBrush = new SolidColorBrush(Colors.Gray) { Opacity = 0.55 };
            LinkPoint1Curveless = 0.4;
            LinkPoint2Curveless = 0.6;
            var labelStye = new Style(typeof(TextBlock));
            labelStye.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(2)));
            LabelStyle = labelStye;
            ShowLabels = true;

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

        private void CreateDiagram(Dictionary<int, List<SankeyNode>> nodes, Dictionary<int, List<SankeyLink>> links)
        {
            if(DiagramPanel.ActualHeight <= 0 || DiagramPanel.ActualWidth <= 0 || nodes == null || nodes.Count < 2 || links == null || links.Count == 0)
            {
                return;
            }

            var panelLength = 0.0;
            var linkLength = 0.0;

            if(IsDiagramVertical)
            {
                panelLength = DiagramPanel.ActualWidth;
                linkLength = LinkLength > 0 ? LinkLength : (DiagramPanel.ActualHeight - nodes.Count * NodeLength) / links.Count;
            }
            else
            {
                DiagramPanel.Orientation = Orientation.Horizontal;
                panelLength = DiagramPanel.ActualHeight;
                linkLength = LinkLength > 0 ? LinkLength : (DiagramPanel.ActualWidth - nodes.Count * NodeLength) / links.Count;
            }

            var nodesGroupContainerStyle = new Style();
            var margin = IsDiagramVertical ? new Thickness(0, 0, NodeIntervalSpace, 0) : new Thickness(0, NodeIntervalSpace, 0, 0);
            nodesGroupContainerStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, margin));
            var maxGroupLength = 0.0;
            var maxGroupCount = 0;

            foreach(var levelNodes in nodes.Values)
            {
                var tempGroupLength = 0.0;

                foreach(var node in levelNodes)
                {
                    if (IsDiagramVertical)
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
                throw new ArgumentOutOfRangeException("panel's length is not enough.");
            }

            var unitLength = nodesOverallLength / maxGroupLength;
            var linkContainers = new List<Canvas>();

            for (var index = 0; index < nodes.Count; index++)
            {
                var nodesGroup = new ItemsControl();
                var nodesGroupWidth = 0.0;
                nodesGroup.ItemContainerStyle = nodesGroupContainerStyle;

                if(IsDiagramVertical)
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

                for (var nIndex = 0; nIndex < nodes[index].Count; nIndex++)
                {
                    if (IsDiagramVertical)
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

                DiagramPanel.Children.Add(nodesGroup);

                if (index != nodes.Count - 1)
                {
                    var canvas = new Canvas();
                    canvas.ClipToBounds = true;

                    if(IsDiagramVertical)
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
                    if (IsDiagramVertical)
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
                    linkContainers[index].Children.Add(DrawLink(links[index][lIndex], linkLength, unitLength).Shape);
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
                                nodes = UpdateNodes(nodes, levelIndex, data, data.To);
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

                nodes = UpdateNodes(nodes, levelIndex, data, label);
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
                        if (IsDiagramVertical)
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
                        if (IsDiagramVertical)
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

                    if (IsDiagramVertical)
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
                        shape.Stroke = data.LinkStroke == null ? defaultLinkBrush : data.LinkStroke;
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

        private Dictionary<int, List<SankeyNode>> UpdateNodes(Dictionary<int, List<SankeyNode>> nodes, int index, SankeyDataRow data, string label)
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
                Style = LabelStyle
            };

            var shape = new Rectangle()
            {
                Fill = NodeFill
            };

            if(IsDiagramVertical)
            {
                shape.Height = NodeLength;
            }
           else
            {
                shape.Width = NodeLength;
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

            link.Shape.StrokeThickness = link.Shape.StrokeThickness * unitLength;
            var fromPoint = new Point();
            var toPoint = new Point();
            var bezierControlPoint1 = new Point();
            var bezierCOntrolPoint2 = new Point();

            if(IsDiagramVertical)
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

        private void AddLabels(Canvas container, Dictionary<int, List<SankeyNode>> nodes, int index)
        {
            foreach (var node in nodes[index])
            {
                node.Label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                if (IsDiagramVertical)
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

                    if(index == nodes.Count - 1)
                    {
                        Canvas.SetRight(node.Label, 0);
                    }
                }

                container.Children.Add(node.Label);
            }
        }

        #endregion

        #region Fields & Properties

        public IEnumerable<SankeyDataRow> Datas
        {
            get { return (IEnumerable<SankeyDataRow>)GetValue(DatasProperty); }
            set { SetValue(DatasProperty, value); }
        }

        public static readonly DependencyProperty DatasProperty = DependencyProperty.Register("Datas", typeof(IEnumerable<SankeyDataRow>), typeof(SankeyDiagram), new PropertyMetadata(new List<SankeyDataRow>(), OnDatasSourceChanged));

        public bool IsDiagramVertical { get; set; }

        public double LinkLength { get; set; }

        public double LinkPoint1Curveless { get; set; }

        public double LinkPoint2Curveless { get; set; }

        public double NodeLength { get; set; }

        public double NodeIntervalSpace { get; set; }

        public Brush NodeFill { get; set; }

        public Style LabelStyle { get; set; }

        public bool ShowLabels { get; set; }

        public StackPanel DiagramPanel { get; set; }

        private Dictionary<int, List<SankeyNode>> currentNodes;

        private Dictionary<int, List<SankeyLink>> currentLinks;

        private Brush defaultLinkBrush;

        private bool isDiagramLoaded;

        #endregion
    }
}
