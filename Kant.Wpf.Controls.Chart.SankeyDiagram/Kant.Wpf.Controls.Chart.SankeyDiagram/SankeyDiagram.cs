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
    [TemplatePart(Name = "PartDiagramGrid", Type = typeof(Grid))]
    [TemplatePart(Name = "PartNodesPanel", Type = typeof(StackPanel))]
    [TemplatePart(Name = "PartLinksContainer", Type = typeof(Canvas))]
    public class SankeyDiagram : Control
    {
        #region Constructor

        static SankeyDiagram()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SankeyDiagram), new FrameworkPropertyMetadata(typeof(SankeyDiagram)));
        }

        public SankeyDiagram()
        {
            styleManager = new SankeyStyleManager(this);
            assist = new SankeyDiagramAssist(this, styleManager);

            Loaded += (s, e) =>
            {
                if (IsDiagramCreated)
                {
                    return;
                }

                assist.CreateDiagram(Datas);
                IsDiagramCreated = true;
            };
        }

        #endregion

        #region Methods

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var grid = GetTemplateChild("PartDiagramGrid") as Grid;
            var panel = GetTemplateChild("PartNodesPanel") as StackPanel;
            var canvas = GetTemplateChild("PartDiagramCanvas") as Canvas;

            if (grid == null)
            {
                throw new MissingMemberException("can not find template child PartDiagramGrid.");
            }
            else
            {
                DiagramGrid = grid;
            }

            if (canvas == null)
            {
                throw new MissingMemberException("can not find template child PartDiagramCanvas.");
            }
            else
            {
                DiagramCanvas = canvas;
            }
        }

        #region dependency property methods

        private static void OnDatasSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((SankeyDiagram)o).assist.UpdateDiagram((IEnumerable<SankeyDataRow>)e.NewValue);
        }

        private static void OnNodeBrushesSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var diagram = (SankeyDiagram)o;
            diagram.styleManager.UpdateNodeBrushes((Dictionary<string, Brush>)e.NewValue, diagram.assist.CurrentNodes, diagram.assist.CurrentLinks);
        }

        private static void OnSankeyFlowDirectionSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var diagram = (SankeyDiagram)o;
            diagram.assist.UpdateDiagram(diagram.Datas);
        }

        private static void OnShowLabelsSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var diagram = (SankeyDiagram)o;
            diagram.styleManager.ChangeLabelsVisibility((bool)e.NewValue, diagram.assist.CurrentLabels);
        }

        private static void OnHighlightModeSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var diagram = (SankeyDiagram)o;
            diagram.SetCurrentValue(SankeyDiagram.HighlightNodeProperty, null);
            diagram.SetCurrentValue(SankeyDiagram.HighlightLinkProperty, null);
        }

        private static object HighlightNodeValueCallback(DependencyObject o, object value)
        {
            var diagram = (SankeyDiagram)o;
            diagram.styleManager.HighlightingNode((string)value, diagram.assist.CurrentNodes, diagram.assist.CurrentLinks);

            return value;
        }

        private static object HighlightLinkSourceValueCallback(DependencyObject o, object value)
        {
            var diagram = (SankeyDiagram)o;
            diagram.styleManager.HighlightingLink((SankeyLinkFinder)value, diagram.assist.CurrentNodes, diagram.assist.CurrentLinks);

            return value;
        }

        #endregion

        #endregion

        #region Fields & Properties

        #region dependency properties

        public IEnumerable<SankeyDataRow> Datas
        {
            get { return (IEnumerable<SankeyDataRow>)GetValue(DatasProperty); }
            set { SetValue(DatasProperty, value); }
        }

        public static readonly DependencyProperty DatasProperty = DependencyProperty.Register("Datas", typeof(IEnumerable<SankeyDataRow>), typeof(SankeyDiagram), new PropertyMetadata(new List<SankeyDataRow>(), OnDatasSourceChanged));

        /// <summary>
        /// if the value is null, keeping last brushes
        /// </summary>
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

        public static readonly DependencyProperty HighlightNodeProperty = DependencyProperty.Register("HighlightNode", typeof(string), typeof(SankeyDiagram), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null, HighlightNodeValueCallback));

        public SankeyLinkFinder HighlightLink
        {
            get { return (SankeyLinkFinder)GetValue(HighlightLinkProperty); }
            set { SetValue(HighlightLinkProperty, value); }
        }

        public static readonly DependencyProperty HighlightLinkProperty = DependencyProperty.Register("HighlightLink", typeof(SankeyLinkFinder), typeof(SankeyDiagram), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null, HighlightLinkSourceValueCallback));

        /// <summary>
        /// LeftToRight by default
        /// </summary>
        public SankeyFlowDirection SankeyFlowDirection
        {
            get { return (SankeyFlowDirection)GetValue(SankeyFlowDirectionProperty); }
            set { SetValue(SankeyFlowDirectionProperty, value); }
        }

        public static readonly DependencyProperty SankeyFlowDirectionProperty = DependencyProperty.Register("SankeyFlowDirection", typeof(SankeyFlowDirection), typeof(SankeyDiagram), new PropertyMetadata(SankeyFlowDirection.LeftToRight, OnSankeyFlowDirectionSourceChanged));

        /// <summary>
        /// Show labels by default
        /// </summary>
        public bool ShowLabels
        {
            get { return (bool)GetValue(ShowLabelsProperty); }
            set { SetValue(ShowLabelsProperty, value); }
        }

        public static readonly DependencyProperty ShowLabelsProperty = DependencyProperty.Register("ShowLabels", typeof(bool), typeof(SankeyDiagram), new PropertyMetadata(true, OnShowLabelsSourceChanged));

        /// <summary>
        /// MouseLeftButtonUp by default
        /// </summary>
        public SankeyHighlightMode HighlightMode
        {
            get { return (SankeyHighlightMode)GetValue(HighlightModeProperty); }
            set { SetValue(HighlightModeProperty, value); }
        }

        public static readonly DependencyProperty HighlightModeProperty = DependencyProperty.Register("HighlightMode", typeof(SankeyHighlightMode), typeof(SankeyDiagram), new PropertyMetadata(SankeyHighlightMode.MouseLeftButtonUp, OnHighlightModeSourceChanged));

        #endregion

        #region diagram initial settings

        /// <summary>
        /// 10 by default
        /// </summary>
        public double NodeThickness { get; set; }

        /// <summary>
        /// 5 by default
        /// </summary>
        public double NodeGap { get; set; }

        /// <summary>
        /// bezier curve control point1's position (point.X or point.Y)
        /// 0.4 by default
        /// </summary>
        public double LinkPoint1Curveless { get; set; }

        /// <summary>
        /// bezier curve control point2's position (point.X or point.Y)
        /// 0.6 by default
        /// </summary>
        public double LinkPoint2Curveless { get; set; }

        /// <summary>
        /// true by default
        /// using node links palette means coloring your link with fromNode's brush
        /// if NodeBrushes exist, nodes & links will use it, if not, use default palette
        /// </summary>
        public bool UseNodeLinksPalette { get; set; }

        /// <summary>
        /// get default palette for reference
        /// </summary>
        public List<Brush> DefaultNodeLinksPalette
        {
            get
            {
                return styleManager.DefaultNodeLinksPalette;
            }
        }

        /// <summary>
        /// brush applying on all nodes
        /// it does not work if you set UseNodeLinksPalette to true
        /// may be not, it will be a dp in the future
        /// </summary>
        public Brush NodeBrush { get; set; }

        public Style LabelStyle { get; set; }

        public Style HighlightLabelStyle { get; set; }

        public Brush HighlightBrush { get; set; }

        /// <summary>
        /// apply to nodes, links
        /// it does not work if you have already setted HighlightBrush property
        /// 1.0 by default
        /// </summary>
        public double HighlightOpacity { get; set; }

        /// <summary>
        /// apply to nodes, links
        /// 0.25 by default
        /// </summary>
        public double LoweredOpacity { get; set; }

        #endregion

        /// <summary>
        /// you can custom the style of diagram grid with this property
        /// </summary>
        public Grid DiagramGrid { get; private set; }

        public Canvas DiagramCanvas { get; private set; }

        public bool IsDiagramCreated { get; private set; }

        private SankeyStyleManager styleManager;

        private SankeyDiagramAssist assist;

        #endregion
    }
}
