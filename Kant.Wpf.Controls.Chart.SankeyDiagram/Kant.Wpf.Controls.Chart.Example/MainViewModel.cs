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
                new SankeyDataRow("A", "C", 2555),
                new SankeyDataRow("A", "D", 3555),
                new SankeyDataRow("A", "E", 1555),
                new SankeyDataRow("B", "C", 1555),
                new SankeyDataRow("B", "D", 2555),
                new SankeyDataRow("B", "E", 1555),
                new SankeyDataRow("C", "F", 2555),
                new SankeyDataRow("C", "G", 1555),
                new SankeyDataRow("C", "H", 1555),
                new SankeyDataRow("D", "F", 2555),
                new SankeyDataRow("D", "G", 1555),
                new SankeyDataRow("D", "H", 1555),
                new SankeyDataRow("D", "I", 1555),
                new SankeyDataRow("E", "H", 1555),
                new SankeyDataRow("E", "I", 1555)
            };

            //SankeyNodeBrushes = new Dictionary<string, Brush>()
            //{
            //    { "A", new SolidColorBrush(Colors.Brown) { Opacity = 0.35 } },
            //    { "B", new SolidColorBrush(Colors.Aqua) { Opacity = 0.25 } },
            //    { "C", new SolidColorBrush(Colors.CornflowerBlue) { Opacity = 0.15 } },
            //    { "D", new SolidColorBrush(Colors.DimGray) { Opacity = 0.45 } },
            //    { "E", new SolidColorBrush(Colors.Firebrick) { Opacity = 0.65 } },
            //};

            SankeyDatas = datas;
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
                    SankeyFlowDirection = random.Next(2) == 1 ? SankeyFlowDirection.TopToBottom : SankeyFlowDirection.LeftToRight;
                    SankeyShowLabels = random.Next(2) == 1 ? false : true;
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

        private SankeyFlowDirection sankeyFlowDirection;
        public SankeyFlowDirection SankeyFlowDirection
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
