using DirectoryAnalysisTool.Models;

namespace DirectoryAnalysisTool.Helpers
{
    public static class FileUtility
    {
        public static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = (decimal)bytes;

            while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
            {
                number /= 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }

        public static void CalculatePercentages(DirectoryNode node, long rootTotalSize)
        {
            if (rootTotalSize == 0) return;
            node.Percentage = ((double)node.TotalSize / rootTotalSize) * 100;
            node.FormattedSize = FormatBytes(node.TotalSize);

            foreach (var child in node.Subdirectories)
                CalculatePercentages(child, rootTotalSize);
        }
    }
}