using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace NaughtyAttributes.Editor
{
    [CustomPropertyDrawer(typeof(AnimatorStateAttribute))]
    public class AnimatorStatePropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            AnimatorStateAttribute attr = (AnimatorStateAttribute)attribute;

            if (property.propertyType != SerializedPropertyType.Generic ||
                property.type != nameof(AnimationStateInfo))
            {
                EditorGUI.HelpBox(position, "[AnimatorState] can only be used on AnimationStateInfo fields", MessageType.Warning);
                return;
            }

            SerializedProperty nameProp = property.FindPropertyRelative("Name");
            SerializedProperty layerProp = property.FindPropertyRelative("Layer");

            Rect labelRect = EditorGUI.PrefixLabel(position, label);
            float half = labelRect.width * 0.5f;
            Rect layerRect = new Rect(labelRect.x, labelRect.y, half - 4, labelRect.height);
            Rect stateRect = new Rect(labelRect.x + half + 4, labelRect.y, half - 4, labelRect.height);

            Animator animator = FindAnimator(property, attr.AnimatorFieldName);
            AnimatorController controller = null;
            if (animator != null)
            {
                if (animator.runtimeAnimatorController is AnimatorController direct)
                {
                    controller = direct;
                }
                else if (animator.runtimeAnimatorController is AnimatorOverrideController overrideController)
                {
                    controller = overrideController.runtimeAnimatorController as AnimatorController;
                }
            }

            string[] layerOptions;
            if (controller != null)
            {
                var layers = controller.layers;
                layerOptions = new string[layers.Length + 1];
                layerOptions[0] = "(None)";
                for (int i = 0; i < layers.Length; i++)
                    layerOptions[i + 1] = layers[i].name;
            }
            else
            {
                layerOptions = new[] { "(No Animator)" };
            }

            int currentLayer = layerProp.intValue;
            int layerIndex = Mathf.Clamp(currentLayer + 1, 0, layerOptions.Length - 1);
            int newLayerIndex = EditorGUI.Popup(layerRect, layerIndex, layerOptions) - 1;
            layerProp.intValue = Mathf.Max(newLayerIndex, -1);

            string[] stateOptions;
            if (controller != null && newLayerIndex >= 0)
            {
                var stateMachine = controller.layers[newLayerIndex].stateMachine;
                var states = stateMachine.states;
                stateOptions = new string[states.Length + 1];
                stateOptions[0] = "(None)";
                for (int i = 0; i < states.Length; i++)
                    stateOptions[i + 1] = states[i].state.name;
            }
            else
            {
                stateOptions = new[] { "(No State)" };
            }

            string currentName = nameProp.stringValue;
            int stateIndex = 0;
            for (int i = 1; i < stateOptions.Length; i++)
                if (stateOptions[i] == currentName)
                    stateIndex = i;
            int newStateIndex = EditorGUI.Popup(stateRect, stateIndex, stateOptions);
            nameProp.stringValue = newStateIndex > 0 ? stateOptions[newStateIndex] : "";
        }

        private Animator FindAnimator(SerializedProperty property, string animatorFieldName)
        {
            object target = property.serializedObject.targetObject;
            Type type = target.GetType();

            FieldInfo field = type.GetField(animatorFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && typeof(Animator).IsAssignableFrom(field.FieldType))
                return field.GetValue(target) as Animator;

            PropertyInfo prop = type.GetProperty(animatorFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && typeof(Animator).IsAssignableFrom(prop.PropertyType))
                return prop.GetValue(target) as Animator;

            return null;
        }

        private object GetParentObject(object root, string path)
        {
            string[] elements = path.Split('.');
            object obj = root;

            foreach (string element in elements)
            {
                if (obj == null) return null;

                if (element == "Array")
                    continue;

                if (element.StartsWith("data["))
                {
                    int start = element.IndexOf("[") + 1;
                    int end = element.IndexOf("]");
                    string indexStr = element.Substring(start, end - start);
                    int index = int.Parse(indexStr);

                    if (obj is IList list)
                    {
                        obj = list[index];
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    FieldInfo field = obj.GetType().GetField(element, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        obj = field.GetValue(obj);
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return obj;
        }
    }
}
