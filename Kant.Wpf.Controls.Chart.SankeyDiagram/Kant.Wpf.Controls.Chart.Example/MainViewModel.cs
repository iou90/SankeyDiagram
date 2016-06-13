﻿using System;
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
        public MainViewModel()
        {
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

            SankeyNodeBrushes = new Dictionary<string, Brush>()
            {
                { "A", new SolidColorBrush(Colors.Brown) { Opacity = 0.35 } },
                { "B", new SolidColorBrush(Colors.Aqua) { Opacity = 0.25 } },
                { "C", new SolidColorBrush(Colors.CornflowerBlue) { Opacity = 0.15 } },
                { "D", new SolidColorBrush(Colors.DimGray) { Opacity = 0.45 } },
                { "E", new SolidColorBrush(Colors.Firebrick) { Opacity = 0.65 } },
            };

            SankeyDatas = datas;
        }

        private ICommand testBigData;
        public ICommand TestBigData
        {
            get
            {
                return GetCommand(testBigData, new CommandBase(() =>
                {
                    var datas = new List<SankeyDataRow>();
                    var randrom = new Random();
                    var count = 0;

                    while (count < 100)
                    {
                        datas.Add(new SankeyDataRow(randrom.Next(9).ToString(), randrom.Next(10, 19).ToString(), randrom.Next(55, 155)));
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