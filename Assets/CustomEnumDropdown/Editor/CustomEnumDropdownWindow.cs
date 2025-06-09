using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using static CustomEnumDropdown.CustomEnumDropdownAttribute;
using CObject = System.Object;

namespace CustomEnumDropdown.Editor
{
    /// <summary>
    /// 自定义枚举下拉选择框，可搜索，按字母、枚举值顺序显示。
    /// </summary>
    public partial class CustomEnumDropdownWindow : EditorWindow
    {
        #region 对外方法

        /// <summary>
        /// 初始化，需在Show之前调用
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="enumType"></param>
        /// <param name="showSequenceType"></param>
        public void Init(SerializedProperty sp, Type enumType, ShowSequenceType showSequenceType, Action onCloseWindow)
        {
            m_onCloseWindow = onCloseWindow;
            m_sp = sp;
            m_enumShowSequence = showSequenceType;
            m_sortedEnumInfoList.Clear();
            var enumNameList = ListPool<string>.Get();
            enumNameList.AddRange(Enum.GetNames(enumType));
            enumNameList.Sort((n1, n2) => ((int)Enum.Parse(enumType, n1)).CompareTo((int)Enum.Parse(enumType, n2)));
            for (int i = 0; i < enumNameList.Count; ++i)
            {
                var enumInfo = new EnumInfo()
                {
                    m_index = i,
                    m_name = enumNameList[i],
                    m_isSelect = m_sp.enumValueIndex == i,
                    m_value = (int)Enum.Parse(enumType, enumNameList[i]),
                };
                m_sortedEnumInfoList.Add(enumInfo);
            }
            ListPool<string>.Release(enumNameList);
            EnumShowSequenceUpdate();
            UpdateShownEnumList(string.Empty, false, false);
        }

        #endregion 对外方法

        #region 内部实现

        private void CreateGUI()
        {
            this.rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDownEvent, TrickleDown.TrickleDown);
            var root = m_visualTreeAsset.Instantiate();
            root.style.flexGrow = 1;
            this.rootVisualElement.Add(root);

            // 搜索框
            m_toolbarSearchField = this.rootVisualElement.Q<ToolbarSearchField>();
            m_toolbarSearchField.RegisterValueChangedCallback(OnSearchFieldValueChanged);
            m_toolbarSearchField.schedule.Execute(() =>
            {
                m_toolbarSearchField.Focus();
            });

            // 顺序显示按钮
            m_enumSortMenuButton = this.rootVisualElement.Q<ToolbarMenu>();
            m_enumSortMenuButton.text = m_enumShowSequence == ShowSequenceType.EnumVal ? SortSequenceNameVal : SortSequenceNameAlphabet;
            m_enumSortMenuButton.menu.AppendAction(SortSequenceNameVal, delegate { OnEnumSortButtonClicked(ShowSequenceType.EnumVal); });
            m_enumSortMenuButton.menu.AppendAction(SortSequenceNameAlphabet, delegate { OnEnumSortButtonClicked(ShowSequenceType.Alphabet); });

            // 列表
            m_enumListView = this.rootVisualElement.Q<ListView>();
            m_enumListView.selectionType = SelectionType.Single;
            m_enumListView.onItemsChosen += OnEnumListViewItemsChosen;
            m_enumListView.itemsSource = m_shownEnumInfoList;
            m_enumListView.bindItem = OnEnumListBindItem;
            m_enumListView.makeItem = OnEnumListMakeItem;
            // 初始使选择在列表中可见
            // Unity Bug：UUM-33784，此时ScrollToItem失效
            //Debug.Log("CreateGUI");
            //var selectedEnumIndexInShownList = m_shownEnumInfoList.FindIndex(t => t.m_isSelect);
            //if (selectedEnumIndexInShownList >= 0)
            //{
            //    m_listView.ScrollToItem(selectedEnumIndexInShownList);
            //    Debug.Log($"ScrollTo index {selectedEnumIndexInShownList}");
            //}
            m_enumListView.schedule.Execute(() =>
            {
                var selectedEnumIndexInShownList = m_shownEnumInfoList.FindIndex(t => t.m_isSelect);
                if (selectedEnumIndexInShownList >= 0)
                {
                    m_enumListView.ScrollToItem(selectedEnumIndexInShownList);
                    m_enumListView.selectedIndex = selectedEnumIndexInShownList;
                }
            });
        }

