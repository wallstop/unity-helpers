namespace WallstopStudios.UnityHelpers.Editor.Tools.UnityMethodAnalyzer
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEditor.IMGUI.Controls;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Unity Editor window for analyzing C# code for inheritance and Unity method issues.
    /// </summary>
    public sealed class UnityMethodAnalyzerWindow : EditorWindow
    {
        private static bool SuppressUserPrompts { get; set; }

        static UnityMethodAnalyzerWindow()
        {
            try
            {
                if (Application.isBatchMode || IsInvokedByTestRunner())
                {
                    SuppressUserPrompts = true;
                }
            }
            catch { }
        }

        private static bool IsInvokedByTestRunner()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; ++i)
            {
                string a = args[i];
                if (
                    a.IndexOf("runTests", StringComparison.OrdinalIgnoreCase) >= 0
                    || a.IndexOf("testResults", StringComparison.OrdinalIgnoreCase) >= 0
                    || a.IndexOf("testPlatform", StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    return true;
                }
            }

            return false;
        }

        [SerializeField]
        private List<string> _sourcePaths = new();

        [SerializeField]
        private TreeViewState _treeViewState;

        [SerializeField]
        private bool _sourcePathsFoldout = true;

        private Vector2 _sourcePathsScrollPosition;

        private MethodAnalyzer _analyzer;
        private IssueTreeView _treeView;
        private Vector2 _scrollPosition;
        private Vector2 _detailScrollPosition;

        private AnalyzerIssue _selectedIssue;
        private bool _isAnalyzing;
        private float _analysisProgress;
        private string _statusMessage = "Ready to analyze";
        private CancellationTokenSource _cancellationTokenSource;

        private bool _groupByFile = true;
        private bool _groupBySeverity;
        private bool _groupByCategory;
        private IssueSeverity? _severityFilter;
        private IssueCategory? _categoryFilter;
        private string _searchFilter = string.Empty;

        private int _criticalCount;
        private int _highCount;
        private int _mediumCount;
        private int _lowCount;
        private int _infoCount;
        private int _totalCount;

        private const float ToolbarHeight = 40f;
        private const float FilterHeight = 50f;
        private const float SummaryHeight = 30f;
        private const float DetailPanelMinHeight = 150f;
        private const float SplitterHeight = 4f;
        private const float MinSourcePathsSectionHeight = 80f;
        private const float MaxSourcePathsSectionHeight = 200f;
        private const float NarrowLayoutThreshold = 700f;
        private const float VeryNarrowLayoutThreshold = 500f;

        private float _detailPanelHeight = 200f;
        private bool _isResizingDetailPanel;

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/Unity Method Analyzer")]
        public static void ShowWindow()
        {
            UnityMethodAnalyzerWindow window = GetWindow<UnityMethodAnalyzerWindow>(
                "Unity Method Analyzer"
            );
            window.minSize = new Vector2(450, 400);
            window.Show();
        }

        private void OnEnable()
        {
            _analyzer = new MethodAnalyzer();
            _treeViewState ??= new TreeViewState();
            _treeView = new IssueTreeView(_treeViewState);
            _treeView.OnIssueSelected += OnIssueSelected;
            _treeView.OnOpenFile += OpenFileAtLine;
            _treeView.OnRevealInExplorer += RevealFileInExplorer;

            if (_sourcePaths == null || _sourcePaths.Count == 0)
            {
                _sourcePaths = new List<string> { GetProjectRoot() };
            }
        }

        private void OnDisable()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            if (_treeView != null)
            {
                _treeView.OnIssueSelected -= OnIssueSelected;
                _treeView.OnOpenFile -= OpenFileAtLine;
                _treeView.OnRevealInExplorer -= RevealFileInExplorer;
            }
        }

        private static string GetProjectRoot()
        {
            return Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
        }

        private void OnGUI()
        {
            float windowWidth = position.width;
            bool isNarrowLayout = windowWidth < NarrowLayoutThreshold;
            bool isVeryNarrowLayout = windowWidth < VeryNarrowLayoutThreshold;

            float sourcePathsHeight = DrawSourcePathsSection(isNarrowLayout);
            DrawToolbar(isNarrowLayout);
            DrawFilters(isNarrowLayout, isVeryNarrowLayout);
            DrawSummary(isNarrowLayout);

            float fixedHeight = ToolbarHeight + FilterHeight + SummaryHeight + sourcePathsHeight;
            float remainingHeight = position.height - fixedHeight - _detailPanelHeight;

            DrawTreeView(Mathf.Max(remainingHeight, 100f));
            DrawSplitter();
            DrawDetailPanel();

            if (_isAnalyzing)
            {
                Repaint();
            }
        }

        private float DrawSourcePathsSection(bool isNarrowLayout)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.BeginHorizontal();
            _sourcePathsFoldout = EditorGUILayout.Foldout(
                _sourcePathsFoldout,
                $"Source Directories ({_sourcePaths?.Count ?? 0})",
                true
            );

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                string browsePath =
                    _sourcePaths?.Count > 0 && !string.IsNullOrEmpty(_sourcePaths[^1])
                        ? _sourcePaths[^1]
                        : GetProjectRoot();

                string selectedPath = EditorUi.OpenFolderPanel(
                    "Select Source Directory",
                    browsePath,
                    ""
                );

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    _sourcePaths ??= new List<string>();
                    if (!_sourcePaths.Contains(selectedPath))
                    {
                        _sourcePaths.Add(selectedPath);
                    }
                }
            }

            GUILayout.EndHorizontal();

            float sectionHeight = EditorGUIUtility.singleLineHeight + 6f;

            if (_sourcePathsFoldout && _sourcePaths != null && _sourcePaths.Count > 0)
            {
                float pathsListHeight = Mathf.Min(
                    _sourcePaths.Count * (EditorGUIUtility.singleLineHeight + 4f),
                    MaxSourcePathsSectionHeight - sectionHeight
                );
                pathsListHeight = Mathf.Max(
                    pathsListHeight,
                    MinSourcePathsSectionHeight - sectionHeight
                );

                _sourcePathsScrollPosition = GUILayout.BeginScrollView(
                    _sourcePathsScrollPosition,
                    GUILayout.Height(pathsListHeight)
                );

                int removeIndex = -1;
                for (int i = 0; i < _sourcePaths.Count; i++)
                {
                    GUILayout.BeginHorizontal();

                    string displayPath = _sourcePaths[i];
                    string projectRoot = GetProjectRoot();
                    if (
                        !string.IsNullOrEmpty(displayPath)
                        && displayPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        displayPath =
                            displayPath.Length > projectRoot.Length
                                ? "." + displayPath.Substring(projectRoot.Length)
                                : ".";
                    }

                    bool pathExists =
                        !string.IsNullOrEmpty(_sourcePaths[i]) && Directory.Exists(_sourcePaths[i]);
                    GUIStyle pathStyle = pathExists
                        ? EditorStyles.label
                        : new GUIStyle(EditorStyles.label)
                        {
                            normal = { textColor = new Color(1f, 0.4f, 0.4f) },
                        };

                    float labelWidth = isNarrowLayout
                        ? position.width - 85f
                        : position.width - 105f;
                    GUILayout.Label(
                        new GUIContent(displayPath, _sourcePaths[i]),
                        pathStyle,
                        GUILayout.Width(labelWidth)
                    );

                    if (GUILayout.Button("...", GUILayout.Width(30)))
                    {
                        string browsePath =
                            !string.IsNullOrEmpty(_sourcePaths[i])
                            && Directory.Exists(_sourcePaths[i])
                                ? _sourcePaths[i]
                                : GetProjectRoot();

                        string selectedPath = EditorUi.OpenFolderPanel(
                            "Select Source Directory",
                            browsePath,
                            ""
                        );

                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            _sourcePaths[i] = selectedPath;
                        }
                    }

                    if (GUILayout.Button("-", GUILayout.Width(25)))
                    {
                        removeIndex = i;
                    }

                    GUILayout.EndHorizontal();
                }

                if (removeIndex >= 0)
                {
                    _sourcePaths.RemoveAt(removeIndex);
                }

                GUILayout.EndScrollView();
                sectionHeight += pathsListHeight;
            }

            GUILayout.EndVertical();

            return sectionHeight + 4f;
        }

        private void DrawToolbar(bool isNarrowLayout)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(ToolbarHeight));

            Color originalBgColor = GUI.backgroundColor;

            bool hasValidPaths =
                _sourcePaths != null
                && _sourcePaths.Any(p => !string.IsNullOrEmpty(p) && Directory.Exists(p));

            GUI.enabled = !_isAnalyzing && hasValidPaths;

            GUIStyle analyzeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                fixedHeight = ToolbarHeight - 8,
                padding = new RectOffset(12, 12, 4, 4),
            };

            GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);

            string buttonText = isNarrowLayout ? "▶ Analyze" : "▶ Analyze Code";
            float buttonWidth = isNarrowLayout ? 95f : 130f;

            if (GUILayout.Button(buttonText, analyzeButtonStyle, GUILayout.Width(buttonWidth)))
            {
                StartAnalysis();
            }

            GUI.backgroundColor = originalBgColor;
            GUI.enabled = true;

            if (_isAnalyzing)
            {
                GUILayout.Space(8);

                GUIStyle cancelButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fixedHeight = ToolbarHeight - 8,
                };

                GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);

                if (GUILayout.Button("Cancel", cancelButtonStyle, GUILayout.Width(60)))
                {
                    CancelAnalysis();
                }

                GUI.backgroundColor = originalBgColor;

                GUILayout.Space(8);

                Rect progressRect = GUILayoutUtility.GetRect(
                    isNarrowLayout ? 80f : 120f,
                    ToolbarHeight - 12,
                    GUILayout.ExpandWidth(false)
                );
                progressRect.y += 2;
                EditorGUI.ProgressBar(
                    progressRect,
                    _analysisProgress,
                    $"{_analysisProgress * 100:F0}%"
                );
            }

            GUILayout.FlexibleSpace();

            GUIStyle statusStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight,
                wordWrap = true,
            };
            GUILayout.Label(
                _statusMessage,
                statusStyle,
                GUILayout.MaxWidth(isNarrowLayout ? 150f : 300f)
            );

            GUILayout.EndHorizontal();
        }

        private void DrawFilters(bool isNarrowLayout, bool isVeryNarrowLayout)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);

            if (isVeryNarrowLayout)
            {
                DrawFiltersVerticalLayout();
            }
            else if (isNarrowLayout)
            {
                DrawFiltersTwoRowLayout();
            }
            else
            {
                DrawFiltersSingleRowLayout();
            }

            GUILayout.EndVertical();
        }

        private void DrawFiltersSingleRowLayout()
        {
            GUILayout.BeginHorizontal();

            DrawGroupBySection();
            GUILayout.Space(15);
            DrawSeverityFilter();
            GUILayout.Space(10);
            DrawCategoryFilter();
            GUILayout.Space(10);
            DrawSearchFilter();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawFiltersTwoRowLayout()
        {
            GUILayout.BeginHorizontal();
            DrawGroupBySection();
            GUILayout.FlexibleSpace();
            DrawSearchFilter();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawSeverityFilter();
            GUILayout.Space(10);
            DrawCategoryFilter();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawFiltersVerticalLayout()
        {
            DrawGroupBySection();
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            DrawSeverityFilter();
            GUILayout.Space(10);
            DrawCategoryFilter();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            DrawSearchFilter();
        }

        private void DrawGroupBySection()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Group By:", GUILayout.Width(60));

            bool newGroupByFile = GUILayout.Toggle(
                _groupByFile,
                "File",
                EditorStyles.miniButtonLeft,
                GUILayout.Width(50)
            );
            bool newGroupBySeverity = GUILayout.Toggle(
                _groupBySeverity,
                "Severity",
                EditorStyles.miniButtonMid,
                GUILayout.Width(60)
            );
            bool newGroupByCategory = GUILayout.Toggle(
                _groupByCategory,
                "Category",
                EditorStyles.miniButtonRight,
                GUILayout.Width(65)
            );
            GUILayout.EndHorizontal();

            if (newGroupByFile != _groupByFile)
            {
                _groupByFile = newGroupByFile;
                if (_groupByFile)
                {
                    _groupBySeverity = false;
                    _groupByCategory = false;
                }

                UpdateTreeViewGrouping();
            }

            if (newGroupBySeverity != _groupBySeverity)
            {
                _groupBySeverity = newGroupBySeverity;
                if (_groupBySeverity)
                {
                    _groupByFile = false;
                    _groupByCategory = false;
                }

                UpdateTreeViewGrouping();
            }

            if (newGroupByCategory != _groupByCategory)
            {
                _groupByCategory = newGroupByCategory;
                if (_groupByCategory)
                {
                    _groupByFile = false;
                    _groupBySeverity = false;
                }

                UpdateTreeViewGrouping();
            }
        }

        private void DrawSeverityFilter()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Severity:", GUILayout.Width(55));
            int severityIndex = _severityFilter.HasValue ? (int)_severityFilter.Value : 0;
            string[] severityOptions = { "All", "Critical", "High", "Medium", "Low", "Info" };
            int newSeverityIndex = EditorGUILayout.Popup(
                severityIndex,
                severityOptions,
                GUILayout.Width(80)
            );
            GUILayout.EndHorizontal();

            if (newSeverityIndex != severityIndex)
            {
                _severityFilter = newSeverityIndex == 0 ? null : (IssueSeverity)newSeverityIndex;
                UpdateTreeViewFilters();
            }
        }

        private void DrawCategoryFilter()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Category:", GUILayout.Width(60));
            int categoryIndex = _categoryFilter.HasValue ? (int)_categoryFilter.Value + 1 : 0;
            string[] categoryOptions =
            {
                "All",
                "Unity Lifecycle",
                "Unity Inheritance",
                "General Inheritance",
            };
            int newCategoryIndex = EditorGUILayout.Popup(
                categoryIndex,
                categoryOptions,
                GUILayout.Width(120)
            );
            GUILayout.EndHorizontal();

            if (newCategoryIndex != categoryIndex)
            {
                _categoryFilter =
                    newCategoryIndex == 0 ? null : (IssueCategory)(newCategoryIndex - 1);
                UpdateTreeViewFilters();
            }
        }

        private void DrawSearchFilter()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(50));
            string newSearch = EditorGUILayout.TextField(_searchFilter, GUILayout.MinWidth(100));
            GUILayout.EndHorizontal();

            if (newSearch != _searchFilter)
            {
                _searchFilter = newSearch;
                UpdateTreeViewFilters();
            }
        }

        private void DrawSummary(bool isNarrowLayout)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUIStyle totalStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
            };
            GUILayout.Label(
                $"Total: {_totalCount}",
                totalStyle,
                GUILayout.Width(isNarrowLayout ? 60f : 80f)
            );

            GUIStyle criticalStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(1f, 0.3f, 0.3f) },
            };

            GUIStyle highStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(1f, 0.6f, 0.2f) },
            };

            GUIStyle mediumStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(1f, 0.9f, 0.2f) },
            };

            GUIStyle lowStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.5f, 0.9f, 0.5f) },
            };

            GUIStyle infoStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.5f, 0.7f, 1f) },
            };

            if (isNarrowLayout)
            {
                GUILayout.Label($"🔴{_criticalCount}", criticalStyle);
                GUILayout.Label($"🟠{_highCount}", highStyle);
                GUILayout.Label($"🟡{_mediumCount}", mediumStyle);
                GUILayout.Label($"🟢{_lowCount}", lowStyle);
                GUILayout.Label($"🔵{_infoCount}", infoStyle);
            }
            else
            {
                GUILayout.Label(
                    $"🔴 Critical: {_criticalCount}",
                    criticalStyle,
                    GUILayout.Width(100)
                );
                GUILayout.Label($"🟠 High: {_highCount}", highStyle, GUILayout.Width(80));
                GUILayout.Label($"🟡 Medium: {_mediumCount}", mediumStyle, GUILayout.Width(100));
                GUILayout.Label($"🟢 Low: {_lowCount}", lowStyle, GUILayout.Width(70));
                GUILayout.Label($"🔵 Info: {_infoCount}", infoStyle, GUILayout.Width(70));
            }

            GUILayout.FlexibleSpace();

            if (
                _totalCount > 0
                && GUILayout.Button("Export", GUILayout.Width(isNarrowLayout ? 60f : 100f))
            )
            {
                ExportReport();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawTreeView(float height)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(height));

            GUILayout.Label("Issues", EditorStyles.boldLabel);

            float innerHeight = height - EditorGUIUtility.singleLineHeight - 8f;
            Rect treeViewRect = GUILayoutUtility.GetRect(
                0,
                innerHeight,
                GUILayout.ExpandWidth(true)
            );
            _treeView?.OnGUI(treeViewRect);

            GUILayout.EndVertical();
        }

        private void DrawSplitter()
        {
            Rect splitterRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                GUIStyle.none,
                GUILayout.Height(SplitterHeight),
                GUILayout.ExpandWidth(true)
            );

            EditorGUI.DrawRect(splitterRect, new Color(0.2f, 0.2f, 0.2f));
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeVertical);

            if (
                Event.current.type == EventType.MouseDown
                && splitterRect.Contains(Event.current.mousePosition)
            )
            {
                _isResizingDetailPanel = true;
                Event.current.Use();
            }

            if (_isResizingDetailPanel)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    _detailPanelHeight -= Event.current.delta.y;
                    _detailPanelHeight = Mathf.Clamp(
                        _detailPanelHeight,
                        DetailPanelMinHeight,
                        position.height - 200
                    );
                    Repaint();
                    Event.current.Use();
                }

                if (Event.current.type == EventType.MouseUp)
                {
                    _isResizingDetailPanel = false;
                    Event.current.Use();
                }
            }
        }

        private void DrawDetailPanel()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(_detailPanelHeight));

            GUILayout.Label("Issue Details", EditorStyles.boldLabel);

            if (_selectedIssue == null)
            {
                GUILayout.Label(
                    "Select an issue to view details. Double-click to open the file.",
                    EditorStyles.centeredGreyMiniLabel
                );
            }
            else
            {
                _detailScrollPosition = GUILayout.BeginScrollView(_detailScrollPosition);

                GUILayout.BeginHorizontal();
                GUILayout.Label("File:", EditorStyles.boldLabel, GUILayout.Width(100));
                if (
                    GUILayout.Button(
                        $"{_selectedIssue.FilePath}:{_selectedIssue.LineNumber}",
                        EditorStyles.linkLabel
                    )
                )
                {
                    OpenFileAtLine(_selectedIssue.FilePath, _selectedIssue.LineNumber);
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Class:", EditorStyles.boldLabel, GUILayout.Width(100));
                GUILayout.Label(_selectedIssue.ClassName);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Method:", EditorStyles.boldLabel, GUILayout.Width(100));
                GUILayout.Label(_selectedIssue.MethodName);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Issue Type:", EditorStyles.boldLabel, GUILayout.Width(100));
                GUILayout.Label(_selectedIssue.IssueType);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Severity:", EditorStyles.boldLabel, GUILayout.Width(100));
                GUILayout.Label(GetSeverityDisplay(_selectedIssue.Severity));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Category:", EditorStyles.boldLabel, GUILayout.Width(100));
                GUILayout.Label(GetCategoryDisplay(_selectedIssue.Category));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                GUILayout.Label("Description:", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(_selectedIssue.Description, MessageType.Warning);

                GUILayout.Label("Recommended Fix:", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(_selectedIssue.RecommendedFix, MessageType.Info);

                if (!string.IsNullOrEmpty(_selectedIssue.BaseClassName))
                {
                    GUILayout.Space(10);
                    GUILayout.Label("Inheritance Details:", EditorStyles.boldLabel);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Base Class:", GUILayout.Width(100));
                    GUILayout.Label(_selectedIssue.BaseClassName);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    if (!string.IsNullOrEmpty(_selectedIssue.BaseMethodSignature))
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Base Method:", GUILayout.Width(100));
                        GUILayout.Label(_selectedIssue.BaseMethodSignature, EditorStyles.miniLabel);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }

                    if (!string.IsNullOrEmpty(_selectedIssue.DerivedMethodSignature))
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Derived Method:", GUILayout.Width(100));
                        GUILayout.Label(
                            _selectedIssue.DerivedMethodSignature,
                            EditorStyles.miniLabel
                        );
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
        }

        private static string GetSeverityDisplay(IssueSeverity severity)
        {
            return severity switch
            {
                IssueSeverity.Critical => "🔴 Critical - Will definitely cause bugs",
                IssueSeverity.High => "🟠 High - Very likely to cause bugs",
                IssueSeverity.Medium => "🟡 Medium - May cause subtle bugs",
                IssueSeverity.Low => "🟢 Low - Code smell or maintainability issue",
                IssueSeverity.Info => "🔵 Info - Informational only",
                _ => "Unknown",
            };
        }

        private static string GetCategoryDisplay(IssueCategory category)
        {
            return category switch
            {
                IssueCategory.UnityLifecycle => "🎮 Unity Lifecycle",
                IssueCategory.UnityInheritance => "🔷 Unity Inheritance",
                IssueCategory.GeneralInheritance => "📦 General Inheritance",
                _ => "Unknown",
            };
        }

        private async void StartAnalysis()
        {
            if (_isAnalyzing)
            {
                return;
            }

            _isAnalyzing = true;
            _analysisProgress = 0f;
            _statusMessage = "Analyzing...";
            _selectedIssue = null;

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                List<string> directories = new();
                string rootPath = GetProjectRoot();

                foreach (string sourcePath in _sourcePaths)
                {
                    if (string.IsNullOrEmpty(sourcePath))
                    {
                        continue;
                    }

                    if (Directory.Exists(sourcePath))
                    {
                        directories.Add(sourcePath);
                    }
                }

                if (directories.Count == 0)
                {
                    _statusMessage = "No valid directories selected";
                    return;
                }

                Progress<float> progress = new Progress<float>(p =>
                {
                    _analysisProgress = p;
                });

                await _analyzer.AnalyzeAsync(
                    rootPath,
                    directories,
                    progress,
                    _cancellationTokenSource.Token
                );

                UpdateIssueCounts();
                _treeView.SetIssues(_analyzer.Issues, rootPath);

                _statusMessage = $"Analysis complete: {_totalCount} issues found";
            }
            catch (OperationCanceledException)
            {
                _statusMessage = "Analysis cancelled";
            }
            catch (Exception ex)
            {
                _statusMessage = $"Analysis failed: {ex.Message}";
                Debug.LogException(ex);
            }
            finally
            {
                _isAnalyzing = false;
                _analysisProgress = 0f;
                Repaint();
            }
        }

        private void CancelAnalysis()
        {
            _cancellationTokenSource?.Cancel();
        }

        private void UpdateIssueCounts()
        {
            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;
            _totalCount = issues.Count;
            _criticalCount = issues.Count(i => i.Severity == IssueSeverity.Critical);
            _highCount = issues.Count(i => i.Severity == IssueSeverity.High);
            _mediumCount = issues.Count(i => i.Severity == IssueSeverity.Medium);
            _lowCount = issues.Count(i => i.Severity == IssueSeverity.Low);
            _infoCount = issues.Count(i => i.Severity == IssueSeverity.Info);
        }

        private void UpdateTreeViewGrouping()
        {
            _treeView?.SetGrouping(_groupByFile, _groupBySeverity, _groupByCategory);
        }

        private void UpdateTreeViewFilters()
        {
            _treeView?.SetFilters(_severityFilter, _categoryFilter, _searchFilter);
        }

        private void OnIssueSelected(AnalyzerIssue issue)
        {
            _selectedIssue = issue;
            Repaint();
        }

        private void OpenFileAtLine(string relativePath, int lineNumber)
        {
            string fullPath = Path.Combine(Application.dataPath, "..", relativePath);
            fullPath = Path.GetFullPath(fullPath);

            if (!File.Exists(fullPath))
            {
                string assetsPath = Path.Combine(Application.dataPath, relativePath);
                if (File.Exists(assetsPath))
                {
                    fullPath = assetsPath;
                }
                else
                {
                    string[] foundFiles = Directory.GetFiles(
                        Application.dataPath,
                        Path.GetFileName(relativePath),
                        SearchOption.AllDirectories
                    );

                    if (foundFiles.Length > 0)
                    {
                        fullPath = foundFiles[0];
                    }
                }
            }

            string assetPath = fullPath;
            if (fullPath.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase))
            {
                assetPath =
                    "Assets" + fullPath.Substring(Application.dataPath.Length).Replace('\\', '/');
            }

            Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset, lineNumber);
            }
            else
            {
                if (File.Exists(fullPath))
                {
                    UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(
                        fullPath,
                        lineNumber
                    );
                }
                else
                {
                    Debug.LogWarning($"Could not find file: {relativePath}");
                }
            }
        }

        private void RevealFileInExplorer(string relativePath)
        {
            string fullPath = Path.Combine(Application.dataPath, "..", relativePath);
            fullPath = Path.GetFullPath(fullPath);

            if (!File.Exists(fullPath))
            {
                string assetsPath = Path.Combine(Application.dataPath, relativePath);
                if (File.Exists(assetsPath))
                {
                    fullPath = assetsPath;
                }
                else
                {
                    string[] foundFiles = Directory.GetFiles(
                        Application.dataPath,
                        Path.GetFileName(relativePath),
                        SearchOption.AllDirectories
                    );

                    if (foundFiles.Length > 0)
                    {
                        fullPath = foundFiles[0];
                    }
                }
            }

            if (File.Exists(fullPath))
            {
                EditorUtility.RevealInFinder(fullPath);
            }
            else
            {
                Debug.LogWarning($"Could not find file to reveal: {relativePath}");
            }
        }

        private void ExportReport()
        {
            string defaultName = $"method-analysis-report-{DateTime.Now:yyyy-MM-dd-HHmmss}.md";
            string path = EditorUtility.SaveFilePanel(
                "Export Analysis Report",
                "",
                defaultName,
                "md"
            );

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                string report = GenerateMarkdownReport();
                File.WriteAllText(path, report);
                _statusMessage = $"Report exported to: {Path.GetFileName(path)}";
                EditorUtility.RevealInFinder(path);
            }
            catch (Exception ex)
            {
                _statusMessage = $"Export failed: {ex.Message}";
                Debug.LogException(ex);
            }
        }

        private string GenerateMarkdownReport()
        {
            System.Text.StringBuilder sb = new();
            IReadOnlyList<AnalyzerIssue> issues = _analyzer.Issues;

            sb.AppendLine("# Unity Method Analysis Report");
            sb.AppendLine();
            sb.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine($"**Total Issues Found:** {issues.Count}");
            sb.AppendLine();

            sb.AppendLine("## Summary by Severity");
            sb.AppendLine();
            sb.AppendLine("| Severity | Count |");
            sb.AppendLine("|----------|-------|");
            sb.AppendLine($"| 🔴 Critical | {_criticalCount} |");
            sb.AppendLine($"| 🟠 High | {_highCount} |");
            sb.AppendLine($"| 🟡 Medium | {_mediumCount} |");
            sb.AppendLine($"| 🟢 Low | {_lowCount} |");
            sb.AppendLine($"| 🔵 Info | {_infoCount} |");
            sb.AppendLine();

            sb.AppendLine("## Summary by Category");
            sb.AppendLine();
            sb.AppendLine("| Category | Count |");
            sb.AppendLine("|----------|-------|");
            sb.AppendLine(
                $"| 🎮 Unity Lifecycle | {issues.Count(i => i.Category == IssueCategory.UnityLifecycle)} |"
            );
            sb.AppendLine(
                $"| 🔷 Unity Inheritance | {issues.Count(i => i.Category == IssueCategory.UnityInheritance)} |"
            );
            sb.AppendLine(
                $"| 📦 General Inheritance | {issues.Count(i => i.Category == IssueCategory.GeneralInheritance)} |"
            );
            sb.AppendLine();

            sb.AppendLine("## Detailed Issues");
            sb.AppendLine();

            IOrderedEnumerable<IGrouping<string, AnalyzerIssue>> issuesByFile = issues
                .OrderBy(i => (int)i.Severity)
                .ThenBy(i => i.FilePath)
                .GroupBy(i => i.FilePath)
                .OrderBy(g => g.Key);

            foreach (IGrouping<string, AnalyzerIssue> fileGroup in issuesByFile)
            {
                sb.AppendLine($"### `{fileGroup.Key}`");
                sb.AppendLine();

                foreach (AnalyzerIssue issue in fileGroup.OrderBy(i => i.LineNumber))
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

                    sb.AppendLine(
                        $"#### {severityEmoji} Line {issue.LineNumber}: `{issue.ClassName}.{issue.MethodName}` - {issue.IssueType}"
                    );
                    sb.AppendLine();
                    sb.AppendLine($"**Category:** {issue.Category}");
                    sb.AppendLine();
                    sb.AppendLine($"**Description:** {issue.Description}");
                    sb.AppendLine();

                    if (!string.IsNullOrEmpty(issue.BaseClassName))
                    {
                        sb.AppendLine($"**Base Class:** `{issue.BaseClassName}`");
                        if (!string.IsNullOrEmpty(issue.BaseMethodSignature))
                        {
                            sb.AppendLine($"**Base Method:** `{issue.BaseMethodSignature}`");
                        }
                    }

                    if (!string.IsNullOrEmpty(issue.DerivedMethodSignature))
                    {
                        sb.AppendLine($"**Derived Method:** `{issue.DerivedMethodSignature}`");
                    }

                    sb.AppendLine();
                    sb.AppendLine($"**Recommended Fix:** {issue.RecommendedFix}");
                    sb.AppendLine();
                    sb.AppendLine("---");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }
#endif
}
