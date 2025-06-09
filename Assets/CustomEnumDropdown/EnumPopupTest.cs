using CustomEnumDropdown;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnumPopupTest : MonoBehaviour
{
    [CustomEnumDropdown("Dropdown Key", CustomEnumDropdownAttribute.ShowSequenceType.Alphabet)]
    public KeyCode m_keyCode;
    
    public KeyCode m_keyCode2;
}

#if UNITY_EDITOR

[CustomEditor(typeof(EnumPopupTest))]
public class EnumPopupTestInspector : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();

        if (EditorGUI.EndChangeCheck()) 
        {
            Debug.Log("Change");
        }
    }
}

#endif