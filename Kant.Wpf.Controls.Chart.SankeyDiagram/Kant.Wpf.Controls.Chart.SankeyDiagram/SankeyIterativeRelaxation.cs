using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kant.Wpf.Controls.Chart
{
    public class SankeyIterativeRelaxation
    {
        public Dictionary<int, List<SankeyNode>> Calculate(SankeyFlowDirection flowDirection, Dictionary<int, List<SankeyNode>> nodes, List<SankeyLink> links, double panelLength, double nodeGap, double unitLength, int iterations)
        {
            nodes = InitializeNodeLength(nodes, unitLength, flowDirection);
            nodes = ResolveCollisions(nodes, panelLength, nodeGap, flowDirection);
            var relaxationAlpha = 1.0;

            for (; iterations > 0; iterations--)
            {
                relaxationAlpha *= 0.99;
                nodes = RelaxFromEndToFront(nodes, relaxationAlpha, flowDirection);
                nodes = ResolveCollisions(nodes, panelLength, nodeGap, flowDirection);
                nodes = RelaxFromFrontToEnd(nodes, relaxationAlpha, flowDirection);
                nodes = ResolveCollisions(nodes, panelLength, nodeGap, flowDirection);
            }

            return nodes;
        }

        private Dictionary<int, List<SankeyNode>> InitializeNodeLength(Dictionary<int, List<SankeyNode>> nodes, double unitLength, SankeyFlowDirection flowDirection)
        {
            foreach (var levelNodes in nodes.Values)
            {
                var index = 0;

                foreach (var node in levelNodes)
                {
                    if (flowDirection == SankeyFlowDirection.TopToBottom)
                    {
                    }
                    else
                    {
                        node.Shape.Height *= unitLength;
                        node.CalculatingCoordinate = index;
                    }

                    index++;
                }

                //levelNodes.Sort((n1, n2) =>
                //{
                //    if (flowDirection == SankeyFlowDirection.TopToBottom)
                //    {
                //        return (int)(n1.Shape.Width - n2.Shape.Width);
                //    }
                //    else
                //    {
                //        return (int)(n1.Shape.Height - n2.Shape.Height);
                //    }
                //});

                //var index = 0;

                //foreach (var node in levelNodes)
                //{
                //    node.CalculatingCoordinate = index;
                //    index++;
                //}
            }

            return nodes;
        }

        private Dictionary<int, List<SankeyNode>> ResolveCollisions(Dictionary<int, List<SankeyNode>> nodes, double panelLength, double nodeGap, SankeyFlowDirection flowDirection)
        {
            foreach (var levelNodes in nodes.Values)
            {
                var tempValue1 = 0.0;
                var tempValue2 = 0.0;
                //var tempNodes = levelNodes.OrderBy(n => n.Shape.Height).ToList();

                levelNodes.Sort((n1, n2) =>
                {
                    return (int)(n1.CalculatingCoordinate - n2.CalculatingCoordinate);
                });

                foreach (var node in levelNodes)
                //foreach (var node in tempNodes)
                {
                    tempValue1 = tempValue2 - node.CalculatingCoordinate;

                    if (tempValue1 > 0)
                    {
                        node.CalculatingCoordinate += tempValue1;
                    }

                    if (flowDirection == SankeyFlowDirection.TopToBottom)
                    {

                    }
                    else
                    {
                        tempValue2 = node.CalculatingCoordinate + node.Shape.Height + nodeGap;
                    }
                }

                // if the last node goes outside the panel, push it back up
                tempValue1 = tempValue2 - nodeGap - panelLength;

                if (tempValue1 > 0)
                {
                    tempValue2 = levelNodes.Last().CalculatingCoordinate -= tempValue1;
                    //tempValue2 = tempNodes.Last().CalculatingCoordinate -= tempValue1;

                    for (var index = levelNodes.Count - 2; index >= 0; index--)
                    //for (var index = tempNodes.Count - 2; index >= 0; index--)
                    {
                        var node = levelNodes[index];
                        //var node = tempNodes[index];

                        if (flowDirection == SankeyFlowDirection.TopToBottom)
                        {

                        }
                        else
                        {
                            tempValue1 = node.CalculatingCoordinate + node.Shape.Height + nodeGap - tempValue2;

                            if(tempValue1 > 0)
                            {
                                node.CalculatingCoordinate -= tempValue1;
                            }

                            tempValue2 = node.CalculatingCoordinate;
                        }
                    }
                }
            }

            return nodes;
        }

        private Dictionary<int, List<SankeyNode>> RelaxFromFrontToEnd(Dictionary<int, List<SankeyNode>> nodes, double alpha, SankeyFlowDirection flowDirection)
        {
            foreach (var levelNodes in nodes.Values)
            {
                foreach (var node in levelNodes)
                {
                    if (node.InLinks.Count > 0)
                    {
                        var tempValue = node.InLinks.Sum(link => GetCenterValue(link.FromNode, flowDirection) * link.Weight) / node.InLinks.Sum(link => link.Weight);
                        node.CalculatingCoordinate += (tempValue - GetCenterValue(node, flowDirection)) * alpha;
                    }
                }
            }

            return nodes;
        }

        private Dictionary<int, List<SankeyNode>> RelaxFromEndToFront(Dictionary<int, List<SankeyNode>> nodes, double alpha, SankeyFlowDirection flowDirection)
        {
            //foreach (var levelNodes in nodes.Values)
            for (var index = nodes.Count - 1; index >= 0; index--)
            {
                //foreach(var node in levelNodes)
                foreach (var node in nodes[index])
                    //for (var lIndex = nodes[index].Count - 1; lIndex >= 0; lIndex--)
                    //for (var lIndex = levelNodes.Count - 1; lIndex >= 0; lIndex--)
                {
                    //var node = nodes[index][lIndex];
                    //var node = levelNodes[lIndex];

                    if (node.OutLinks.Count > 0)
                    {
                        var tempValue = node.OutLinks.Sum(link => GetCenterValue(link.ToNode, flowDirection) * link.Weight) / node.OutLinks.Sum(link => link.Weight);
                        node.CalculatingCoordinate += (tempValue - GetCenterValue(node, flowDirection)) * alpha;
                    }
                }
            }

            return nodes;
        }

        private double GetCenterValue(SankeyNode node, SankeyFlowDirection flowDirection)
        {
            if(flowDirection == SankeyFlowDirection.TopToBottom)
            {
                return 0;
            }
            else
            {
                return node.CalculatingCoordinate + node.Shape.Height / 2;
            }
        }
    }
}
