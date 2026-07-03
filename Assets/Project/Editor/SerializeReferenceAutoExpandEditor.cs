using UnityEditor;
using UnityEngine;

namespace Project.Scripts.Editor
{
    /// <summary>
    /// MonoBehaviour の Inspector 描画時に SerializeReference フィールドを自動展開する。
    /// finishedDefaultHeaderGUI では MonoBehaviour に対して呼ばれないケースがあるため、
    /// CustomEditor で OnInspectorGUI をオーバーライドして対応する。
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    [CanEditMultipleObjects]
    public class SerializeReferenceAutoExpandEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            ExpandManagedReferences(serializedObject);
            base.OnInspectorGUI();
        }

        internal static void ExpandManagedReferences(SerializedObject so)
        {
            so.Update();

            var iterator = so.GetIterator();
            bool enterChildren = true;
            bool changed = false;

            while (iterator.NextVisible(enterChildren))
            {
                if (iterator.propertyType == SerializedPropertyType.ManagedReference)
                {
                    if (!string.IsNullOrEmpty(iterator.managedReferenceFullTypename)
                        && !iterator.isExpanded)
                    {
                        iterator.isExpanded = true;
                        changed = true;
                    }

                    enterChildren = false;
                }
                else
                {
                    enterChildren = true;
                }
            }

            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    [CustomEditor(typeof(ScriptableObject), true)]
    [CanEditMultipleObjects]
    public class SerializeReferenceAutoExpandScriptableObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            SerializeReferenceAutoExpandEditor.ExpandManagedReferences(serializedObject);
            base.OnInspectorGUI();
        }
    }
}
