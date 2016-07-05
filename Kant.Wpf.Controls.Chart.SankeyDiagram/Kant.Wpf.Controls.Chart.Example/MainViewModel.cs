using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace Kant.Wpf.Controls.Chart.Example
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Constructor

        public MainViewModel()
        {
            random = new Random();

            var datas = new List<SankeyDataRow>()
            {
                new SankeyDataRow("Agricultural 'waste'", "Bio-conversion", 124.729),
                new SankeyDataRow("Bio-conversion","Liquid", 0.597),
                new SankeyDataRow("Bio-conversion", "Losses", 26.862),
                new SankeyDataRow("Bio-conversion", "Solid", 280.322),
                new SankeyDataRow("Bio-conversion", "Gas", 81.144),
                new SankeyDataRow("Biofuel imports", "Liquid", 35),
                new SankeyDataRow("Biomass imports", "Solid", 35),
                new SankeyDataRow("Coal imports", "Coal", 11.606),
                new SankeyDataRow("Coal reserves", "Coal", 63.965),
                new SankeyDataRow("Coal", "Solid", 75.571),
                new SankeyDataRow("District heating", "Industry", 10.639),
                new SankeyDataRow("District heating", "Heating and cooling - commercial", 22.505),
                new SankeyDataRow("District heating", "Heating and cooling - homes", 46.184),
                new SankeyDataRow("Electricity grid", "Over generation / exports", 104.453),
                new SankeyDataRow("Electricity grid", "Heating and cooling - homes", 113.726),
                new SankeyDataRow("Electricity grid", "H2 conversion", 27.14),
                new SankeyDataRow("Electricity grid", "Industry", 342.165),
                new SankeyDataRow("Electricity grid", "Road transport", 37.797),
                new SankeyDataRow("Electricity grid", "Agriculture", 4.412),
                new SankeyDataRow("Electricity grid", "Heating and cooling - commercial", 40.858),
                new SankeyDataRow("Electricity grid", "Losses", 56.691),
                new SankeyDataRow("Electricity grid", "Rail transport", 7.863),
                new SankeyDataRow("Electricity grid", "Lighting & appliances - commercial", 90.008),
                new SankeyDataRow("Electricity grid", "Lighting & appliances - homes", 93.494),
                new SankeyDataRow("Gas imports", "Ngas", 40.719),
                new SankeyDataRow("Gas reserves", "Ngas", 82.233),
                new SankeyDataRow("Gas", "Heating and cooling - commercial", 0.129),
                new SankeyDataRow("Gas", "Losses", 1.401),
                new SankeyDataRow("Gas", "Thermal generation", 151.891),
                new SankeyDataRow("Gas", "Agriculture", 2.096),
                new SankeyDataRow("Gas", "Industry", 48.58),
                new SankeyDataRow("Geothermal", "Electricity grid", 7.013),
                new SankeyDataRow("H2 conversion", "H2", 20.897),
                new SankeyDataRow("H2 conversion", "Losses", 6.242),
                new SankeyDataRow("H2", "Road transport", 20.897),
                new SankeyDataRow("Hydro", "Electricity grid", 6.995),
                new SankeyDataRow("Liquid", "Industry", 121.066),
                new SankeyDataRow("Liquid", "International shipping", 128.69),
                new SankeyDataRow("Liquid", "Road transport", 135.835),
                new SankeyDataRow("Liquid", "Domestic aviation", 14.458),
                new SankeyDataRow("Liquid", "International aviation", 206.267),
                new SankeyDataRow("Liquid", "Agriculture", 3.64),
                new SankeyDataRow("Liquid", "National navigation", 33.218),
                new SankeyDataRow("Liquid", "Rail transport", 4.413),
                new SankeyDataRow("Marine algae", "Bio-conversion", 4.375),
                new SankeyDataRow("Ngas", "Gas", 122.952),
                new SankeyDataRow("Nuclear", "Thermal generation", 839.978),
                new SankeyDataRow("Oil imports", "Oil", 504.287),
                new SankeyDataRow("Oil reserves", "Oil", 107.703),
                new SankeyDataRow("Oil", "Liquid", 611.99),
                new SankeyDataRow("Other waste", "Solid", 56.587),
                new SankeyDataRow("Other waste", "Bio-conversion", 77.81),
                new SankeyDataRow("Pumped heat",  "Heating and cooling - homes", 193.026),
                new SankeyDataRow("Pumped heat", "Heating and cooling - commercial", 70.672),
                new SankeyDataRow("Solar PV", "Electricity grid", 59.901),
                new SankeyDataRow("Solar Thermal", "Heating and cooling - homes", 19.263),
                new SankeyDataRow("Solar", "Solar Thermal", 19.263),
                new SankeyDataRow("Solar", "Solar PV", 59.901),
                new SankeyDataRow("Solid", "Agriculture", 0.882),
                new SankeyDataRow("Solid", "Thermal generation", 400.12),
                new SankeyDataRow("Solid", "Industry", 46.477),
                new SankeyDataRow("Thermal generation", "Electricity grid", 525.531),
                new SankeyDataRow("Thermal generation", "Losses", 787.129),
                new SankeyDataRow("Thermal generation", "District heating", 79.329),
                new SankeyDataRow("Tidal", "Electricity grid", 9.452),
                new SankeyDataRow("UK land based bioenergy", "Bio-conversion", 182.01),
                new SankeyDataRow("Wave", "Electricity grid", 19.013),
                new SankeyDataRow("Wind", "Electricity grid", 289.366)
            };

            //var datas = new List<SankeyDataRow>()
            //{
            //    new SankeyDataRow("A", "C", 255),
            //    new SankeyDataRow("A", "D", 355),
            //    new SankeyDataRow("B", "C", 555),
            //    new SankeyDataRow("B", "D", 255),
            //    new SankeyDataRow("B", "E", 1555),
            //    new SankeyDataRow("C", "H", 155),
            //    new SankeyDataRow("D", "F", 25),
            //    new SankeyDataRow("D", "G", 155),
            //    new SankeyDataRow("D", "H", 15),
            //    new SankeyDataRow("D", "I", 55),
            //    new SankeyDataRow("E", "H", 1555),
            //    new SankeyDataRow("B", "G", 255),
            //    new SankeyDataRow("A", "E", 95),
            //    new SankeyDataRow("E", "I", 1555),
            //    new SankeyDataRow("C", "G", 755),
            //    new SankeyDataRow("C", "F", 455),
            //};

            //SankeyNodeBrushes = new Dictionary<string, Brush>()
            //{
            //    { "A", new SolidColorBrush(Colors.Brown) { Opacity = 0.35 } },
            //    { "B", new SolidColorBrush(Colors.Aqua) { Opacity = 0.25 } },
            //    { "C", new SolidColorBrush(Colors.CornflowerBlue) { Opacity = 0.15 } },
            //    { "D", new SolidColorBrush(Colors.DimGray) { Opacity = 0.45 } },
            //    { "E", new SolidColorBrush(Colors.Firebrick) { Opacity = 0.65 } },
            //};

            SankeyDatas = datas;
            SankeyShowLabels = true;
            SankeyLinkCurvature = 0.95;
            SankeyFlowDirection = FlowDirection.TopToBottom;
        }

        #endregion

        #region Commands

        private ICommand testBigData;
        public ICommand TestBigData
        {
            get
            {
                return GetCommand(testBigData, new CommandBase(() =>
                {
                    var datas = new List<SankeyDataRow>();
                    var count = 0;

                    while (count < 100)
                    {
                        datas.Add(new SankeyDataRow(random.Next(9).ToString(), random.Next(10, 19).ToString(), random.Next(55, 155)));
                        count++;
                    }

                    SankeyDatas = datas;
                }));
            }
        }

        private ICommand clearDiagram;
        public ICommand ClearDiagram
        {
            get
            {
                return GetCommand(clearDiagram, new CommandBase(() =>
                {
                    SankeyDatas = null;
                }));
            }
        }

        private ICommand highlightingNode;
        public ICommand HighlightingNode
        {
            get
            {
                return GetCommand(highlightingNode, new CommandBase(() =>
                {
                    //var fromNodes = new List<string>();

                    //foreach(var data in SankeyDatas)
                    //{
                    //    if(!fromNodes.Exists(n => n == data.From))
                    //    {
                    //        fromNodes.Add(data.From);
                    //    }
                    //}

                    //HighlightSankeyNode = fromNodes[random.Next(fromNodes.Count)];
                    //HighlightSankeyNode = random.Next(25).ToString();
                    HighlightSankeyNode = "H";
                    //HighlightSankeyNode = "5";
                    //HighlightSankeyNode = "";
                    //HighlightSankeyNode = "Z";
                }));
            }
        }

        private ICommand highlightingLink;
        public ICommand HighlightingLink
        {
            get
            {
                return GetCommand(highlightingLink, new CommandBase(() =>
                {
                    HighlightSankeyLink = new SankeyLinkFinder("C", "F");
                    //HighlightSankeyLink = new SankeyLinkFinder("5", random.Next(9, 20).ToString());
                }));
            }
        }

        private ICommand changeStyles;
        public ICommand ChangeStyles
        {
            get
            {
                return GetCommand(changeStyles, new CommandBase(() =>
                {
                    if (random.Next(2) == 1)
                    {
                        SankeyNodeBrushes = new Dictionary<string, Brush>()
                        {
                            { "A", new SolidColorBrush(Colors.Brown) { Opacity = 0.35 } },
                            { "B", new SolidColorBrush(Colors.Aqua) { Opacity = 0.25 } },
                            { "C", new SolidColorBrush(Colors.CornflowerBlue) { Opacity = 0.15 } },
                            { "D", new SolidColorBrush(Colors.DimGray) { Opacity = 0.45 } },
                            { "E", new SolidColorBrush(Colors.Firebrick) { Opacity = 0.65 } },
                        };
                    }
                    else
                    {
                        SankeyNodeBrushes = null;
                    }

                    SankeyLinkCurvature = random.Next(1, 11) * 0.1;
                    SankeyFlowDirection = random.Next(2) == 1 ? FlowDirection.TopToBottom : FlowDirection.LeftToRight;
                    SankeyShowLabels = random.Next(2) == 1 ? false : true;
                    SankeyHighlightMode = random.Next(2) == 1 ? HighlightMode.MouseEnter : HighlightMode.MouseLeftButtonUp;
                }));
            }
        }

        #endregion

        #region Fields & Properties

        private List<SankeyDataRow> sankeyDatas;
        public List<SankeyDataRow> SankeyDatas
        {
            get
            {
                return sankeyDatas;
            }
            set
            {
                if (value != sankeyDatas)
                {
                    sankeyDatas = value;
                    RaisePropertyChanged(() => SankeyDatas);
                }
            }
        }

        private double sankeyLinkCurvature;
        public double SankeyLinkCurvature
        {
            get
            {
                return sankeyLinkCurvature;
            }
            set
            {
                if (value != sankeyLinkCurvature)
                {
                    sankeyLinkCurvature = value;
                    RaisePropertyChanged(() => SankeyLinkCurvature);
                }
            }
        }

        private Dictionary<string, Brush> sankeyNodeBrushes;
        public Dictionary<string, Brush> SankeyNodeBrushes
        {
            get
            {
                return sankeyNodeBrushes;
            }
            set
            {
                if(value != sankeyNodeBrushes)
                {
                    sankeyNodeBrushes = value;
                    RaisePropertyChanged(() => SankeyNodeBrushes);
                }
            }
        }

        private string highlightSankeyNode;
        public string HighlightSankeyNode
        {
            get
            {
                return highlightSankeyNode;
            }
            set
            {
                highlightSankeyNode = value;
                RaisePropertyChanged(() => HighlightSankeyNode);
            }
        }

        private SankeyLinkFinder highlightSankeyLink;
        public SankeyLinkFinder HighlightSankeyLink
        {
            get
            {
                return highlightSankeyLink;
            }
            set
            {
                if(value != highlightSankeyLink)
                {
                    highlightSankeyLink = value;
                    RaisePropertyChanged(() => HighlightSankeyLink);
                }
            }
        }

        private FlowDirection sankeyFlowDirection;
        public FlowDirection SankeyFlowDirection
        {
            get
            {
                return sankeyFlowDirection;
            }
            set
            {
                if(value != sankeyFlowDirection)
                {
                    sankeyFlowDirection = value;
                    RaisePropertyChanged(() => SankeyFlowDirection);
                }
            }
        }

        private bool sankeyShowLabels;
        public bool SankeyShowLabels
        {
            get
            {
                return sankeyShowLabels;
            }
            set
            {
                if (value != sankeyShowLabels)
                {
                    sankeyShowLabels = value;
                    RaisePropertyChanged(() => SankeyShowLabels);
                }
            }
        }

        private HighlightMode sankeyHighlightMode;
        public HighlightMode SankeyHighlightMode
        {
            get
            {
                return sankeyHighlightMode;
            }
            set
            {
                if (value != sankeyHighlightMode)
                {
                    sankeyHighlightMode = value;
                    RaisePropertyChanged(() => SankeyHighlightMode);
                }
            }
        }

        private Random random;

        #endregion

        #region INotifyPropertyChanged & ICommand

        private ICommand GetCommand(ICommand c, CommandBase command)
        {
            if (c == null)
            {
                c = command;
            }

            return c;
        }

        private void RaisePropertyChanged<T>(Expression<Func<T>> action)
        {
            var propertyName = GetPropertyName(action);
            RaisePropertyChanged(propertyName);
        }

        private static string GetPropertyName<T>(Expression<Func<T>> action)
        {
            var expression = (MemberExpression)action.Body;
            var propertyName = expression.Member.Name;
            return propertyName;
        }

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
