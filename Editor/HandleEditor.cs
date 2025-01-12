using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NaughtyAttributes.Editor
{
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class HandleEditor : NaughtyInspector
    {
        public virtual void OnSceneGUI()
        {
            SerializedObject serializedObject = new SerializedObject(target);
            DrawHandles(serializedObject, serializedObject.targetObject.GetType(), serializedObject.targetObject, "");
        }

        private void DrawHandles(SerializedObject serializedObject, Type type, object targetObject, string pathPrefix)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var handleAttribute = field.GetCustomAttribute<HandleAttribute>();
                if (handleAttribute != null)
                {
                    string propertyPath = pathPrefix + field.Name;
                    SerializedProperty property = serializedObject.FindProperty(propertyPath);

                    if (property != null && IsVisibleInHierarchy(property))
                    {
                        if (field.FieldType == typeof(Vector3))
                        {
                            DrawHandle(property, (MonoBehaviour)serializedObject.targetObject, handleAttribute.Type);
                        }
                        else if (typeof(IList).IsAssignableFrom(field.FieldType))
                        {
                            DrawListHandles(property, (MonoBehaviour)serializedObject.targetObject, handleAttribute.Type);
                        }
                    }
                }
                else if (field.FieldType.IsSerializable && !field.FieldType.IsPrimitive && field.FieldType != typeof(string))
                {
                    object nestedObject = field.GetValue(targetObject);
                    if (nestedObject != null)
                    {
                        DrawHandles(serializedObject, field.FieldType, nestedObject, pathPrefix + field.Name + ".");
                    }
                }
            }
        }

        private bool IsVisibleInHierarchy(SerializedProperty property)
        {
            while (property != null)
            {
                if (!PropertyUtility.IsVisible(property))
                {
                    return false;
                }

                property = property.propertyPath.Contains(".")
                    ? property.serializedObject.FindProperty(property.propertyPath.Substring(0, property.propertyPath.LastIndexOf('.')))
                    : null;
            }

            return true;
        }

        private void DrawHandle(SerializedProperty property, MonoBehaviour owner, HandleType handleType)
        {
            Transform ownerTransform = owner.transform;

            EditorGUI.BeginChangeCheck();

            Vector3 position = handleType == HandleType.Global
                ? property.vector3Value
                : ownerTransform.TransformPoint(property.vector3Value);

            Vector3 updatedPosition = Handles.PositionHandle(position, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(owner, "Move Handle");

                if (handleType == HandleType.Global)
                {
                    property.vector3Value = updatedPosition; // Store global position
                }
                else
                {
                    property.vector3Value = ownerTransform.InverseTransformPoint(updatedPosition); // Store local position
                }

                property.serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawListHandles(SerializedProperty listProperty, MonoBehaviour owner, HandleType handleType)
        {
            if (listProperty.isArray)
            {
                for (int i = 0; i < listProperty.arraySize; i++)
                {
                    SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                    if (element.propertyType == SerializedPropertyType.Vector3)
                    {
                        DrawHandle(element, owner, handleType);
                    }
                }
            }
        }
    }
}