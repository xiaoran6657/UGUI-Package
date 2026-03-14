using Test;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(DebugInventoryTester))]
    public class DebugInventoryTesterEditor : UnityEditor.Editor
    {
        // 本地缓存输入框的值（不会丢失 Play 模式中的运行）
        private int _inputItemId;
        private int _inputCount;
        private int _inputSlotIndex;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Run-time Inspector Tester (Play Mode Only)", EditorStyles.boldLabel);

            // 显示/使用默认值（当选择对象时从目标读取默认值以便便捷）
            if (!target) return;
            
            var t = (DebugInventoryTester)target;
            _inputItemId = EditorGUILayout.IntField("Item ID", Mathf.Max(0, _inputItemId == 0 ? t.testItemId : _inputItemId));
            _inputCount = EditorGUILayout.IntField("Count", Mathf.Max(0, _inputCount == 0 ? t.testCount : _inputCount));
            _inputSlotIndex = EditorGUILayout.IntField("Slot Index", Mathf.Max(0, _inputSlotIndex == 0 ? t.testSlotIndex : _inputSlotIndex));

            EditorGUILayout.Space();

            GUI.enabled = Application.isPlaying; // 仅在 Play 模式可点击
            if (GUILayout.Button("Add Item (Inspector)"))
            {
                t.AddItemById_FromInspector(_inputItemId, _inputCount);
            }

            if (GUILayout.Button("Set Slot (Inspector)"))
            {
                t.SetSlot_FromInspector(_inputSlotIndex, _inputItemId, _inputCount);
            }

            if (GUILayout.Button("Clear Slot (Inspector)"))
            {
                t.ClearSlot_FromInspector(_inputSlotIndex);
            }

            if (GUILayout.Button("Add Default Values"))
            {
                t.AddDefault();
            }
            
            if (GUILayout.Button("Sort"))
            {
                t.SortInventory_FromInspector();
            }

            GUI.enabled = true;

            // 小提示
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("These buttons work only in Play mode. Enter Play mode to test.", MessageType.Info);
            }
        }
    }
}