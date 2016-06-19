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
            styleManager = new SankeyStyleManager(this);
            assist = new SankeyDiagramAssist(this, styleManager);

            Loaded += (s, e) =>
            {
                if (IsDiagramCreated)
                {
                    return;
                }

                assist.CreateDiagram();
                IsDiagramCreated = true;
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
            ((SankeyDiagram)o).assist.UpdateDiagram(e.NewValue as IEnumerable<SankeyDataRow>, e.OldValue as IEnumerable<SankeyDataRow>);
        }

        private static void OnNodeBrushesSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((SankeyDiagram)o).assist.UpdateNodeBrushes(e.NewValue as Dictionary<string, Brush>, e.OldValue as Dictionary<string, Brush>);
        }

        private static object HighlightNodeValueCallback(DependencyObject o, object value)
        {
            ((SankeyDiagram)o).assist.HighlightingNode(value as string);

            return value;
        }

        private static object HighlightLinkSourceValueCallback(DependencyObject o, object value)
        {
            ((SankeyDiagram)o).assist.HighlightingLink(value as SankeyLinkFinder);

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

        #endregion

        #region diagram initial settings

        /// <summary>
        /// LeftToRight by default
        /// </summary>
        public SankeyFlowDirection SankeyFlowDirection { get; set; }

        /// <summary>
        /// Show by default
        /// </summary>
        public bool ShowLabels { get; set; }

        /// <summary>
        /// MouseLeftButtonUp by default
        /// </summary>
        public SankeyHighlightMode HighlightMode { get; set; }

        /// <summary>
        /// 10 by default
        /// </summary>
        public double NodeThickness { get; set; }

        /// <summary>
        /// 5 by default
        /// </summary>
        public double NodeIntervalSpace { get; set; }

        /// <summary>
        /// default value is calculated based on diagram panel size
        /// </summary>
        public double LinkAeraLength { get; set; }

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
        /// you can custom the style of diagram panel with this property
        /// </summary>
        public StackPanel DiagramPanel { get; set; }

        public bool IsDiagramCreated { set; get; }

        private SankeyStyleManager styleManager;

        private SankeyDiagramAssist assist;

        #endregion
    }
}
