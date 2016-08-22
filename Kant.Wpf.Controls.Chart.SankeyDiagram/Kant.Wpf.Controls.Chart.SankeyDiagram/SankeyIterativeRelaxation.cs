using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kant.Wpf.Controls.Chart
{
    public static class SankeyIterativeRelaxation
    {
        public static Dictionary<int, List<SankeyNode>> Calculate(FlowDirection flowDirection, Dictionary<int, List<SankeyNode>> nodes, List<SankeyLink> links, double panelLength, double nodeGap, double unitLength, int iterations)
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

        private static Dictionary<int, List<SankeyNode>> InitializeNodeLength(Dictionary<int, List<SankeyNode>> nodes, double unitLength, FlowDirection flowDirection)
        {
            foreach (var levelNodes in nodes.Values)
            {
                var index = 0;

                foreach (var node in levelNodes)
                {
                    if (flowDirection == FlowDirection.TopToBottom)
                    {
                        node.Shape.Width *= unitLength;
                        node.X = index;
                    }
                    else
                    {
                        node.Shape.Height *= unitLength;
                        node.Y = index;
                    }

                    index++;
                }
            }

            return nodes;
        }

        private static Dictionary<int, List<SankeyNode>> ResolveCollisions(Dictionary<int, List<SankeyNode>> nodes, double panelLength, double nodeGap, FlowDirection flowDirection)
        {
            foreach (var levelNodes in nodes.Values)
            {
                var tempValue1 = 0.0;
                var tempValue2 = 0.0;

                if (flowDirection == FlowDirection.TopToBottom)
                {
                    levelNodes.Sort((n1, n2) => { return (int)(n1.X - n2.X); });
                }
                else
                {
                    levelNodes.Sort((n1, n2) => { return (int)(n1.Y - n2.Y); });
                }

                foreach (var node in levelNodes)
                {
                    if (flowDirection == FlowDirection.TopToBottom)
                    {
                        tempValue1 = tempValue2 - node.X;

                        if (tempValue1 > 0)
                        {
                            node.X += tempValue1;
                        }

                        tempValue2 = node.X + node.Shape.Width + nodeGap;
                    }
                    else
                    {
                        tempValue1 = tempValue2 - node.Y;

                        if (tempValue1 > 0)
                        {
                            node.Y += tempValue1;
                        }

                        tempValue2 = node.Y + node.Shape.Height + nodeGap;
                    }
                }

                // if the last node goes outside the panel, push it back up
                tempValue1 = tempValue2 - nodeGap - panelLength;

                if (tempValue1 > 0)
                {
                    if (flowDirection == FlowDirection.TopToBottom)
                    {
                        tempValue2 = levelNodes.Last().X -= tempValue1;
                    }
                    else
                    {
                        tempValue2 = levelNodes.Last().Y -= tempValue1;
                    }

                    for (var index = levelNodes.Count - 2; index >= 0; index--)
                    {
                        var node = levelNodes[index];

                        if (flowDirection == FlowDirection.TopToBottom)
                        {
                            tempValue1 = node.X + node.Shape.Width + nodeGap - tempValue2;

                            if (tempValue1 > 0)
                            {
                                node.X -= tempValue1;
                            }

                            tempValue2 = node.X;
                        }
                        else
                        {
                            tempValue1 = node.Y + node.Shape.Height + nodeGap - tempValue2;

                            if(tempValue1 > 0)
                            {
                                node.Y -= tempValue1;
                            }

                            tempValue2 = node.Y;
                        }
                    }
                }
            }

            return nodes;
        }

        private static Dictionary<int, List<SankeyNode>> RelaxFromFrontToEnd(Dictionary<int, List<SankeyNode>> nodes, double alpha, FlowDirection flowDirection)
        {
            foreach (var levelNodes in nodes.Values)
            {
                foreach (var node in levelNodes)
                {
                    if (node.InLinks.Count > 0)
                    {
                        var tempValue = node.InLinks.Sum(link => GetCenterValue(link.FromNode, flowDirection) * link.Weight) / node.InLinks.Sum(link => link.Weight);

                        if (flowDirection == FlowDirection.TopToBottom)
                        {
                            node.X += (tempValue - GetCenterValue(node, flowDirection)) * alpha;
                        }
                        else
                        {
                            node.Y += (tempValue - GetCenterValue(node, flowDirection)) * alpha;
                        }
                    }
                }
            }

            return nodes;
        }

        private static Dictionary<int, List<SankeyNode>> RelaxFromEndToFront(Dictionary<int, List<SankeyNode>> nodes, double alpha, FlowDirection flowDirection)
        {
            for (var index = nodes.Count - 1; index >= 0; index--)
            {
                foreach (var node in nodes[index])
                {
                    if (node.OutLinks.Count > 0)
                    {
                        var tempValue = node.OutLinks.Sum(link => GetCenterValue(link.ToNode, flowDirection) * link.Weight) / node.OutLinks.Sum(link => link.Weight);

                        if (flowDirection == FlowDirection.TopToBottom)
                        {
                            node.X += (tempValue - GetCenterValue(node, flowDirection)) * alpha;
                        }
                        else
                        {
                            node.Y += (tempValue - GetCenterValue(node, flowDirection)) * alpha;
                        }
                    }
                }
            }

            return nodes;
        }

        private static double GetCenterValue(SankeyNode node, FlowDirection flowDirection)
        {
            return flowDirection == FlowDirection.TopToBottom ? node.X + node.Shape.Width / 2 : node.Y + node.Shape.Height / 2;
        }
    }
}
