using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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

            SankeyDatas = datas;
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
                if(sankeyDatas != value)
                {
                    sankeyDatas = value;
                    RaisePropertyChanged(() => SankeyDatas);
                }
            }
        }

        #region INotifyPropertyChanged & Command

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
