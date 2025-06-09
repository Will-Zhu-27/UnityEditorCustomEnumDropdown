using System.Diagnostics;
using UnityEngine;

namespace CustomEnumDropdown
{
    /// <summary>
    /// 自定义编辑器枚举下拉框，支持搜索、滚轮滑动、按枚举值排序、字母排序
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public class CustomEnumDropdownAttribute : PropertyAttribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="labelName"></param>
        /// <param name="showSequence"></param>
        public CustomEnumDropdownAttribute(string labelName = "", ShowSequenceType showSequence = ShowSequenceType.EnumVal)
        {
            LabelName = labelName;
            ShowSequence = showSequence;
        }

        public string LabelName { get; private set; }

        public ShowSequenceType ShowSequence { get; private set; }

        /// <summary>
        /// 显示顺序
        /// </summary>
        public enum ShowSequenceType
        {
            /// <summary>
            /// 枚举值顺序
            /// </summary>
            EnumVal,

            /// <summary>
            /// 字母顺序
            /// </summary>
            Alphabet,
        }
    }
}