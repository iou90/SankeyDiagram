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
    [TemplatePart(Name = "PartDiagramCanvas", Type = typeof(Canvas))]
    public class SankeyDiagram : Control, IDisposable
    {
        #region Constructor

        static SankeyDiagram()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SankeyDiagram), new FrameworkPropertyMetadata(typeof(SankeyDiagram)));
        }

        public SankeyDiagram()
        {
            disposedValue = false;
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
            var grid = GetTemplateChild("PartDiagramGrid") as Grid;
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
                diagramCanvas = canvas;
                assist.DiagramCanvas = diagramCanvas;
                assist.DiagramCanvas.SizeChanged += assist.DiagramCanvasSizeChanged;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (isLabelStyleChangedNeedReRender)
            {
                assist.CreateDiagram();
            }
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if(assist != null)
                    {
                        assist.ClearDiagram();
                    }
                }

                disposedValue = true;
            }
        }

        ~SankeyDiagram()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region dependency property methods

        private static void OnDatasSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((SankeyDiagram)o).assist.UpdateDiagram((IEnumerable<SankeyDataRow>)e.NewValue);
        }

        private static void OnLinkCurvenessSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var diagram = (SankeyDiagram)o;
            diagram.styleManager.UpdateLinkCurvature((double)e.NewValue, diagram.assist.CurrentLinks);
        }

        private static void OnNodeBrushSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var diagram = (SankeyDiagram)o;
            diagram.styleManager.UpdateNodeBrushes((Brush)e.NewValue, diagram.assist.CurrentNodes);
        }

        private static void OnNodeBrushesSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var diagram = (SankeyDiagram)o;
            diagram.styleManager.UpdateNodeBrushes((Dictionary<string, Brush>)e.NewValue, diagram.assist.CurrentNodes, diagram.assist.CurrentLinks);
        }

        private static void OnSankeyFlowDirectionSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ReLoadDiagram((SankeyDiagram)o);
        }

        private static void OnLabelStyleSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var diagram = (SankeyDiagram)o;
            diagram.styleManager.ClearHighlight();
            diagram.assist.UpdateLabelStyle();

            if (diagram.FirstAndLastLabelPosition == FirstAndLastLabelPosition.Outward)
            {
                diagram.assist.RemeatureLabel();
                diagram.isLabelStyleChangedNeedReRender = true;
            }
        }

        private static void ReLoadDiagram(SankeyDiagram diagram)
        {
            if (diagram.IsDiagramCreated)
            {
                diagram.styleManager.ClearHighlight();
                diagram.assist.CreateDiagram();
            }
        }

        private static void OnShowLabelsSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var diagram = (SankeyDiagram)o;
            diagram.styleManager.ChangeLabelsVisibility((bool)e.NewValue, diagram.assist.CurrentLabels);
        }

        private static void OnToolTipTemplateSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var diagram = (SankeyDiagram)o;
            diagram.styleManager.ChangeToolTipTemplate((ControlTemplate)e.NewValue, diagram.assist.CurrentLinks);
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

        private static object HighlightLinkValueCallback(DependencyObject o, object value)
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
        /// 0.55 by default
        /// </summary>
        public double LinkCurvature
        {
            get { return (double)GetValue(LinkCurvatureProperty); }
            set { SetValue(LinkCurvatureProperty, value); }
        }

        public static readonly DependencyProperty LinkCurvatureProperty = DependencyProperty.Register("LinkCurvature", typeof(double), typeof(SankeyDiagram), new PropertyMetadata(0.55, OnLinkCurvenessSourceChanged));

        /// <summary>
        /// universal node brush,
        /// it will not work if UsePallette set to be NodesLinks
        /// </summary>
        public Brush NodeBrush
        {
            get { return (Brush)GetValue(NodeBrushProperty); }
            set { SetValue(NodeBrushProperty, value); }
        }

        public static readonly DependencyProperty NodeBrushProperty = DependencyProperty.Register("NodeBrush", typeof(Brush), typeof(SankeyDiagram), new PropertyMetadata(new SolidColorBrush(Colors.Black), OnNodeBrushSourceChanged));

        public Dictionary<string, Brush> NodeBrushes
        {
            get { return (Dictionary<string, Brush>)GetValue(NodeBrushesProperty); }
            set { SetValue(NodeBrushesProperty, value); }
        }

        public static readonly DependencyProperty NodeBrushesProperty = DependencyProperty.Register("NodeBrushes", typeof(Dictionary<string, Brush>), typeof(SankeyDiagram), new PropertyMetadata(OnNodeBrushesSourceChanged));

        public Style LabelStyle
        {
            get { return (Style)GetValue(LabelStyleProperty); }
            set { SetValue(LabelStyleProperty, value); }
        }

        public static readonly DependencyProperty LabelStyleProperty = DependencyProperty.Register("LabelStyle", typeof(Style), typeof(SankeyDiagram),new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnLabelStyleSourceChanged));

        public string HighlightNode
        {
            get { return (string)GetValue(HighlightNodeProperty); }
            set { SetValue(HighlightNodeProperty, value); }
        }

        public static readonly DependencyProperty HighlightNodeProperty = DependencyProperty.Register("HighlightNode", typeof(string), typeof(SankeyDiagram), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null, HighlightNodeValueCallback));

        /// <summary>
        /// MouseLeftButtonUp by default
        /// </summary>
        public HighlightMode HighlightMode
        {
            get { return (HighlightMode)GetValue(HighlightModeProperty); }
            set { SetValue(HighlightModeProperty, value); }
        }

        public static readonly DependencyProperty HighlightModeProperty = DependencyProperty.Register("HighlightMode", typeof(HighlightMode), typeof(SankeyDiagram), new PropertyMetadata(HighlightMode.MouseLeftButtonUp, OnHighlightModeSourceChanged));

        public SankeyLinkFinder HighlightLink
        {
            get { return (SankeyLinkFinder)GetValue(HighlightLinkProperty); }
            set { SetValue(HighlightLinkProperty, value); }
        }

        public static readonly DependencyProperty HighlightLinkProperty = DependencyProperty.Register("HighlightLink", typeof(SankeyLinkFinder), typeof(SankeyDiagram), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null, HighlightLinkValueCallback));

        /// <summary>
        /// LeftToRight by default
        /// </summary>
        public FlowDirection SankeyFlowDirection
        {
            get { return (FlowDirection)GetValue(SankeyFlowDirectionProperty); }
            set { SetValue(SankeyFlowDirectionProperty, value); }
        }

        public static readonly DependencyProperty SankeyFlowDirectionProperty = DependencyProperty.Register("SankeyFlowDirection", typeof(FlowDirection), typeof(SankeyDiagram), new PropertyMetadata(Chart.FlowDirection.LeftToRight, OnSankeyFlowDirectionSourceChanged));

        /// <summary>
        /// Show labels by default
        /// </summary>
        public bool ShowLabels
        {
            get { return (bool)GetValue(ShowLabelsProperty); }
            set { SetValue(ShowLabelsProperty, value); }
        }

        public static readonly DependencyProperty ShowLabelsProperty = DependencyProperty.Register("ShowLabels", typeof(bool), typeof(SankeyDiagram), new PropertyMetadata(true, OnShowLabelsSourceChanged));

        public ControlTemplate ToolTipTemplate
        {
            get { return (ControlTemplate)GetValue(ToolTipTemplateProperty); }
            set { SetValue(ToolTipTemplateProperty, value); }
        }

        public static readonly DependencyProperty ToolTipTemplateProperty = DependencyProperty.Register("ToolTipTemplate", typeof(ControlTemplate), typeof(SankeyDiagram), new PropertyMetadata(OnToolTipTemplateSourceChanged));

        #endregion

        #region diagram initial settings

        /// <summary>
        /// 10 by default
        /// will be changed when panel size changed, in the future
        /// </summary>
        public double NodeThickness { get; set; }

        /// <summary>
        /// 5 by default
        /// will be changed when panel size changed, in the future
        /// </summary>
        public double NodeGap { get; set; }

        /// <summary>
        /// NodesLinks by default
        /// </summary>
        public SankeyPalette UsePallette { get; set; }

        //public Style LabelStyle { get; set; }

        public FirstAndLastLabelPosition FirstAndLastLabelPosition { get; set; }

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

        public bool IsDiagramCreated { get; private set; }

        private Canvas diagramCanvas;

        private SankeyStyleManager styleManager;

        private SankeyDiagramAssist assist;

        private bool isLabelStyleChangedNeedReRender;

        private bool disposedValue;

        #endregion
    }
}
