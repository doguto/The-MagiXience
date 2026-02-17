using System.Collections.Generic;
using UnityEditor;

namespace Project.Scripts.Editor
{
    [InitializeOnLoad]
    public static class SerializeReferenceAutoExpandEditor
    {
        private static readonly HashSet<string> _initialized = new HashSet<string>();

        static SerializeReferenceAutoExpandEditor()
        {
            Selection.selectionChanged += () => _initialized.Clear();
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnHeaderGUI;
        }

        private static void OnHeaderGUI(UnityEditor.Editor editor)
        {
            var so = editor.serializedObject;
            int instanceId = so.targetObject.GetInstanceID();
            string editorKey = $"{instanceId}";

            if (!_initialized.Add(editorKey))
                return;

            var iterator = so.GetIterator();
            bool enterChildren = true;
            bool changed = false;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = true;

                if (iterator.propertyType == SerializedPropertyType.ManagedReference
                    && !string.IsNullOrEmpty(iterator.managedReferenceFullTypename)
                    && !iterator.isExpanded)
                {
                    iterator.isExpanded = true;
                    changed = true;
                }
            }

            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
