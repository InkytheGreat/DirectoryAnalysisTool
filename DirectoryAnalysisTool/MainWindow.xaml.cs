using System.Windows;
using System.Windows.Controls;
using DirectoryAnalysisTool.Models;

namespace DirectoryAnalysisTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Handles UI-specific events that cannot be easily managed via pure MVVM commands.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        #region Treemap Layout Management

        /// <summary>
        /// Updates the ViewModel with new canvas dimensions whenever the window or control is resized.
        /// This ensures the algorithm always fills the available space.
        /// </summary>
        private void TreemapControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm && e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                vm.UpdateVisualizationSize(e.NewSize.Width, e.NewSize.Height);
            }
        }

        #endregion

        #region TreeView Interaction logic

        /// <summary>
        /// Syncs the Treemap highlight with the user's selection in the sidebar TreeView.
        /// </summary>
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is MainViewModel vm && e.NewValue is DirectoryNode selectedNode)
            {
                vm.HighlightNodeByPath(selectedNode.FullPath);
            }
        }

        /// <summary>
        /// Automatically scrolls the TreeView to show the selected item.
        /// Triggered when the ViewModel programmatically selects a node (e.g., clicking a block in the Treemap).
        /// </summary>
        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem treeViewItem)
            {
                treeViewItem.BringIntoView();

                // Prevent the event from bubbling up, which would cause parent folders to scroll unnecessarily.
                e.Handled = true;
            }
        }

        #endregion
    }
}