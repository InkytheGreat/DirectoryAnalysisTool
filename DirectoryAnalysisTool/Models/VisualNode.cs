using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using System.Text;

namespace DirectoryAnalysisTool.Models
{
    public class VisualNode
    {
        
            public string Name { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public Brush Color { get; set; }
            public string FullPath { get; set; }
            public bool IsDirectory { get; set; }

            private Brush _nodeBorderBrush = Brushes.Black;
            public Brush NodeBorderBrush
            {
                get => _nodeBorderBrush;
                set { _nodeBorderBrush = value; OnPropertyChanged(nameof(NodeBorderBrush)); }
            }

            private double _nodeBorderThickness = 1.0;
            public double NodeBorderThickness
            {
                get => _nodeBorderThickness;
                set { _nodeBorderThickness = value; OnPropertyChanged(nameof(NodeBorderThickness)); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        
    }
}
