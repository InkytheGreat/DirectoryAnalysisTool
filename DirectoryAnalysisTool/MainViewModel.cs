using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management;
using DirectoryAnalysisTool.Helpers;
using DirectoryAnalysisTool.Models;
using System.IO.Abstractions;

namespace DirectoryAnalysisTool
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Private Fields & Constants

        private string _currentPath;
        private double _currentCanvasWidth = 800;
        private double _currentCanvasHeight = 600;

        // Color palette for directories by depth
        private static readonly List<Brush> _directoryPalette = new List<Brush>
        {
            Brushes.DarkSlateGray, // Depth 0 (Root)
            Brushes.Indigo,        // Depth 1
            Brushes.SeaGreen,      // Depth 2
            Brushes.Chocolate,     // Depth 3
            Brushes.DeepPink,      // Depth 4
            Brushes.Olive          // Depth 5
        };

        #endregion

        #region Properties (Data Binding)

        public ObservableCollection<VisualNode> VisualNodes { get; set; }
        public ObservableCollection<DirectoryNode> RootDirectories { get; set; }

        public string CurrentPath
        {
            get => _currentPath;
            set
            {
                _currentPath = value;
                OnPropertyChanged(nameof(CurrentPath));
            }
        }

        #endregion

        #region Commands

        public ICommand SelectFolderCommand { get; }
        public ICommand OpenInExplorerCommand { get; }
        public ICommand SelectNodeCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand NavigateUpCommand { get; }

        private readonly IFileSystem _fileSystem;

        #endregion

        public MainViewModel(IFileSystem fileSystem = null)
        {

            _fileSystem = fileSystem ?? new FileSystem();
            VisualNodes = new ObservableCollection<VisualNode>();
            RootDirectories = new ObservableCollection<DirectoryNode>();

            // Command Initialization
            SelectFolderCommand = new RelayCommand(ExecuteSelectFolder);
            OpenInExplorerCommand = new RelayCommand(ExecuteOpenInExplorer);
            SelectNodeCommand = new RelayCommand(ExecuteSelectNode);
            ZoomInCommand = new RelayCommand(ExecuteZoomIn);
            NavigateUpCommand = new RelayCommand(ExecuteNavigateUp);
        }

        #region Command Execution Methods

        /// <summary>
        /// Opens a folder dialog and starts the analysis of the chosen path.
        /// </summary>
        private async void ExecuteSelectFolder(object parameter)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select a folder to analyze";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    await AnalyzePathAsync(dialog.SelectedPath);
                }
            }
        }

        /// <summary>
        /// Drills down into a specific folder and makes it the new root.
        /// </summary>
        private async void ExecuteZoomIn(object parameter)
        {
            if (parameter is string path && Directory.Exists(path))
            {
                await AnalyzePathAsync(path);
            }
        }

        /// <summary>
        /// Moves the analysis to the parent folder of the current path.
        /// </summary>
        private async void ExecuteNavigateUp(object parameter)
        {
            if (string.IsNullOrEmpty(CurrentPath)) return;

            try
            {
                var parentDir = Directory.GetParent(CurrentPath);
                if (parentDir != null && parentDir.Exists)
                {
                    await AnalyzePathAsync(parentDir.FullName);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Could not navigate up: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles clicking a node in the Treemap or the TreeView.
        /// </summary>
        private void ExecuteSelectNode(object parameter)
        {
            if (parameter is string path)
            {
                HighlightNodeByPath(path);
                ExpandAndSelectInTree(path, RootDirectories);
            }
        }

        /// <summary>
        /// Opens the file or folder in Windows Explorer.
        /// </summary>
        private void ExecuteOpenInExplorer(object parameter)
        {
            if (parameter is string path && !string.IsNullOrEmpty(path))
            {
                try
                {
                    if (File.Exists(path))
                        Process.Start("explorer.exe", $"/select,\"{path}\"");
                    else if (Directory.Exists(path))
                        Process.Start("explorer.exe", $"\"{path}\"");
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Explorer Error: {ex.Message}");
                }
            }
        }

        #endregion

        #region Core Logic: Scanning & Analysis

        /// <summary>
        /// The main orchestrator: Scans the drive, calculates sizes, and builds the UI nodes.
        /// </summary>
        public async Task AnalyzePathAsync(string targetPath)
        {
            CurrentPath = targetPath;
            DirectoryNode rootNode = null;

            await Task.Run(() =>
            {
                // SSDs handle parallel scans well, HDDs do not.
                bool isSSD = CheckIfDriveIsSSD(targetPath);
                rootNode = BuildTreeFromPath(_fileSystem.DirectoryInfo.New(CurrentPath), isSSD);

                if (rootNode != null && rootNode.TotalSize > 0)
                {
                    FileUtility.CalculatePercentages(rootNode, rootNode.TotalSize);
                }
            });

            if (rootNode != null)
            {
                VisualNodes.Clear();
                RootDirectories.Clear();
                RootDirectories.Add(rootNode);
                BuildVisualization(rootNode);
            }
        }

        /// <summary>
        /// Recursively crawls the file system.
        /// </summary>
        private DirectoryNode BuildTreeFromPath(IDirectoryInfo dirInfo, bool isSolidState)
        {
            var node = new DirectoryNode { Name = dirInfo.Name, FullPath = dirInfo.FullName };
            long localTotalSize = 0;

            try
            {
                // Add Files
                foreach (var file in dirInfo.EnumerateFiles())
                {
                    node.Files.Add(new FileNode { Name = file.Name, Size = file.Length, FullPath = file.FullName });
                    localTotalSize += file.Length;
                }

                // Add Subdirectories (Multi-threaded if SSD, Sequential if HDD)
                if (isSolidState)
                {
                    var subNodes = new ConcurrentBag<DirectoryNode>();
                    Parallel.ForEach(dirInfo.EnumerateDirectories(), subDir =>
                    {
                        var childNode = BuildTreeFromPath(subDir, isSolidState);
                        if (childNode != null) subNodes.Add(childNode);
                    });

                    foreach (var child in subNodes)
                    {
                        node.Subdirectories.Add(child);
                        localTotalSize += child.TotalSize;
                    }
                }
                else
                {
                    foreach (var subDir in dirInfo.EnumerateDirectories())
                    {
                        var childNode = BuildTreeFromPath(subDir, isSolidState);
                        if (childNode != null)
                        {
                            node.Subdirectories.Add(childNode);
                            localTotalSize += childNode.TotalSize;
                        }
                    }
                }

                node.TotalSize = localTotalSize;
            }
            catch (UnauthorizedAccessException) { return null; }
            catch (Exception) { return null; }

            return node;
        }

        #endregion

        #region Treemap Rendering Logic

        /// <summary>
        /// Entry point for generating the visual blocks based on current canvas size.
        /// </summary>
        public void BuildVisualization(DirectoryNode rootNode)
        {
            VisualNodes.Clear();
            AddVisualNode(rootNode, 0, 0, _currentCanvasWidth, _currentCanvasHeight, 0);
        }

        /// <summary>
        /// Recursive Treemap algorithm.
        /// </summary>
        private void AddVisualNode(DirectoryNode node, double x, double y, double width, double height, int depth)
        {
            // Culling: Don't render if too small to see
            if (width < 2 || height < 2 || node.TotalSize == 0) return;

            // Add the directory block
            VisualNodes.Add(new VisualNode
            {
                Name = node.Name,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Color = _directoryPalette[depth % _directoryPalette.Count],
                FullPath = node.FullPath,
                IsDirectory = true
            });

            // Setup Internal Padding
            double headerHeight = 22;
            double padding = 12;
            double innerX = x + padding;
            double innerY = y + headerHeight;
            double innerWidth = width - (padding * 2);
            double innerHeight = height - (headerHeight + padding);

            if (innerWidth < 1 || innerHeight < 1) return;

            // Sort children for better spatial packing
            var children = new List<(bool IsDir, long Size, object Item)>();
            foreach (var file in node.Files) children.Add((false, file.Size, file));
            foreach (var sub in node.Subdirectories) children.Add((true, sub.TotalSize, sub));
            children.Sort((a, b) => b.Size.CompareTo(a.Size));

            // Layout Loop
            double currentX = innerX, currentY = innerY;
            double remW = innerWidth, remH = innerHeight;
            double remSize = node.TotalSize;

            foreach (var child in children)
            {
                if (remSize <= 0) break;

                double ratio = (double)child.Size / remSize;
                double cW, cH;

                // Split along the longest axis
                if (remW > remH) { cW = remW * ratio; cH = remH; }
                else { cW = remW; cH = remH * ratio; }

                if (cW >= 1.0 && cH >= 1.0)
                {
                    if (child.IsDir)
                        AddVisualNode((DirectoryNode)child.Item, currentX, currentY, cW, cH, depth + 1);
                    else
                        VisualNodes.Add(new VisualNode
                        {
                            Name = ((FileNode)child.Item).Name,
                            X = currentX,
                            Y = currentY,
                            Width = cW,
                            Height = cH,
                            Color = Brushes.SteelBlue,
                            FullPath = ((FileNode)child.Item).FullPath,
                            IsDirectory = false
                        });
                }

                // Advance coordinates
                if (remW > remH) { currentX += cW; remW -= cW; }
                else { currentY += cH; remH -= cH; }
                remSize -= child.Size;
            }
        }

        /// <summary>
        /// Redraws the treemap when the window is resized.
        /// </summary>
        public void UpdateVisualizationSize(double width, double height)
        {
            _currentCanvasWidth = width;
            _currentCanvasHeight = height;

            if (RootDirectories != null && RootDirectories.Count > 0)
            {
                BuildVisualization(RootDirectories[0]);
            }
        }

        #endregion

        #region Helpers & UI Sync

        /// <summary>
        /// Visually highlights a specific path in the treemap by changing its border.
        /// </summary>
        public void HighlightNodeByPath(string targetPath)
        {
            foreach (var node in VisualNodes)
            {
                bool isTarget = node.FullPath == targetPath;
                node.NodeBorderBrush = isTarget ? Brushes.Gold : Brushes.Black;
                node.NodeBorderThickness = isTarget ? 3.0 : 1.0;
            }
        }

        /// <summary>
        /// Recursively finds a node in the TreeView, selects it, and expands its parents.
        /// </summary>
        private bool ExpandAndSelectInTree(string targetPath, IEnumerable<DirectoryNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.FullPath == targetPath)
                {
                    node.IsSelected = true;
                    return true;
                }

                if (ExpandAndSelectInTree(targetPath, node.Subdirectories))
                {
                    node.IsExpanded = true;
                    return true;
                }
            }
            return false;
        }

        private bool CheckIfDriveIsSSD(string targetPath)
        {
            try
            {
                string driveLetter = Path.GetPathRoot(targetPath).TrimEnd('\\');
                string query = $@"SELECT MediaType FROM MSFT_PhysicalDisk WHERE DeviceId IN (SELECT DiskNumber FROM MSFT_Partition WHERE DriveLetter = '{driveLetter.Replace(":", "")}')";

                using (var searcher = new ManagementObjectSearcher(@"\\.\Root\Microsoft\Windows\Storage", query))
                using (var results = searcher.Get())
                {
                    foreach (var item in results)
                    {
                        return Convert.ToInt32(item["MediaType"]) == 4; // 4 = SSD
                    }
                }
            }
            catch { return false; }
            return false;
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

}