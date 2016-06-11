using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

            // test
            var datas = new List<SankeyDataRow>()
            {
                new SankeyDataRow("A", "C", 2),
                new SankeyDataRow("A", "D", 3),
                new SankeyDataRow("A", "E", 1),
                new SankeyDataRow("B", "C", 1),
                new SankeyDataRow("B", "D", 2),
                new SankeyDataRow("B", "E", 1),
                new SankeyDataRow("C", "F", 2),
                new SankeyDataRow("C", "H", 1),
                new SankeyDataRow("C", "J", 1),
                new SankeyDataRow("D", "F", 2),
                new SankeyDataRow("D", "H", 1),
                new SankeyDataRow("D", "I", 1),
                new SankeyDataRow("D", "J", 1),
                new SankeyDataRow("E", "H", 1),
                new SankeyDataRow("E", "I", 1),
            };

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var nodeDictionary = ProduceNodes(datas, new Dictionary<int, List<SankeyNode>>(), 0);
            currentNodes = CalculateNodesLength(datas, nodeDictionary);
            currentLinks = ProduceLinks(datas, nodeDictionary);
            stopwatch.Stop();

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
            DiagramPanel = (StackPanel)GetTemplateChild("PartDiagramPanel");
        }

        private static void OnDatasSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((SankeyDiagram)o).OnDatasChanged((IEnumerable<SankeyDataRow>)e.NewValue, (IEnumerable<SankeyDataRow>)e.OldValue);
        }

        private void OnDatasChanged(IEnumerable<SankeyDataRow> newDatas, IEnumerable<SankeyDataRow> oldDatas)
        {
            if(newDatas == oldDatas)
            {
                return;
            }

            if(newDatas == null || newDatas.Count() == 0)
            {
                // clean panel
            }

            // key means col/row index
            var nodeDictionary = ProduceNodes(newDatas, new Dictionary<int, List<SankeyNode>>(), 0);

            // calculate node height
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
            if(DiagramPanel.ActualHeight <= 0 || nodes == null || nodes.Count < 2 || links == null || links.Count == 0)
            {
                return;
            }

            var panelLength = DiagramPanel.ActualHeight;
            var linkLength = (DiagramPanel.ActualWidth - nodes.Count * NodeLength) / links.Count;
            var nodeIntervalSpace = NodeIntervalSpace <= 0 ? defaultNodeIntervalSpace : NodeIntervalSpace;
            var nodesGroupContainerStyle = new Style();
            nodesGroupContainerStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, nodeIntervalSpace, 0, 0)));
            var maxGroupLength = 0.0;
            var maxGroupCount = 0;

            for(var index = 0; index < nodes.Count; index++)
            {
                var tempGroupLength = 0.0;

                for(var gIndex = 0; gIndex < nodes[index].Count; gIndex++)
                {
                    tempGroupLength += nodes[index][gIndex].Shape.Height;    
                }

                if(tempGroupLength > maxGroupLength)
                {
                    maxGroupLength = tempGroupLength;
                    maxGroupCount = nodes[index].Count;
                }
            }

            var unitLength = (int)((panelLength - ((maxGroupCount - 1) * nodeIntervalSpace)) / maxGroupLength);

            for (var index = 0; index < nodes.Count; index++)
            {
                var nodesGroup = new ItemsControl();
                nodesGroup.VerticalAlignment = VerticalAlignment.Bottom;
                nodesGroup.ItemContainerStyle = nodesGroupContainerStyle;

                for (var gIndex = 0; gIndex < nodes[index].Count; gIndex++)
                {
                    nodes[index][gIndex].Shape.Height = nodes[index][gIndex].Shape.Height * unitLength;
                    nodesGroup.Items.Add(nodes[index][gIndex].Shape);
                }

                DiagramPanel.Children.Add(nodesGroup);
                var canvas = new Canvas() { Width = linkLength };
                DiagramPanel.Children.Add(canvas);
            }
                    //var geometry = new PathGeometry()
                    //{
                    //    Figures = new PathFigureCollection()
                    //    {
                    //        new PathFigure()
                    //        {
                    //            StartPoint = new Point(),

                //            Segments = new PathSegmentCollection()
                //            {
                //                new BezierSegment()
                //                {
                //                    Point1 = new Point(),
                //                    Point2 = new Point(),
                //                    Point3 = new Point()
                //                }
                //            }
                //        }
                //    }
                //};

                //geometry.Freeze();
        }

        private Dictionary<int, List<SankeyNode>> ProduceNodes(IEnumerable<SankeyDataRow> datas, Dictionary<int, List<SankeyNode>> nodes, int levelIndex)
        {
            var isDatasUpdated = false;
            var tempDatas = datas.ToList();

            for (var index = 0; index < tempDatas.Count; index++)
            {
                // if a node name only exists in From property, it'll be in the first col/row
                if (levelIndex == 0 && tempDatas.Exists(d => d.To == tempDatas[index].From))
                {
                    continue;
                }

                if(levelIndex > 0)
                {
                    var node = nodes[levelIndex - 1].Find(findNode => findNode.Label.Text == tempDatas[index].From);

                    if(node != null)
                    { 
                        var previousLevelNodes = tempDatas.FindAll(d => d.From == node.Label.Text);

                        if (previousLevelNodes.Count == 0)
                        {
                            break;
                        }

                        for (var pIndex = 0; pIndex < previousLevelNodes.Count; pIndex++)
                        {
                            if (previousLevelNodes[pIndex].To == tempDatas[index].To)
                            {
                                isDatasUpdated = true;
                                nodes = UpdateNodes(nodes, levelIndex, tempDatas[index], tempDatas[index].To);
                            }
                        }
                    }

                    var isDataDuplicate = false;

                    for (var i = 1; i < levelIndex + 1; i++)
                    {
                        if (nodes[levelIndex - i].Exists(findNode => findNode.Label.Text == tempDatas[index].From))
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
                var label = !tempDatas.Exists(d => d.From == tempDatas[index].From) ? tempDatas[index].To : tempDatas[index].From;

                nodes = UpdateNodes(nodes, levelIndex, tempDatas[index], label);
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
            var nodeFromHeightDictionary = new Dictionary<string, double>();
            var nodeToHeightDictionary = new Dictionary<string, double>();
            var tempDatas = datas.ToList();

            for (var index = 0; index < tempDatas.Count; index++)
            {
                var length = tempDatas[index].Weight;

                if (nodeFromHeightDictionary.Keys.Contains(tempDatas[index].From))
                {
                    nodeFromHeightDictionary[tempDatas[index].From] += length;
                }
                else
                {
                    nodeFromHeightDictionary.Add(tempDatas[index].From, length);
                }

                if (nodeToHeightDictionary.Keys.Contains(tempDatas[index].To))
                {
                    nodeToHeightDictionary[tempDatas[index].To] += length;
                }
                else
                {
                    nodeToHeightDictionary.Add(tempDatas[index].To, length);
                }
            }

            for (var index = 0; index < nodes.Count; index++)
            {
                if (index == nodes.Count - 1)
                {
                    foreach (var node in nodes[index])
                    {
                        node.Shape.Height = nodeToHeightDictionary[node.Label.Text];
                    }

                    continue;
                }

                if (index == 0)
                {
                    foreach (var node in nodes[index])
                    {
                        node.Shape.Height = nodeFromHeightDictionary[node.Label.Text];
                    }

                    continue;
                }

                foreach (var node in nodes[index])
                {
                    var fromHeight = nodeFromHeightDictionary[node.Label.Text];
                    var toHeight = nodeToHeightDictionary[node.Label.Text];
                    node.Shape.Height = fromHeight > toHeight ? fromHeight : toHeight;
                }
            }

            return nodes;
        }

        private Dictionary<int, List<SankeyLink>> ProduceLinks(IEnumerable<SankeyDataRow> datas, Dictionary<int, List<SankeyNode>> nodes)
        {
            var linkDictionary = new Dictionary<int, List<SankeyLink>>();
            var tempDatas = datas.ToList();

            for (var index = 0; index < tempDatas.Count; index++)
            {
                for (var fCount = 0; fCount < nodes.Count; fCount++)
                {
                    var fromNode = nodes[fCount].Find(findNode => findNode.Label.Text == tempDatas[index].From);

                    if (fromNode != null)
                    {
                        var link = new SankeyLink()
                        {
                            FromNode = fromNode
                        };

                        for (var tIndex = 0; tIndex < nodes.Count; tIndex++)
                        {
                            var toNode = nodes[tIndex].Find(findNode => findNode.Label.Text == tempDatas[index].To);

                            if (toNode != null)
                            {
                                link.ToNode = toNode;

                                break;
                            }
                        }

                        var shape = new Path();
                        shape.Fill = tempDatas[index].LinkFill == null ? defaultLinkBrush : tempDatas[index].LinkFill;
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
                Width = NodeLength,
                Fill = NodeFill
            };

            return new SankeyNode(shape, l);
        }

        #endregion

        #region Fields & Properties

        public IEnumerable<SankeyDataRow> Datas
        {
            get { return (IEnumerable<SankeyDataRow>)GetValue(DatasProperty); }
            set { SetValue(DatasProperty, value); }
        }

        public static readonly DependencyProperty DatasProperty = DependencyProperty.Register("Datas", typeof(IEnumerable<SankeyDataRow>), typeof(SankeyDiagram), new PropertyMetadata(new List<SankeyDataRow>(), OnDatasSourceChanged));

        public double LinkLength { get; set; }

        public double NodeLength { get; set; }

        public double NodeIntervalSpace { get; set; }

        public Brush NodeFill { get; set; }

        public Style LabelStyle { get; set; }

        public StackPanel DiagramPanel { get; set; }

        private Dictionary<int, List<SankeyNode>> currentNodes;

        private Dictionary<int, List<SankeyLink>> currentLinks;

        private double defaultNodeIntervalSpace;

        private Brush defaultLinkBrush;

        private bool isDiagramLoaded;

        #endregion
    }
}
