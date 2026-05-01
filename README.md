# Directory Analysis Tool
### Disk Usage Treemap and Tree Viewer

The Directory Analysis Tool is a C#/.NET Windows desktop application designed to solve the problem of visualizing massive hierarchical datasets. By mimicking the core functionality of tools like WinDirStat or TreeSize, this project showcases an application of N Trees, recursive algorithms, and asynchronous hardware optimization.

---

## Problem Description
Modern storage devices can contain millions of files. Visualizing this data presents three major challenges:
1.  **Traversal Efficiency:** Crawling a disk is an I/O bound task that can easily freeze a UI or crash a system if not handled correctly.
2.  **Spatial Mapping:** Representing a tree structure on a 2D canvas requires a recursive algorithm to ensure that the area of each box proportionally matches the file size.
3.  **Hardware Awareness:** Spinning hard drives (HDD) and Solid State Drives (SSD) require different traversal strategies to reach optimal performance.

---

## Key Features
* **Recursive Disk Scanning:** Quickly calculates the size of folders by aggregating the weights of all leaf nodes (files) up the tree.
* **Dynamic Treemap Visualization:** An interactive GUI that provides a spatial view of your disk.
* **SSD/HDD Awareness:** Uses Windows Management Instrumentation (WMI) to detect drive types. It uses `Parallel.ForEach` for SSDs and sequential recursion for HDDs to optimize throughput.
* **Dual-View Synchronization:** Selecting a block in the Treemap instantly expands and scrolls to the corresponding node in the TreeView (and vice versa).
* **Drill-Down Navigation:** Zoom into any subdirectory to make it the new root, allowing for analysis of deep file paths.

---

## Technical Architecture

### The Core Data Structure: N Tree
The main datastructure of the application is the `DirectoryNode` class. Each node can have N children (subdirectories).
* **Nodes:** Represent directories.
* **Leaves:** Represent files (`FileNode`).
* **Weight Calculation:** Total size is calculated using a post-order traversal logic where a parent’s `TotalSize` is the sum of its files plus the `TotalSize` of all its children.

### The Treemap Algorithm
The visualization uses a recursive algorithm. For every directory:
1.  Sort children by size (descending).
2.  Determine the orientation (Vertical or Horizontal) based on the longest axis of the available space.
3.  Recursively sub-divide the canvas area based on the percentage of space each child occupies.

### MVVM Pattern
The project strictly follows the Model-View-ViewModel pattern:
* **View (MainWindow.xaml):** Defines the UI and data-bindings.
* **ViewModel (MainViewModel.cs):** Controls scanning logic, manages the `ObservableCollections`, and handles UI commands.
* **Model (DirectoryNode.cs):** Plain objects representing the file system structure.

### Classes & Methods
The system follows a strict MVVM (Model-View-ViewModel) architectural pattern to decouple the data processing logic from the rendering layer.


* **MainViewModel**: The central orchestrator. It contains the AnalyzePathAsync method, which initiates the background thread for disk scanning. It also houses the AddVisualNode recursive method used to calculate Treemap coordinates.

* **DirectoryNode**: An N-ary tree structure where each node represents a directory. It stores metadata such as TotalSize and Percentage, and maintains a list of child DirectoryNodes and FileNodes.

* **VisualNode**: A simplified "flat" model used by the UI. It converts the abstract tree data into concrete geometric properties (X, Y, Width, Height, and Color) for the WPF Canvas.

---

## Tech Stack
* **Language:** C# 10.0+
* **Framework:** .NET 6+ (WPF)
* **System API:** System.IO (File Enumeration), System.Management (WMI Drive Detection)
* **Threading:** Task Parallel Library (TPL) for non-blocking UI scans.

---

## How to Compile into a Portable EXE
This application can be compiled into a single, portable executable that runs without requiring a .NET installation on the target machine:

```bash dotnet publish -r win-x64 -c Release --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true```

1. Open the terminal in the project directory.

2. Run the command above.

3. The portable app will be located in \bin\Release\netX.X\win-x64\publish\.

There is also a standalone portable version available in the releases section of the GitHub repository.

---

## Testing
This application was tested on my personal PC with multiple drives of various sizes and types.

* **Testing Hardware**: Tested on a 2TB NVMe SSD (Samsung 980 PRO 2TB) and a 8TB HDD (ST8000DM004-2CX188)

* **Methodology**: Using the precompiled exe (available in the Releases) I measured the performance by scanning the root of each drive

* **Results**: Scanning the whole NVMe SSD took about 8 seconds (1.73 TB used). Scanning a folder in the 8TB HDD (1.8 TB in the folder) took 3 seconds (faster than the built in Windows tool)

