namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class SpriteSheetAnimationWindow : EditorWindow
    {
        private Texture2D _spriteSheet;
        private List<RowData> _rows = new List<RowData>();
        private IMGUIContainer _previewContainer;
        private SegmentData _previewSegment;
        private float _previewTime;

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/Sprite Sheet Animator")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<SpriteSheetAnimationWindow>();
            wnd.titleContent = new GUIContent("Sprite Animator");
            wnd.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            ConstructUI();
            EditorApplication.update += UpdatePreview;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdatePreview;
        }

        private void ConstructUI()
        {
            rootVisualElement.Clear();

            // Object field for drag & drop or picker
            var objField = new ObjectField("Sprite Sheet")
            {
                objectType = typeof(Texture2D),
                allowSceneObjects = false,
            };
            objField.RegisterValueChangedCallback(evt =>
            {
                _spriteSheet = evt.newValue as Texture2D;
                LoadSprites();
            });
            rootVisualElement.Add(objField);

            // Button to select via Unity picker
            var pickBtn = new Button(() =>
            {
                EditorGUIUtility.ShowObjectPicker<Texture2D>(_spriteSheet, false, "", 0);
            })
            {
                text = "Select Sprite Sheet",
            };
            rootVisualElement.Add(pickBtn);

            // Handle selection from picker
            rootVisualElement
                .schedule.Execute(() =>
                {
                    if (
                        Event.current != null
                        && Event.current.commandName == "ObjectSelectorUpdated"
                    )
                    {
                        var sel = EditorGUIUtility.GetObjectPickerObject();
                        if (sel is Texture2D tex)
                        {
                            _spriteSheet = tex;
                            objField.SetValueWithoutNotify(tex);
                            LoadSprites();
                        }
                    }
                })
                .Until(() => false);

            // Container for rows and segment settings
            var contentScroll = new ScrollView();
            rootVisualElement.Add(contentScroll);

            // Preview area
            _previewContainer = new IMGUIContainer(DrawPreview) { name = "preview" };
            _previewContainer.style.height = 120;
            rootVisualElement.Add(_previewContainer);

            // Generate animations button
            var genBtn = new Button(GenerateAnimations) { text = "Generate Animations" };
            rootVisualElement.Add(genBtn);
        }

        private void LoadSprites()
        {
            _rows.Clear();
            if (_spriteSheet == null)
                return;

            string path = AssetDatabase.GetAssetPath(_spriteSheet);
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            var sprites = new List<Sprite>();
            foreach (var a in assets)
                if (a is Sprite s)
                    sprites.Add(s);
            if (sprites.Count == 0)
                return;

            // Group by row (Y position)
            var rowMap = new Dictionary<float, List<Sprite>>();
            foreach (var s in sprites)
            {
                float y = s.rect.y;
                if (!rowMap.TryGetValue(y, out var list))
                {
                    list = new List<Sprite>();
                    rowMap[y] = list;
                }
                list.Add(s);
            }

            var sortedRows = new List<KeyValuePair<float, List<Sprite>>>(rowMap);
            sortedRows.Sort((a, b) => b.Key.CompareTo(a.Key)); // top row first

            foreach (var kv in sortedRows)
            {
                var row = new RowData();
                kv.Value.Sort((a, b) => a.rect.x.CompareTo(b.rect.x));
                row.sprites = kv.Value;
                row.length = row.sprites.Count;
                row.segmentLengths = new List<int> { row.length }; // default single segment
                _rows.Add(row);
            }

            RefreshRowUI();
        }

        private void RefreshRowUI()
        {
            var content = rootVisualElement.Q<ScrollView>();
            content.Clear();
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                var box = new Box();
                box.Add(new Label($"Row {i} ({row.length} sprites):"));

                // Segment lengths field
                var segField = new TextField("Segment lengths (e.g. 3,2)")
                {
                    value = string.Join(",", row.segmentLengths),
                };
                segField.RegisterValueChangedCallback(evt =>
                {
                    if (ParseInts(evt.newValue, out var lengths))
                        row.segmentLengths = lengths;
                    else
                        row.segmentLengths = new List<int> { row.length };
                    RebuildSegments(row);
                    RefreshRowUI();
                });
                box.Add(segField);

                // Build segments UI
                RebuildSegments(row);
                foreach (var seg in row.segments)
                {
                    var segBox = new Box();
                    segBox.style.flexDirection = FlexDirection.Row;
                    segBox.style.alignItems = Align.Center;
                    segBox.style.justifyContent = Justify.SpaceBetween;

                    var nameField = new TextField("Name") { value = seg.name };
                    nameField.RegisterValueChangedCallback(e => seg.name = e.newValue);
                    segBox.Add(nameField);

                    var rateField = new FloatField("FPS") { value = seg.baseFramerate };
                    rateField.RegisterValueChangedCallback(e => seg.baseFramerate = e.newValue);
                    segBox.Add(rateField);

                    var curveField = new CurveField("Curve") { value = seg.curve };
                    curveField.RegisterValueChangedCallback(e => seg.curve = e.newValue);
                    segBox.Add(curveField);

                    var previewBtn = new Button(() => StartPreview(seg)) { text = "Preview" };
                    segBox.Add(previewBtn);

                    box.Add(segBox);
                }

                content.Add(box);
            }
        }

        private void RebuildSegments(RowData row)
        {
            row.segments = new List<SegmentData>();
            int offset = 0;
            int idx = 0;
            foreach (var nl in row.segmentLengths)
            {
                if (offset >= row.length)
                    break;
                int len = Mathf.Min(nl, row.length - offset);
                var seg = new SegmentData
                {
                    start = offset,
                    length = len,
                    baseFramerate = 12f,
                    curve = AnimationCurve.Linear(0, 1, 1, 1),
                    name = $"{_spriteSheet.name}_r{idx}_s{offset}_{len}",
                    row = row,
                };
                row.segments.Add(seg);
                offset += len;
                idx++;
            }
            if (offset < row.length)
            {
                var seg = new SegmentData
                {
                    start = offset,
                    length = row.length - offset,
                    baseFramerate = 12f,
                    curve = AnimationCurve.Linear(0, 1, 1, 1),
                    name =
                        $"{_spriteSheet.name}_r{_rows.IndexOf(row)}_s{offset}_{row.length - offset}",
                    row = row,
                };
                row.segments.Add(seg);
            }
        }

        private bool ParseInts(string s, out List<int> list)
        {
            list = new List<int>();
            var parts = s.Split(',');
            foreach (var p in parts)
            {
                if (int.TryParse(p.Trim(), out var v) && v > 0)
                    list.Add(v);
                else
                    return false;
            }
            return list.Count > 0;
        }

        private void StartPreview(SegmentData seg)
        {
            _previewSegment = seg;
            _previewTime = 0;
        }

        private void UpdatePreview()
        {
            if (_previewSegment == null)
                return;
            _previewTime += Time.deltaTime;
            _previewContainer.MarkDirtyRepaint();
        }

        private void DrawPreview()
        {
            if (_previewSegment == null || _spriteSheet == null)
                return;
            var row = _previewSegment.row;
            int idx = (int)(_previewTime * _previewSegment.baseFramerate) % _previewSegment.length;
            // variable rate can be added here via curve evaluation
            var sprite = row.sprites[_previewSegment.start + idx];

            Rect r = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                var uv = new Rect(
                    sprite.rect.x / _spriteSheet.width,
                    sprite.rect.y / _spriteSheet.height,
                    sprite.rect.width / _spriteSheet.width,
                    sprite.rect.height / _spriteSheet.height
                );
                GUI.DrawTextureWithTexCoords(r, _spriteSheet, uv, true);
            }
        }

        private void GenerateAnimations()
        {
            if (_spriteSheet == null)
                return;
            string defaultPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(_spriteSheet));
            string folder = EditorUtility.OpenFolderPanel("Select Output Folder", defaultPath, "");
            if (string.IsNullOrEmpty(folder))
                return;

            foreach (var row in _rows)
            {
                foreach (var seg in row.segments)
                {
                    var clip = new AnimationClip();
                    clip.frameRate = seg.baseFramerate;

                    var keys = new ObjectReferenceKeyframe[seg.length];
                    float time = 0f;
                    for (int i = 0; i < seg.length; i++)
                    {
                        float speedMult = seg.curve.Evaluate(
                            (float)i / Mathf.Max(1, seg.length - 1)
                        );
                        float dt = 1f / seg.baseFramerate * speedMult;
                        time += dt;
                        keys[i] = new ObjectReferenceKeyframe
                        {
                            time = time,
                            value = row.sprites[seg.start + i],
                        };
                    }
                    var binding = new EditorCurveBinding
                    {
                        type = typeof(SpriteRenderer),
                        path = string.Empty,
                        propertyName = "m_Sprite",
                    };
                    AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

                    string clipPath = Path.Combine(folder, seg.name + ".anim");
                    clipPath = AssetDatabase.GenerateUniqueAssetPath(clipPath);
                    AssetDatabase.CreateAsset(clip, clipPath);
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Done", "Animations generated!", "OK");
        }

        // Data classes
        private class RowData
        {
            public List<Sprite> sprites;
            public int length;
            public List<int> segmentLengths;
            public List<SegmentData> segments = new List<SegmentData>();
        }

        private class SegmentData
        {
            public RowData row;
            public int start;
            public int length;
            public float baseFramerate;
            public AnimationCurve curve;
            public string name;
        }
    }
}
