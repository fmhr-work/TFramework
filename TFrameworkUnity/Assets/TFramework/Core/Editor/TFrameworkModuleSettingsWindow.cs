using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TFramework.Core.Editor
{
    /// <summary>
    /// モジュール設定ウィンドウ
    /// </summary>
    public sealed class TFrameworkModuleSettingsWindow : EditorWindow
    {
        private enum ModuleInstallState
        {
            Missing,
            Installed,
            OutsideResources,
            MultipleFound
        }

        private sealed class ModuleRow
        {
            public ModuleSettingsCatalog.ModuleSettingsDefinition Definition { get; }
            public ModuleInstallState State { get; set; }
            public int CandidateCount { get; set; }

            public ModuleRow(ModuleSettingsCatalog.ModuleSettingsDefinition definition)
            {
                Definition = definition;
            }
        }

        private readonly List<ModuleRow> _rows = new();
        private ListView _moduleListView;
        private VisualElement _rightPane;
        private ToolbarButton _refreshButton;

        private int _selectedIndex = -1;
        private UnityEngine.Object _selectedCandidateAsset;

        [MenuItem("TFramework/Settings/Modules")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<TFrameworkModuleSettingsWindow>();
            wnd.titleContent = new GUIContent("TFramework Settings");
            wnd.minSize = new Vector2(900, 600);
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();

            var splitView = new TwoPaneSplitView(0, 280, TwoPaneSplitViewOrientation.Horizontal);
            root.Add(splitView);

            var leftPane = new VisualElement { style = { flexGrow = 1 } };
            splitView.Add(leftPane);

            var rightPane = new VisualElement { style = { flexGrow = 1 } };
            splitView.Add(rightPane);
            _rightPane = rightPane;

            leftPane.Add(CreateLeftHeader());

            _moduleListView = new ListView
            {
                style = { flexGrow = 1 },
                fixedItemHeight = 22,
                selectionType = SelectionType.Single,
                makeItem = MakeLeftItem,
                bindItem = BindLeftItem
            };
            _moduleListView.selectionChanged += OnSelectionChanged;
            leftPane.Add(_moduleListView);

            var toolbar = new Toolbar();
            _refreshButton = new ToolbarButton(RefreshAll) { text = "Refresh" };
            toolbar.Add(_refreshButton);
            leftPane.Add(toolbar);

            RebuildRows();
            _moduleListView.itemsSource = _rows;
            _moduleListView.Rebuild();

            DrawRightPaneEmpty();
        }

        private VisualElement CreateLeftHeader()
        {
            var header = new Label("Modules")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    paddingLeft = 6,
                    paddingTop = 6,
                    paddingBottom = 6
                }
            };
            return header;
        }

        private VisualElement MakeLeftItem()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            row.Add(new Label { name = "name", style = { flexGrow = 1, paddingLeft = 6 } });
            row.Add(new Label { name = "state", style = { width = 130, unityTextAlign = TextAnchor.MiddleRight, paddingRight = 6 } });
            return row;
        }

        private void BindLeftItem(VisualElement element, int index)
        {
            if (index < 0 || index >= _rows.Count) return;

            var row = _rows[index];
            var name = element.Q<Label>("name");
            var state = element.Q<Label>("state");

            name.text = row.Definition.DisplayName;
            state.text = FormatState(row.State, row.CandidateCount);
        }

        private string FormatState(ModuleInstallState state, int candidateCount)
        {
            return state switch
            {
                ModuleInstallState.Installed => "Installed",
                ModuleInstallState.Missing => "Missing",
                ModuleInstallState.OutsideResources => "Outside",
                ModuleInstallState.MultipleFound => $"Multiple ({candidateCount})",
                _ => state.ToString()
            };
        }

        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            var selected = selection.FirstOrDefault();
            _selectedIndex = selected != null ? _rows.IndexOf((ModuleRow)selected) : -1;
            _selectedCandidateAsset = null;
            DrawRightPane();
        }

        private void RefreshAll()
        {
            RebuildRows();
            _moduleListView.Rebuild();
            DrawRightPane();
        }

        private void RebuildRows()
        {
            _rows.Clear();
            foreach (var def in ModuleSettingsCatalog.Definitions)
            {
                var row = new ModuleRow(def);
                var result = SettingsAssetUtility.FindAll(def.SettingsType, def.ResourceName);
                var inResourcesNamed = SettingsAssetUtility.FindInResourcesByName(def.SettingsType, def.ResourceName);

                row.CandidateCount = result.AllAssets.Count;

                if (inResourcesNamed.Count == 1)
                {
                    row.State = result.AllAssets.Count > 1 ? ModuleInstallState.MultipleFound : ModuleInstallState.Installed;
                }
                else if (inResourcesNamed.Count > 1)
                {
                    row.State = ModuleInstallState.MultipleFound;
                }
                else if (result.AssetsInResources.Count > 0)
                {
                    row.State = ModuleInstallState.OutsideResources;
                }
                else if (result.AssetsOutsideResources.Count > 0)
                {
                    row.State = ModuleInstallState.OutsideResources;
                }
                else
                {
                    row.State = ModuleInstallState.Missing;
                }

                _rows.Add(row);
            }
        }

        private void DrawRightPaneEmpty()
        {
            _rightPane.Clear();
            _rightPane.Add(new Label("Select a module from the left list.") { style = { paddingLeft = 8, paddingTop = 8 } });
        }

        private void DrawRightPane()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _rows.Count)
            {
                DrawRightPaneEmpty();
                return;
            }

            var def = _rows[_selectedIndex].Definition;
            var search = SettingsAssetUtility.FindAll(def.SettingsType, def.ResourceName);
            var installed = SettingsAssetUtility.FindInResourcesByName(def.SettingsType, def.ResourceName).ToList();

            _rightPane.Clear();
            _rightPane.style.paddingLeft = 10;
            _rightPane.style.paddingRight = 10;
            _rightPane.style.paddingTop = 10;

            _rightPane.Add(new Label(def.DisplayName)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 14
                }
            });

            var playMode = EditorApplication.isPlayingOrWillChangePlaymode;
            if (playMode)
            {
                _rightPane.Add(new HelpBox("Install/Move/Duplicate actions are disabled in Play Mode.", HelpBoxMessageType.Info));
            }

            if (installed.Count == 1)
            {
                DrawInstalled(def, installed[0], search, playMode);
                return;
            }

            if (installed.Count > 1)
            {
                DrawMultiple(def, installed.Cast<UnityEngine.Object>().ToList(), search, playMode);
                return;
            }

            if (search.AssetsInResources.Count > 0)
            {
                DrawOutside(def, search.AssetsInResources.ToList(), search, playMode);
                return;
            }

            if (search.AssetsOutsideResources.Count > 0)
            {
                DrawOutside(def, search.AssetsOutsideResources.ToList(), search, playMode);
                return;
            }

            DrawMissing(def, playMode);
        }

        private void DrawMissing(ModuleSettingsCatalog.ModuleSettingsDefinition def, bool playMode)
        {
            _rightPane.Add(new HelpBox("Settings asset was not found in any Resources folder. Click Install to create one.", HelpBoxMessageType.Warning));

            var install = new ToolbarButton(() =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                SettingsAssetUtility.CreateInResources(def.SettingsType, def.ResourceName);
                RefreshAll();
            })
            {
                text = "Install (Create in Assets/Resources)"
            };
            install.SetEnabled(!playMode);
            _rightPane.Add(install);
        }

        private void DrawInstalled(
            ModuleSettingsCatalog.ModuleSettingsDefinition def,
            UnityEngine.Object asset,
            SettingsAssetUtility.SettingsSearchResult search,
            bool playMode)
        {
            if (search.AllAssets.Count > 1)
            {
                _rightPane.Add(new HelpBox($"Multiple assets of the same Settings type were found (total: {search.AllAssets.Count}). Showing the one in Resources.", HelpBoxMessageType.Info));
            }

            _rightPane.Add(CreateAssetActions(asset));
            _rightPane.Add(new InspectorElement(asset));
        }

        private void DrawOutside(
            ModuleSettingsCatalog.ModuleSettingsDefinition def,
            List<UnityEngine.Object> outsideAssets,
            SettingsAssetUtility.SettingsSearchResult search,
            bool playMode)
        {
            var hasInResources = search.AssetsInResources.Count > 0;
            var message = hasInResources
                ? "A Settings asset exists in a Resources folder, but the file name does not match. Use Move/Duplicate to place it with the correct name."
                : "A Settings asset exists, but it is not in a Resources folder. Use Move/Duplicate to place it in Resources.";
            _rightPane.Add(new HelpBox(message, HelpBoxMessageType.Warning));

            if (_selectedCandidateAsset == null)
            {
                _selectedCandidateAsset = outsideAssets[0];
            }

            _rightPane.Add(CreateCandidatePicker(outsideAssets));
            _rightPane.Add(CreateMoveDuplicateActions(def, playMode));
            _rightPane.Add(CreateAssetActions(_selectedCandidateAsset));
            _rightPane.Add(new InspectorElement(_selectedCandidateAsset));
        }

        private void DrawMultiple(
            ModuleSettingsCatalog.ModuleSettingsDefinition def,
            List<UnityEngine.Object> installedAssets,
            SettingsAssetUtility.SettingsSearchResult search,
            bool playMode)
        {
            _rightPane.Add(new HelpBox("Multiple Settings assets with the same name exist in Resources. Please keep only one.", HelpBoxMessageType.Error));

            _rightPane.Add(CreateCandidatePicker(installedAssets));
            _rightPane.Add(CreateAssetActions(_selectedCandidateAsset ?? installedAssets[0]));
            _rightPane.Add(new InspectorElement(_selectedCandidateAsset ?? installedAssets[0]));
        }

        private VisualElement CreateCandidatePicker(List<UnityEngine.Object> candidates)
        {
            var container = new VisualElement();
            container.style.marginTop = 8;
            container.style.marginBottom = 8;

            container.Add(new Label("Candidates") { style = { unityFontStyleAndWeight = FontStyle.Bold } });

            var choices = candidates.Select(a => $"{a.name}  ({AssetDatabase.GetAssetPath(a)})").ToList();
            var currentIndex = Mathf.Max(0, candidates.IndexOf(_selectedCandidateAsset));
            var popup = new PopupField<string>(choices, currentIndex);
            popup.RegisterValueChangedCallback(evt =>
            {
                var idx = choices.IndexOf(evt.newValue);
                if (idx >= 0 && idx < candidates.Count)
                {
                    _selectedCandidateAsset = candidates[idx];
                    DrawRightPane();
                }
            });
            container.Add(popup);
            return container;
        }

        private VisualElement CreateMoveDuplicateActions(ModuleSettingsCatalog.ModuleSettingsDefinition def, bool playMode)
        {
            var toolbar = new Toolbar();
            toolbar.style.marginTop = 8;
            toolbar.style.marginBottom = 8;

            var move = new ToolbarButton(() =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!SettingsAssetUtility.MoveToResources(_selectedCandidateAsset, def.ResourceName, out var error))
                {
                    EditorUtility.DisplayDialog("Move failed", error, "OK");
                    return;
                }
                RefreshAll();
            })
            {
                text = "Move to Resources"
            };
            move.SetEnabled(!playMode);
            move.style.marginRight = 8;

            var duplicate = new ToolbarButton(() =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!SettingsAssetUtility.DuplicateToResources(_selectedCandidateAsset, def.ResourceName, out var error))
                {
                    EditorUtility.DisplayDialog("Duplicate failed", error, "OK");
                    return;
                }
                RefreshAll();
            })
            {
                text = "Duplicate to Resources"
            };
            duplicate.SetEnabled(!playMode);

            toolbar.Add(move);
            toolbar.Add(duplicate);
            return toolbar;
        }

        private VisualElement CreateAssetActions(UnityEngine.Object asset)
        {
            var toolbar = new Toolbar();
            toolbar.style.marginTop = 8;
            toolbar.style.marginBottom = 8;

            var select = new ToolbarButton(() =>
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            })
            {
                text = "Select"
            };

            var reveal = new ToolbarButton(() =>
            {
                var path = AssetDatabase.GetAssetPath(asset);
                if (!string.IsNullOrEmpty(path))
                {
                    EditorUtility.RevealInFinder(path);
                }
            })
            {
                text = "Open Folder"
            };

            select.style.marginRight = 0;
            reveal.style.marginRight = 0;

            toolbar.Add(select);
            toolbar.Add(reveal);
            return toolbar;
        }
    }
}

