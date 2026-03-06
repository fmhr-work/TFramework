using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace TFramework.MasterData.Editor
{
    /// <summary>
    /// MasterDataViewer
    /// </summary>
    public class MasterDataViewerWindow : EditorWindow
    {
        private MasterDataSettings _settings;
        private ListView _containerListView;
        private VisualElement _rightPane;
        private ListView _dataListView;
        private VisualElement _headerRow;
        private List<ScriptableObject> _loadedContainers = new List<ScriptableObject>();
        private ScriptableObject _selectedContainer;
        private List<object> _currentDataList = new List<object>();
        
        private string _searchText = "";
        private const float CellWidth = 120f;
        
        // リフレクション情報のキャッシュ
        private Type _itemType;
        private FieldInfo[] _fields;
        
        // ソート状態
        private FieldInfo _currentSortField;
        private bool _isSortAscending = true;

        [MenuItem("TFramework/MasterData/Viewer")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<MasterDataViewerWindow>();
            wnd.titleContent = new GUIContent("MasterData Viewer");
            wnd.minSize = new Vector2(800, 600);
        }

        private void CreateGUI()
        {
            LoadSettings();

            var root = rootVisualElement;

            // 分割ビュー
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            root.Add(splitView);

            // 左：マスターデータ型のリスト
            var leftPane = new VisualElement
            {
                style =
                {
                    flexGrow = 1
                }
            };
            splitView.Add(leftPane);

            var leftHeader = new Label("MasterData Types")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    paddingLeft = 5,
                    paddingTop = 5,
                    paddingBottom = 5,
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f)
                }
            };
            leftPane.Add(leftHeader);

            _containerListView = new ListView
            {
                style =
                {
                    flexGrow = 1
                },
                makeItem = () => new Label(),
                bindItem = (e, i) => 
                {
                    var container = _loadedContainers[i];
                    ((Label)e).text = container != null ? container.GetType().Name.Replace("Container", "") : "null";
                }
            };
            _containerListView.selectionChanged += OnContainerSelected;
            _containerListView.itemsSource = _loadedContainers;
            leftPane.Add(_containerListView);

            // 右：テーブルビュー
            _rightPane = new VisualElement
            {
                style =
                {
                    flexGrow = 1
                }
            };
            splitView.Add(_rightPane);

            // Toolbar
            var toolbar = new Toolbar();
            
            var searchField = new ToolbarSearchField
            {
                style =
                {
                    width = 200
                }
            };
            searchField.RegisterValueChangedCallback(evt => 
            {
                _searchText = evt.newValue;
                RefreshFilteredData();
            });
            toolbar.Add(searchField);
            
            var refreshBtn = new ToolbarButton(() => 
            {
                LoadSettings(); 
                _containerListView.Rebuild();
                RefreshContainerData();
            })
            {
                text = "Refresh"
            };
            toolbar.Add(refreshBtn);



            _rightPane.Add(toolbar);

            // Table Header
            _headerRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    height = 25,
                    backgroundColor = new Color(0.3f, 0.3f, 0.3f),
                    overflow = Overflow.Hidden
                }
            };
            _rightPane.Add(_headerRow);

            // Table Data ListView
            _dataListView = new ListView
            {
                style =
                {
                    flexGrow = 1
                },
                fixedItemHeight = 25,
                makeItem = MakeRowItem,
                bindItem = BindRowItem,
                itemsSource = _currentDataList,
                selectionType = SelectionType.None
            };
            _rightPane.Add(_dataListView);

            LoadContainers();
        }

        private void LoadSettings()
        {
            _settings = Resources.Load<MasterDataSettings>("MasterDataSettings");
            if (_settings == null)
            {
                var guids = AssetDatabase.FindAssets("t:MasterDataSettings");
                if (guids.Length > 0)
                {
                    _settings = AssetDatabase.LoadAssetAtPath<MasterDataSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
        }

        private void LoadContainers()
        {
            _loadedContainers.Clear();
            if (_settings != null && _settings.Containers != null)
            {
                 _loadedContainers.AddRange(_settings.Containers.Where(c => c != null));
            }
            if (_loadedContainers.Count == 0)
            {
                var guids = AssetDatabase.FindAssets("t:ScriptableObject");
                foreach (var guid in guids)
                {
                    var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                    if(obj != null && obj.GetType().Name.EndsWith("Container"))
                    {
                        _loadedContainers.Add(obj);
                    }
                }
            }
            // Sort by name
             _loadedContainers.Sort((a,b) => a.name.CompareTo(b.name));
            if (_containerListView != null) _containerListView.Rebuild();
        }

        private void OnContainerSelected(IEnumerable<object> selection)
        {
            _selectedContainer = selection.FirstOrDefault() as ScriptableObject;
            RefreshContainerData();
        }

        private void RefreshContainerData()
        {
            _currentSortField = null;
            _isSortAscending = true;

            if (_selectedContainer == null) return;

            var containerType = _selectedContainer.GetType();
            var allProp = containerType.GetProperty("All");
            if (allProp == null) return;

            var list = allProp.GetValue(_selectedContainer) as System.Collections.IList;
            if (list == null || list.Count == 0) 
            {
                _currentDataList.Clear();
                _dataListView.Rebuild();
                return;
            }

            _itemType = list[0].GetType();
            _fields = _itemType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            RefreshFilteredData(list);
            RebuildHeader();
        }

        private void RefreshFilteredData(System.Collections.IList sourceList = null)
        {
            _currentDataList.Clear();
            
            // ソースが提供されていないがコンテナが選択されている場合、ソースを再取得
            if (sourceList == null && _selectedContainer != null)
            {
                var containerType = _selectedContainer.GetType();
                var allProp = containerType.GetProperty("All");
                if (allProp != null)
                {
                    sourceList = allProp.GetValue(_selectedContainer) as System.Collections.IList;
                }
            }

            if (sourceList != null)
            {
                if (string.IsNullOrEmpty(_searchText))
                {
                    foreach(var item in sourceList) _currentDataList.Add(item);
                }
                else
                {
                    foreach(var item in sourceList)
                    {
                        if (ItemMatches(item, _searchText))
                        {
                            _currentDataList.Add(item);
                        }
                    }
                }
            }
            
            if (_currentSortField != null) ApplySort();
            _dataListView.Rebuild();
        }

        private VisualElement MakeRowItem()
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };
            return row;
        }

        private void BindRowItem(VisualElement row, int index)
        {
            row.Clear();
            if (index >= _currentDataList.Count) return;
            var item = _currentDataList[index];

            if (_fields == null) return;

            foreach (var field in _fields)
            {
                var cell = CreateCell(field, item);
                cell.style.width = CellWidth;
                cell.style.borderRightWidth = 1;
                cell.style.borderRightColor = new Color(0.2f, 0.2f, 0.2f);
                cell.style.overflow = Overflow.Hidden;
                row.Add(cell);
            }
        }

        private void RebuildHeader()
        {
            _headerRow.Clear();
            if (_fields == null) return;

            foreach (var field in _fields)
            {
                string text = field.Name;
                if (_currentSortField == field)
                {
                    text += _isSortAscending ? " ▲" : " ▼";
                }

                var label = new Label(text)
                {
                    style =
                    {
                        width = CellWidth,
                        unityTextAlign = TextAnchor.MiddleLeft,
                        paddingLeft = 5,
                        borderRightWidth = 1,
                        borderRightColor = Color.black
                    }
                };

                label.RegisterCallback<ClickEvent>(_ => 
                {
                    if (_currentSortField == field)
                    {
                        _isSortAscending = !_isSortAscending;
                    }
                    else
                    {
                        _currentSortField = field;
                        _isSortAscending = true;
                    }
                    ApplySort();
                    RebuildHeader();
                    _dataListView.Rebuild();
                });

                _headerRow.Add(label);
            }
        }

        private void ApplySort()
        {
             if (_currentSortField == null) return;
             
             _currentDataList.Sort((a, b) => 
             {
                 var valA = _currentSortField.GetValue(a);
                 var valB = _currentSortField.GetValue(b);
                 
                 int result;
                 if (valA == null && valB == null) result = 0;
                 else if (valA == null) result = -1;
                 else if (valB == null) result = 1;
                 else if (valA is IComparable compA)
                 {
                     result = compA.CompareTo(valB);
                 }
                 else
                 {
                     result = valA.ToString().CompareTo(valB.ToString());
                 }
                 
                 return _isSortAscending ? result : -result;
             });
        }



        private bool ItemMatches(object item, string search)
        {
            foreach (var field in _fields)
            {
                var val = field.GetValue(item);
                if (val != null && val.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private VisualElement CreateCell(FieldInfo field, object item)
        {
            var val = field.GetValue(item);
            var type = field.FieldType;

            VisualElement cellWrapper = new VisualElement
            {
                style =
                {
                    justifyContent = Justify.Center
                }
            };

            if (type == typeof(bool))
            {
                var toggle = new Toggle
                {
                    value = (bool)val
                };
                toggle.RegisterValueChangedCallback(evt => 
                {
                    field.SetValue(item, evt.newValue);
                    EditorUtility.SetDirty(_selectedContainer);
                });
                // ブール値の中央揃え
                toggle.style.alignSelf = Align.Center;
                cellWrapper.Add(toggle);
            }
            else if (type == typeof(int) || type == typeof(long))
            {
                var fieldInt = new IntegerField
                {
                    value = Convert.ToInt32(val)
                };
                fieldInt.RegisterValueChangedCallback(evt => 
                {
                     try{
                        field.SetValue(item, Convert.ChangeType(evt.newValue, type));
                        EditorUtility.SetDirty(_selectedContainer);
                     }catch{}
                });
                cellWrapper.Add(fieldInt);
            }
            else if (type == typeof(float))
            {
                var fieldFloat = new FloatField
                {
                    value = (float)val
                };
                fieldFloat.RegisterValueChangedCallback(evt => 
                {
                    field.SetValue(item, evt.newValue);
                    EditorUtility.SetDirty(_selectedContainer);
                });
                cellWrapper.Add(fieldFloat);
            }
            else if (type.IsEnum)
            {
                var fieldEnum = new EnumField((Enum)val);
                fieldEnum.RegisterValueChangedCallback(evt => 
                {
                    field.SetValue(item, evt.newValue);
                    EditorUtility.SetDirty(_selectedContainer);
                });
                cellWrapper.Add(fieldEnum);
            }
            else
            {
                var fieldStr = new TextField
                {
                    value = val?.ToString() ?? ""
                };
                fieldStr.RegisterValueChangedCallback(evt => 
                {
                    if (type == typeof(string))
                    {
                        field.SetValue(item, evt.newValue);
                        EditorUtility.SetDirty(_selectedContainer);
                    }
                });
                if (type != typeof(string)) fieldStr.isReadOnly = true;
                cellWrapper.Add(fieldStr);
            }

            return cellWrapper;
        }
    }
}

