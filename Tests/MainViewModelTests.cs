using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using DirectoryAnalysisTool.Models;
using Xunit;

namespace DirectoryAnalysisTool.Tests
{
    public class MainViewModelTests
    {
        [Fact]
        public async Task AnalyzePathAsync_ShouldPopulateRootDirectories()
        {
            // Arrange: Create a fake file system
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\Work\Project\readme.txt", new MockFileData("Hello World") }, // 11 bytes
                { @"C:\Work\Project\data.bin", new MockFileData(new byte[100]) }    // 100 bytes
            });

            var viewModel = new MainViewModel(mockFileSystem);

            // Act
            await viewModel.AnalyzePathAsync(@"C:\Work\Project");

            // Assert
            Assert.Single(viewModel.RootDirectories);
            var root = viewModel.RootDirectories.First();
            Assert.Equal(111, root.TotalSize); // 100 + 11
            Assert.Equal(2, root.Files.Count);
        }

        [Fact]
        public void HighlightNodeByPath_ShouldSetGoldBorder()
        {
            // Arrange
            var vm = new MainViewModel();
            var targetPath = @"C:\Test";
            vm.VisualNodes.Add(new VisualNode { FullPath = targetPath });
            vm.VisualNodes.Add(new VisualNode { FullPath = @"C:\Other" });

            // Act
            vm.HighlightNodeByPath(targetPath);

            // Assert
            var highlighted = vm.VisualNodes.First(n => n.FullPath == targetPath);
            Assert.Equal(System.Windows.Media.Brushes.Gold, highlighted.NodeBorderBrush);
            Assert.Equal(3.0, highlighted.NodeBorderThickness);
        }
    }
}