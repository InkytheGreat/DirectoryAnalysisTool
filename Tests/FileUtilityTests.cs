using Xunit;
using DirectoryAnalysisTool;
using DirectoryAnalysisTool.Helpers;
using DirectoryAnalysisTool.Models;

namespace DirectoryAnalysisTool.Tests
{
    public class FileUtilityTests
    {
        [Theory]
        [InlineData(0, "0.0 B")]
        [InlineData(1024, "1.0 KB")]
        [InlineData(1048576, "1.0 MB")]
        [InlineData(5368709120, "5.0 GB")]
        public void FormatBytes_ShouldReturnCorrectHumanReadableString(long bytes, string expected)
        {
            // Act
            var result = FileUtility.FormatBytes(bytes);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CalculatePercentages_ShouldSetCorrectValues()
        {
            // Arrange
            var root = new DirectoryNode { TotalSize = 100 };
            var child = new DirectoryNode { TotalSize = 25 };
            root.Subdirectories.Add(child);

            // Act
            FileUtility.CalculatePercentages(root, root.TotalSize);

            // Assert
            Assert.Equal(100.0, root.Percentage);
            Assert.Equal(25.0, child.Percentage);
        }
    }
}