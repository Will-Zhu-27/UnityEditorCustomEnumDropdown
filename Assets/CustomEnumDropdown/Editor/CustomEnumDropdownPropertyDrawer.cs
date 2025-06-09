using UnityEditor;
using UnityEngine;

namespace CustomEnumDropdown.Editor
{
    [CustomPropertyDrawer(typeof(CustomEnumDropdownAttribute))]
    public class CustomEnumDropdownPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var customEnumDropdownAttribute = (CustomEnumDropdownAttribute)this.attribute;

            // 标签名设置
            if (!string.IsNullOrEmpty(customEnumDropdownAttribute.LabelName))
            {
                label.text = customEnumDropdownAttribute.LabelName;
            }

            string enumString;
            if (property.enumValueIndex >= 0 && property.enumValueIndex < property.enumDisplayNames.Length)
            {
                enumString = property.enumDisplayNames[property.enumValueIndex];
            }
            else
            {
                enumString = "-";
            }

            EditorGUI.BeginProperty(position, label, property);

            var enumFieldRect = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            var enumFieldStyle = new GUIStyle(EditorStyles.popup);
            if (GUI.Button(enumFieldRect, enumString, enumFieldStyle))
            {
                m_oldValBeforeDropdown = property.enumValueIndex;

                var windowRect = new Rect(enumFieldRect)
                {
                    position = GUIUtility.GUIToScreenPoint(enumFieldRect.position),
                };
                var customEnumDropdownWindow = CustomEnumDropdownWindow.CreateInstance<CustomEnumDropdownWindow>();
                customEnumDropdownWindow.Init(property, fieldInfo.FieldType, customEnumDropdownAttribute.ShowSequence, delegate { m_needCheckChange = true; });
                customEnumDropdownWindow.ShowAsDropDown(windowRect, new Vector2(windowRect.width, 400));
            }

            EditorGUI.EndProperty();

            // 为了触发EditorGUI.EndChangeCheck方便Inspector扩展
            if (m_needCheckChange && m_oldValBeforeDropdown != property.enumValueIndex)
            {
                m_needCheckChange= false;
                GUI.changed = true;
            }
        }

        private bool m_needCheckChange;

        private int m_oldValBeforeDropdown;
    }
}
