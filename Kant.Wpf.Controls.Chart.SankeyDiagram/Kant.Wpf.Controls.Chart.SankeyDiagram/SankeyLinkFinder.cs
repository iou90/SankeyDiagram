using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kant.Wpf.Controls.Chart
{
    public class SankeyLinkFinder
    {
        public SankeyLinkFinder()
        {
        }

        public SankeyLinkFinder(string from, string to)
        {
            if(string.IsNullOrEmpty(from))
            {
                throw new ArgumentException("from node of the link is null");
            }

            if (string.IsNullOrEmpty(to))
            {
                throw new ArgumentException("to node of the link is null");
            }

            From = from;
            To = to;
        }

        public string From { get; private set; }

        public string To { get; private set; }
    }
}
