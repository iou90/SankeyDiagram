A powerful  and easy to use WPF library for drawing Sankey diagram.

##Usage

* in xaml　　　　　　　　　　　　　　　　`

```xml
<kantCharts:SankeyDiagram Height="355"
            NodeThickness="25"
            NodeGap="5"
            UseNodeLinksPalette="True"
            HighlightOpacity="0.95"
            LoweredOpacity="0.45"
            HighlightBrush="{StaticResource SankeyHighlightBrush}"
            LabelStyle="{StaticResource SankeyLabelStyle}"
            HighlightLabelStyle="{StaticResource SankeyHighlightLabelStyle}"
            Datas="{Binding SankeyDatas}"
            SankeyFlowDirection="{Binding SankeyFlowDirection}"
            ShowLabels="{Binding SankeyShowLabels}"
            HighlightMode="{Binding SankeyHighlightMode}"
            NodeBrushes="{Binding SankeyNodeBrushes}"
            HighlightNode="{Binding HighlightSankeyNode}"
            HighlightLink="{Binding HighlightSankeyLink}" />
```

* in view model

```c#
var datas = new List<SankeyDataRow>()
{
    new SankeyDataRow("A", "C", 255),
    new SankeyDataRow("A", "D", 355),
    new SankeyDataRow("B", "C", 555),
    new SankeyDataRow("B", "D", 255),
    new SankeyDataRow("B", "E", 1555),
    new SankeyDataRow("C", "H", 155),
    new SankeyDataRow("D", "F", 25),
    new SankeyDataRow("D", "G", 155),
    new SankeyDataRow("D", "H", 15),
    new SankeyDataRow("D", "I", 55),
    new SankeyDataRow("E", "H", 1555),
    new SankeyDataRow("B", "G", 255),
    new SankeyDataRow("A", "E", 95),
    new SankeyDataRow("E", "I", 1555),
    new SankeyDataRow("C", "G", 755),
    new SankeyDataRow("C", "F", 455),
};
```

* the beauty of data visualization
![sankey diagram](https://raw.githubusercontent.com/iou90/SankeyDiagram/master/demo_screenshot.png)



For style or other settings, please see https://github.com/iou90/SankeyDiagram/wiki/Supported-features

For upcoming features,  please see https://github.com/iou90/SankeyDiagram/wiki/What-features-are-being-developed-and-roadmap

Demo applications included in the source project.



Contact: iou90@outlook.com