        private void OnDisable()
        {
            m_onCloseWindow?.Invoke();
            m_onCloseWindow = null;
        }

        /// <summary>
        /// 枚举显示顺序更新，注意这里不刷新枚举显示列表
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void EnumShowSequenceUpdate()
        {
            switch (m_enumShowSequence)
            {
                case ShowSequenceType.EnumVal:
                    m_sortedEnumInfoList.Sort((t1, t2) => t1.m_value.CompareTo(t2.m_value));
                    break;

                case ShowSequenceType.Alphabet:
                    m_sortedEnumInfoList.Sort((t1, t2) => String.Compare(t1.m_name, t2.m_name, StringComparison.OrdinalIgnoreCase));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// KeyDownEvent处理
        /// </summary>
        /// <param name="keyDownEvent"></param>
        private void OnKeyDownEvent(KeyDownEvent keyDownEvent)
        {
            // 焦点在ListView上，让其自行处理
            if (m_enumListView.focusController.focusedElement == m_enumListView)
            {
                return;
            }

            if (keyDownEvent.keyCode == KeyCode.UpArrow)
            {
                int upSelectedIndex = m_enumListView.selectedIndex - 1;
                if (upSelectedIndex < 0)
                {
                    upSelectedIndex = m_shownEnumInfoList.Count - 1;
                }
                m_enumListView.selectedIndex = upSelectedIndex;
                m_enumListView.ScrollToItem(m_enumListView.selectedIndex);
                keyDownEvent.StopPropagation();
                return;
            }

            if (keyDownEvent.keyCode == KeyCode.DownArrow)
            {
                int downSelectedIndex = m_enumListView.selectedIndex + 1;
                if (downSelectedIndex >= m_shownEnumInfoList.Count)
                {
                    downSelectedIndex = 0;
                }
                m_enumListView.selectedIndex = downSelectedIndex;
                m_enumListView.ScrollToItem(m_enumListView.selectedIndex);
                keyDownEvent.StopPropagation();
                return;
            }

            if (keyDownEvent.keyCode != KeyCode.Return && keyDownEvent.keyCode != KeyCode.KeypadEnter)
            {
                return;
            }

            int selectedIndex = m_enumListView.selectedIndex;
            if (selectedIndex >= 0 && selectedIndex < m_shownEnumInfoList.Count)
            {
                m_sp.enumValueIndex = m_shownEnumInfoList[selectedIndex].m_index;
                m_sp.serializedObject.ApplyModifiedProperties();
                Close();
            }
        }

        /// <summary>
        /// 枚举列表Item确认选中回调
        /// </summary>
        /// <param name="indexEnumerable"></param>
        private void OnEnumListViewItemsChosen(IEnumerable<CObject> indexEnumerable)
        {
            int index = m_enumListView.selectedIndex;
            if (m_shownEnumInfoList == null || index >= m_shownEnumInfoList.Count)
            {
                return;
            }

            m_sp.enumValueIndex = m_shownEnumInfoList[index].m_index;
            m_sp.serializedObject.ApplyModifiedProperties();
            Close();
        }

        /// <summary>
        /// 枚举显示顺序按钮点击
        /// </summary>
        private void OnEnumSortButtonClicked(ShowSequenceType showSequence)
        {
            m_enumShowSequence = showSequence;
            m_enumSortMenuButton.text = m_enumShowSequence == ShowSequenceType.EnumVal ? SortSequenceNameVal : SortSequenceNameAlphabet;
            EnumShowSequenceUpdate();
            UpdateShownEnumList(m_toolbarSearchField.value, true, true);
            m_enumListView.Focus();
        }

        /// <summary>
        /// 枚举列表MakeItem
        /// </summary>
        /// <returns></returns>
        private VisualElement OnEnumListMakeItem()
        {
            var ret = new CustomEnumDropdownItemVisualElement(m_ListItemTreeAsset);
            ret.RegisterCallback<MouseUpEvent>(OnListViewItemMouseUpEvent);
            return ret;
        }

        /// <summary>
        /// 枚举列表Item鼠标按钮Up事件
        /// </summary>
        /// <param name="mouseUpEvent"></param>
        private void OnListViewItemMouseUpEvent(MouseUpEvent mouseUpEvent)
        {
            if (mouseUpEvent.button != 0)
            {
                return;
            }

            var currentTarget = (VisualElement)mouseUpEvent.currentTarget;
            if (currentTarget.userData is not EnumInfo enumInfo)
            {
                return;
            }

            m_sp.enumValueIndex = enumInfo.m_index;
            m_sp.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(m_sp.serializedObject.targetObject);
            Close();
        }

        /// <summary>
        /// 枚举列表BindItem
        /// </summary>
        /// <param name="visualElement"></param>
        /// <param name="index"></param>
        private void OnEnumListBindItem(VisualElement visualElement, int index)
        {
            if (visualElement is not CustomEnumDropdownItemVisualElement customEnumDropdownItem)
            {
                return;
            }
            customEnumDropdownItem.UpdateInfo(m_shownEnumInfoList[index]);
        }

        /// <summary>
        /// 搜索内容改变
        /// </summary>
        /// <param name="evt"></param>
        private void OnSearchFieldValueChanged(ChangeEvent<string> evt)
        {
            UpdateShownEnumList(evt.newValue, true, true);
        }

        /// <summary>
        /// 更新显示的枚举列表
        /// </summary>
        /// <param name="searchStr"></param>
        /// <param name="listViewRefreshItems">ListView刷新</param>
        /// <param name="scrollToSelectedItem">ListView执行ScrollToIndex到选中的</param>
        private void UpdateShownEnumList(string searchStr, bool listViewRefreshItems, bool scrollToSelectedItem)
        {
            m_shownEnumInfoList.Clear();

            bool needCheckEnumVal = !string.IsNullOrEmpty(searchStr);
            foreach (var enumInfo in m_sortedEnumInfoList)
            {
                if (needCheckEnumVal && !enumInfo.m_name.ToLower().Contains(searchStr.ToLower()))
                {
                    continue;
                }
                m_shownEnumInfoList.Add(enumInfo);
            }

            if (m_enumListView == null)
            {
                return;
            }

            if (needCheckEnumVal)
            {
                m_enumListView.selectedIndex = 0;
            }
            else
            {
                m_enumListView.selectedIndex = m_shownEnumInfoList.FindIndex(t => t.m_isSelect);
            }

            if (listViewRefreshItems)
            {
                m_enumListView.RefreshItems();
            }

            if (scrollToSelectedItem)
            {
                m_enumListView.ScrollToItem(m_enumListView.selectedIndex < 0 ? 0 : m_enumListView.selectedIndex);
            }
        }

        #endregion 内部实现

        #region 事件


        #endregion 事件

        #region 字段

        [SerializeField]
        private VisualTreeAsset m_visualTreeAsset = default;

        [SerializeField]
        private VisualTreeAsset m_ListItemTreeAsset = default;

        /// <summary>
        /// 搜索框UI
        /// </summary>
        private ToolbarSearchField m_toolbarSearchField;

        /// <summary>
        /// 枚举列表UI
        /// </summary>
        private ListView m_enumListView;

        private ToolbarMenu m_enumSortMenuButton;

        /// <summary>
        /// 枚举赋值SerializedProperty
        /// </summary>
        private SerializedProperty m_sp;

        /// <summary>
        /// 枚举显示顺序
        /// </summary>
        private ShowSequenceType m_enumShowSequence;

        /// <summary>
        /// 排序过的枚举信息列表
        /// </summary>
        private List<EnumInfo> m_sortedEnumInfoList = new();

        /// <summary>
        /// 显示的枚举信息列表
        /// </summary>
        private List<EnumInfo> m_shownEnumInfoList = new();

        /// <summary>
        /// 关闭窗口回调
        /// </summary>
        public Action m_onCloseWindow;

        #endregion 字段

        #region 常量 & 定义

        private const string SortSequenceNameVal = "值顺序";

        private const string SortSequenceNameAlphabet = "字母序";

        #endregion 常量 & 定义
    }
}
