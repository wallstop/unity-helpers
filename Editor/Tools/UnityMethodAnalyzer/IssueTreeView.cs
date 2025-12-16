namespace WallstopStudios.UnityHelpers.Editor.Tools.UnityMethodAnalyzer
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.IMGUI.Controls;
    using UnityEngine;

    /// <summary>
    /// TreeView item for displaying analyzer issues.
    /// </summary>
    internal sealed class IssueTreeViewItem : TreeViewItem
    {
        public AnalyzerIssue Issue { get; }
        public string FilePath { get; }
        public int LineNumber { get; }
        public bool IsFile { get; }
        public bool IsCategory { get; }
        public IssueSeverity? Severity { get; }
        public int IssueCount { get; set; }
        public int CriticalCount { get; set; }
        public int HighCount { get; set; }

        public IssueTreeViewItem(
            int id,
            int depth,
            string displayName,
            AnalyzerIssue issue = null,
            string filePath = null,
            int lineNumber = 0,
            bool isFile = false,
            bool isCategory = false,
            IssueSeverity? severity = null
        )
            : base(id, depth, displayName)
        {
            Issue = issue;
            FilePath = filePath;
            LineNumber = lineNumber;
            IsFile = isFile;
            IsCategory = isCategory;
            Severity = severity;
        }
    }

    /// <summary>
    /// TreeView for displaying analyzer issues in a hierarchical format.
    /// </summary>
    internal sealed class IssueTreeView : TreeView
    {
        private IReadOnlyList<AnalyzerIssue> _issues;
        private string _rootPath;
        private bool _groupByFile = true;
        private bool _groupBySeverity;
        private bool _groupByCategory;
        private IssueSeverity? _severityFilter;
        private IssueCategory? _categoryFilter;
        private string _searchFilter;

        public event Action<AnalyzerIssue> OnIssueSelected;
        public event Action<string, int> OnOpenFile;
        public event Action<string> OnRevealInExplorer;

        public IssueTreeView(TreeViewState state)
            : base(state)
        {
            _issues = Array.Empty<AnalyzerIssue>();
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            Reload();
        }

        public void SetIssues(IReadOnlyList<AnalyzerIssue> issues, string rootPath)
        {
            _issues = issues ?? Array.Empty<AnalyzerIssue>();
            _rootPath = rootPath;
            Reload();
        }

        public void SetGrouping(bool byFile, bool bySeverity, bool byCategory)
        {
            _groupByFile = byFile;
            _groupBySeverity = bySeverity;
            _groupByCategory = byCategory;
            Reload();
        }

        public void SetFilters(IssueSeverity? severity, IssueCategory? category, string search)
        {
            _severityFilter = severity;
            _categoryFilter = category;
            _searchFilter = search;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem root = new(-1, -1, "Root");

            if (_issues == null || _issues.Count == 0)
            {
                root.AddChild(new TreeViewItem(0, 0, "No issues found"));
                return root;
            }

            IEnumerable<AnalyzerIssue> filteredIssues = _issues;

            if (_severityFilter.HasValue)
            {
                filteredIssues = filteredIssues.Where(i => i.Severity == _severityFilter.Value);
            }

            if (_categoryFilter.HasValue)
            {
                filteredIssues = filteredIssues.Where(i => i.Category == _categoryFilter.Value);
            }

            if (!string.IsNullOrWhiteSpace(_searchFilter))
            {
                string search = _searchFilter.ToLowerInvariant();
                filteredIssues = filteredIssues.Where(i =>
                    i.ClassName.ToLowerInvariant().Contains(search)
                    || i.MethodName.ToLowerInvariant().Contains(search)
                    || i.IssueType.ToLowerInvariant().Contains(search)
                    || i.FilePath.ToLowerInvariant().Contains(search)
                    || i.Description.ToLowerInvariant().Contains(search)
                );
            }

            List<AnalyzerIssue> issueList = filteredIssues.ToList();

            if (issueList.Count == 0)
            {
                root.AddChild(new TreeViewItem(0, 0, "No issues match the current filters"));
                return root;
            }

            int id = 1;

            if (_groupBySeverity)
            {
                BuildTreeBySeverity(root, issueList, ref id);
            }
            else if (_groupByCategory)
            {
                BuildTreeByCategory(root, issueList, ref id);
            }
            else if (_groupByFile)
            {
                BuildTreeByFile(root, issueList, ref id);
            }
            else
            {
                BuildFlatTree(root, issueList, ref id);
            }

            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        private void BuildTreeBySeverity(TreeViewItem root, List<AnalyzerIssue> issues, ref int id)
        {
            IOrderedEnumerable<IGrouping<IssueSeverity, AnalyzerIssue>> groups = issues
                .GroupBy(i => i.Severity)
                .OrderBy(g => (int)g.Key);

            foreach (IGrouping<IssueSeverity, AnalyzerIssue> group in groups)
            {
                List<AnalyzerIssue> groupIssues = group.ToList();
                string severityName = GetSeverityDisplayName(group.Key);
                IssueTreeViewItem severityItem = new(
                    id++,
                    0,
                    $"{severityName} ({groupIssues.Count})",
                    isCategory: true,
                    severity: group.Key
                )
                {
                    IssueCount = groupIssues.Count,
                };

                IOrderedEnumerable<IGrouping<string, AnalyzerIssue>> fileGroups = groupIssues
                    .GroupBy(i => i.FilePath)
                    .OrderBy(g => g.Key);

                foreach (IGrouping<string, AnalyzerIssue> fileGroup in fileGroups)
                {
                    List<AnalyzerIssue> fileIssues = fileGroup.ToList();
                    IssueTreeViewItem fileItem = new(
                        id++,
                        1,
                        $"{fileGroup.Key} ({fileIssues.Count})",
                        filePath: fileGroup.Key,
                        isFile: true
                    )
                    {
                        IssueCount = fileIssues.Count,
                        CriticalCount = fileIssues.Count(i => i.Severity == IssueSeverity.Critical),
                        HighCount = fileIssues.Count(i => i.Severity == IssueSeverity.High),
                    };

                    foreach (AnalyzerIssue issue in fileIssues.OrderBy(i => i.LineNumber))
                    {
                        string display = FormatIssueDisplay(issue);
                        IssueTreeViewItem issueItem = new(
                            id++,
                            2,
                            display,
                            issue,
                            issue.FilePath,
                            issue.LineNumber,
                            severity: issue.Severity
                        );
                        fileItem.AddChild(issueItem);
                    }

                    severityItem.AddChild(fileItem);
                }

                root.AddChild(severityItem);
            }
        }

        private void BuildTreeByCategory(TreeViewItem root, List<AnalyzerIssue> issues, ref int id)
        {
            IOrderedEnumerable<IGrouping<IssueCategory, AnalyzerIssue>> groups = issues
                .GroupBy(i => i.Category)
                .OrderBy(g => (int)g.Key);

            foreach (IGrouping<IssueCategory, AnalyzerIssue> group in groups)
            {
                List<AnalyzerIssue> groupIssues = group.ToList();
                string categoryName = GetCategoryDisplayName(group.Key);
                IssueTreeViewItem categoryItem = new(
                    id++,
                    0,
                    $"{categoryName} ({groupIssues.Count})",
                    isCategory: true
                )
                {
                    IssueCount = groupIssues.Count,
                    CriticalCount = groupIssues.Count(i => i.Severity == IssueSeverity.Critical),
                    HighCount = groupIssues.Count(i => i.Severity == IssueSeverity.High),
                };

                IOrderedEnumerable<IGrouping<string, AnalyzerIssue>> fileGroups = groupIssues
                    .GroupBy(i => i.FilePath)
                    .OrderBy(g => g.Key);

                foreach (IGrouping<string, AnalyzerIssue> fileGroup in fileGroups)
                {
                    List<AnalyzerIssue> fileIssues = fileGroup.ToList();
                    IssueTreeViewItem fileItem = new(
                        id++,
                        1,
                        $"{fileGroup.Key} ({fileIssues.Count})",
                        filePath: fileGroup.Key,
                        isFile: true
                    )
                    {
                        IssueCount = fileIssues.Count,
                        CriticalCount = fileIssues.Count(i => i.Severity == IssueSeverity.Critical),
                        HighCount = fileIssues.Count(i => i.Severity == IssueSeverity.High),
                    };

                    foreach (AnalyzerIssue issue in fileIssues.OrderBy(i => i.LineNumber))
                    {
                        string display = FormatIssueDisplay(issue);
                        IssueTreeViewItem issueItem = new(
                            id++,
                            2,
                            display,
                            issue,
                            issue.FilePath,
                            issue.LineNumber,
                            severity: issue.Severity
                        );
                        fileItem.AddChild(issueItem);
                    }

                    categoryItem.AddChild(fileItem);
                }

                root.AddChild(categoryItem);
            }
        }

        private void BuildTreeByFile(TreeViewItem root, List<AnalyzerIssue> issues, ref int id)
        {
            IOrderedEnumerable<IGrouping<string, AnalyzerIssue>> fileGroups = issues
                .GroupBy(i => i.FilePath)
                .OrderByDescending(g => g.Count(i => i.Severity == IssueSeverity.Critical))
                .ThenByDescending(g => g.Count(i => i.Severity == IssueSeverity.High))
                .ThenBy(g => g.Key);

            foreach (IGrouping<string, AnalyzerIssue> fileGroup in fileGroups)
            {
                List<AnalyzerIssue> fileIssues = fileGroup.ToList();
                int criticalCount = fileIssues.Count(i => i.Severity == IssueSeverity.Critical);
                int highCount = fileIssues.Count(i => i.Severity == IssueSeverity.High);

                string prefix =
                    criticalCount > 0 ? "🔴 "
                    : highCount > 0 ? "🟠 "
                    : "🟡 ";

                IssueTreeViewItem fileItem = new(
                    id++,
                    0,
                    $"{prefix}{fileGroup.Key} ({fileIssues.Count})",
                    filePath: fileGroup.Key,
                    isFile: true
                )
                {
                    IssueCount = fileIssues.Count,
                    CriticalCount = criticalCount,
                    HighCount = highCount,
                };

                foreach (AnalyzerIssue issue in fileIssues.OrderBy(i => i.LineNumber))
                {
                    string display = FormatIssueDisplay(issue);
                    IssueTreeViewItem issueItem = new(
                        id++,
                        1,
                        display,
                        issue,
                        issue.FilePath,
                        issue.LineNumber,
                        severity: issue.Severity
                    );
                    fileItem.AddChild(issueItem);
                }

                root.AddChild(fileItem);
            }
        }

        private void BuildFlatTree(TreeViewItem root, List<AnalyzerIssue> issues, ref int id)
        {
            IOrderedEnumerable<AnalyzerIssue> sortedIssues = issues
                .OrderBy(i => (int)i.Severity)
                .ThenBy(i => i.FilePath)
                .ThenBy(i => i.LineNumber);

            foreach (AnalyzerIssue issue in sortedIssues)
            {
                string display =
                    $"{FormatIssueDisplay(issue)} - {issue.FilePath}:{issue.LineNumber}";
                IssueTreeViewItem issueItem = new(
                    id++,
                    0,
                    display,
                    issue,
                    issue.FilePath,
                    issue.LineNumber,
                    severity: issue.Severity
                );
                root.AddChild(issueItem);
            }
        }

        private static string FormatIssueDisplay(AnalyzerIssue issue)
        {
            string severityEmoji = issue.Severity switch
            {
                IssueSeverity.Critical => "🔴",
                IssueSeverity.High => "🟠",
                IssueSeverity.Medium => "🟡",
                IssueSeverity.Low => "🟢",
                IssueSeverity.Info => "🔵",
                _ => "⚪",
            };

            return $"{severityEmoji} Line {issue.LineNumber}: {issue.ClassName}.{issue.MethodName} - {issue.IssueType}";
        }

        private static string GetSeverityDisplayName(IssueSeverity severity)
        {
            return severity switch
            {
                IssueSeverity.Critical => "🔴 Critical",
                IssueSeverity.High => "🟠 High",
                IssueSeverity.Medium => "🟡 Medium",
                IssueSeverity.Low => "🟢 Low",
                IssueSeverity.Info => "🔵 Info",
                _ => "Unknown",
            };
        }

        private static string GetCategoryDisplayName(IssueCategory category)
        {
            return category switch
            {
                IssueCategory.UnityLifecycle => "🎮 Unity Lifecycle",
                IssueCategory.UnityInheritance => "🔷 Unity Inheritance",
                IssueCategory.GeneralInheritance => "📦 General Inheritance",
                _ => "Unknown",
            };
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            IssueTreeViewItem item = args.item as IssueTreeViewItem;

            Rect contentRect = args.rowRect;
            contentRect.xMin += GetContentIndent(args.item);

            if (item?.Issue != null)
            {
                GUIStyle style = new(EditorStyles.label);

                Color textColor = item.Severity switch
                {
                    IssueSeverity.Critical => new Color(1f, 0.3f, 0.3f),
                    IssueSeverity.High => new Color(1f, 0.6f, 0.2f),
                    IssueSeverity.Medium => new Color(1f, 0.9f, 0.2f),
                    IssueSeverity.Low => new Color(0.5f, 0.9f, 0.5f),
                    _ => Color.white,
                };

                style.normal.textColor = textColor;
                GUI.Label(contentRect, args.item.displayName, style);
            }
            else if (item?.IsFile == true)
            {
                GUIStyle style = new(EditorStyles.boldLabel);

                if (item.CriticalCount > 0)
                {
                    style.normal.textColor = new Color(1f, 0.5f, 0.5f);
                }
                else if (item.HighCount > 0)
                {
                    style.normal.textColor = new Color(1f, 0.7f, 0.4f);
                }

                GUI.Label(contentRect, args.item.displayName, style);
            }
            else
            {
                base.RowGUI(args);
            }
        }

        protected override void SingleClickedItem(int id)
        {
            TreeViewItem item = FindItem(id, rootItem);
            if (item is IssueTreeViewItem issueItem)
            {
                if (issueItem.Issue != null)
                {
                    OnIssueSelected?.Invoke(issueItem.Issue);
                }
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            TreeViewItem item = FindItem(id, rootItem);
            if (item is IssueTreeViewItem issueItem)
            {
                string filePath = issueItem.FilePath;
                int lineNumber = issueItem.LineNumber;

                if (issueItem.Issue != null)
                {
                    filePath = issueItem.Issue.FilePath;
                    lineNumber = issueItem.Issue.LineNumber;
                }

                if (!string.IsNullOrEmpty(filePath))
                {
                    OnOpenFile?.Invoke(filePath, lineNumber);
                }
            }
        }

        protected override void ContextClickedItem(int id)
        {
            TreeViewItem item = FindItem(id, rootItem);
            if (item is IssueTreeViewItem issueItem)
            {
                string filePath = issueItem.FilePath;
                if (issueItem.Issue != null)
                {
                    filePath = issueItem.Issue.FilePath;
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                GenericMenu menu = new();
                menu.AddItem(
                    new GUIContent("Open File"),
                    false,
                    () =>
                    {
                        int lineNumber = issueItem.LineNumber;
                        if (issueItem.Issue != null)
                        {
                            lineNumber = issueItem.Issue.LineNumber;
                        }

                        OnOpenFile?.Invoke(filePath, lineNumber);
                    }
                );
                menu.AddItem(
                    new GUIContent("Reveal in File Browser"),
                    false,
                    () => OnRevealInExplorer?.Invoke(filePath)
                );
                menu.ShowAsContext();
            }
        }
    }
#endif
}
