using UnityEngine.UIElements;

namespace CustomEnumDropdown.Editor
{
    /// <summary>
    /// 自定义枚举下拉选择框ListView Item
    /// </summary>
    public class CustomEnumDropdownItemVisualElement : VisualElement
    {
        public CustomEnumDropdownItemVisualElement(VisualTreeAsset visualTreeAsset)
        {
            visualTreeAsset.CloneTree(this);
            m_checkMark = this.Q<VisualElement>("CheckMark");
            m_enumName = this.Q<Label>("EnumName");
            m_enumValue = this.Q<Label>("EnumValue");
        }

        public void UpdateInfo(EnumInfo enumInfo)
        {
            this.userData = enumInfo;
            m_enumName.text = enumInfo.m_name;
            m_enumValue.text = enumInfo.m_value.ToString();
            m_checkMark.visible = enumInfo.m_isSelect;
        }

        private VisualElement m_checkMark;
        private Label m_enumName;
        private Label m_enumValue;
    }
}
