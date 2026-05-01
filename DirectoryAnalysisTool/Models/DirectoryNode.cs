using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DirectoryAnalysisTool.Models
{
    public class DirectoryNode
    {
            public string Name { get; set; }
            public string FullPath { get; set; }
            public long TotalSize { get; set; }
            public double Percentage { get; set; }
            public string FormattedSize { get; set; }
            public List<FileNode> Files { get; set; } = new List<FileNode>();
            public List<DirectoryNode> Subdirectories { get; set; } = new List<DirectoryNode>();

            private bool _isExpanded;
            public bool IsExpanded
            {
                get => _isExpanded;
                set { _isExpanded = value; OnPropertyChanged(nameof(IsExpanded)); }
            }

            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
