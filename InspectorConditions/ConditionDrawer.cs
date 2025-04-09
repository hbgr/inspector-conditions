#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace hbgr.InspectorConditions
{
    [CustomPropertyDrawer(typeof(Condition))]
    public class ConditionDrawer : PropertyDrawer
    {
        private const string CONDITIONS_PROPERTY = "_conditions";
        private const string OPERATOR_PROPERTY = "_operator";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 200;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty conditionGroupProperty = property.FindPropertyRelative(CONDITIONS_PROPERTY);

            // Draw label
            EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label, EditorStyles.whiteLabel);

            //  Update position to use based on label size
            var labelSize = EditorStyles.whiteLabel.CalcSize(label);
            position.y += labelSize.y;

            // Cache indent
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Recursively draw each condition group, starting with this object's condition group
            DrawConditionGroupRecursive(position, conditionGroupProperty, 0);

            // Reset indent
            EditorGUI.indentLevel = indent;

            property.serializedObject.ApplyModifiedProperties();
            EditorGUI.EndProperty();
        }

        private static Rect DrawConditionGroupRecursive(Rect position, SerializedProperty conditionGroupProperty,
            int depth)
        {
            Rect rect = position;
            rect.height = 0;

            // If property is not a ConditionGroup don't draw it
            if (conditionGroupProperty.boxedValue is not ConditionGroup conditionGroup)
            {
                return rect;
            }

            // If there are no conditions in the group draw the select condition button
            if (conditionGroup.editorConditions is not { Count: > 0 } conditions)
            {
                var selectRect = new Rect
                {
                    x = position.x,
                    y = position.y + rect.height,
                    width = position.width,
                    height = 0
                };
                selectRect = DrawSelectCondition(selectRect, conditionGroupProperty, depth);
                rect.height += selectRect.height;
                return rect;
            }

            // Draw group operator
            var operatorProperty = conditionGroupProperty.FindPropertyRelative(OPERATOR_PROPERTY);
            var operatorRect = new Rect
            {
                x = position.x,
                y = position.y + rect.height,
                width = 50,
                height = 0
            };
            operatorRect = DrawConditionGroupOperator(operatorRect, operatorProperty, depth);

            // Draw all conditions in the group
            for (int i = 0; i < conditions.Count; i++)
            {
                var condition = conditions[i];
                var property = conditionGroupProperty.FindPropertyRelative(CONDITIONS_PROPERTY)
                    .GetArrayElementAtIndex(i);

                if (condition is not ConditionGroup && condition is not ConditionItem)
                {
                    continue;
                }

                Rect conditionRect = new Rect
                {
                    x = position.x + operatorRect.width * 2f,
                    y = position.y + rect.height,
                    width = position.width - operatorRect.width * 2f - 20,
                    height = 0
                };

                if (condition is ConditionGroup)
                {
                    conditionRect = DrawConditionGroupRecursive(conditionRect, property, depth + 1);
                    rect.height += conditionRect.height;
                }
                else
                {
                    conditionRect = DrawConditionItem(conditionRect, property, depth);
                    rect.height += conditionRect.height;
                    var conditionAddRect = new Rect
                    {
                        x = conditionRect.x,
                        y = position.y + rect.height,
                        width = conditionRect.width,
                        height = 0
                    };
                    conditionAddRect =
                        DrawAddToConditionButton(conditionAddRect, conditionGroupProperty, property, depth);
                    rect.height += conditionAddRect.height;
                }

                // Draw buttons next to the property
                DrawDeleteButton(conditionRect, conditionGroupProperty, property, depth);
                DrawMoveUpButton(conditionRect, conditionGroupProperty, property, depth);
                DrawMoveDownButton(conditionRect, conditionGroupProperty, property, depth);
            }

            var groupAddRect = new Rect
            {
                x = position.x,
                y = position.y + rect.height,
                width = rect.width,
                height = 0
            };
            groupAddRect = DrawAddToGroupButton(groupAddRect, conditionGroupProperty, depth);
            rect.height += groupAddRect.height;

            return rect;
        }

        private static Rect DrawSelectCondition(Rect position, SerializedProperty conditionGroupProperty, int depth)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            if (GUI.Button(position, "Select") && conditionGroupProperty.boxedValue is ConditionGroup conditionGroup)
            {
                // Find possible conditions that can be added
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(type => type.IsSubclassOf(typeof(ConditionItem)))
                    .ToArray();

                if (types.Length <= 0)
                {
                    return position;
                }

                // Create text contents for each possible condition
                var contents = new GUIContent[types.Length];
                for (int i = 0; i < contents.Length; i++)
                {
                    contents[i] = new GUIContent(types[i].FullName);
                }

                // Create, populate and show the condition menu
                GenericMenu menu = new GenericMenu();

                for (int i = 0; i < contents.Length; i++)
                {
                    menu.AddItem(contents[i], false,
                        obj => HandleAddConditionToExistingGroup(types, obj, conditionGroupProperty), i);
                }

                menu.ShowAsContext();
                Event.current.Use();
            }

            return position;
        }

        private static void HandleAddToConditionToExistingCondition(Type[] conditionTypes, object typeIndex,
            SerializedProperty conditionGroupProperty, SerializedProperty existingConditionProperty)
        {
            if (typeIndex is not int i || conditionTypes.Length < i + 1 || conditionTypes[i] is not Type type ||
                conditionGroupProperty.boxedValue is not ConditionGroup parentConditionGroup ||
                existingConditionProperty.boxedValue is not ConditionItem existingConditionItem ||
                conditionGroupProperty.FindPropertyRelative(CONDITIONS_PROPERTY) is not SerializedProperty
                    parentConditionsProperty)
            {
                return;
            }

            var newGroup = new ConditionGroup
            {
                editorConditions = new List<ConditionItem> { existingConditionItem }
            };
            existingConditionProperty.boxedValue = newGroup;
            existingConditionProperty.serializedObject.ApplyModifiedProperties();
            HandleAddConditionToExistingGroup(conditionTypes, typeIndex, existingConditionProperty);
        }

        private static void HandleAddConditionToExistingGroup(Type[] conditionTypes, object typeIndex,
            SerializedProperty conditionGroupProperty)
        {
            if (typeIndex is not int i || conditionTypes.Length < i + 1 || conditionTypes[i] is not Type type ||
                conditionGroupProperty.boxedValue is not ConditionGroup conditionGroup ||
                conditionGroupProperty.FindPropertyRelative(CONDITIONS_PROPERTY) is not SerializedProperty
                    conditionsProperty
               )
            {
                return;
            }

            var maybeCondition = Activator.CreateInstance(type);
            if (maybeCondition is ConditionItem condition)
            {
                conditionsProperty.arraySize += 1;
                var entry = conditionsProperty.GetArrayElementAtIndex(conditionsProperty.arraySize - 1);
                entry.managedReferenceValue = condition;
                conditionGroupProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        private static Rect DrawConditionGroupOperator(Rect position, SerializedProperty operatorProperty, int depth)
        {
            var cachedPosition = position;
            position.width = 50;
            position.height = EditorGUI.GetPropertyHeight(operatorProperty);
            var enumIntValue = operatorProperty.intValue;

            var maybeEnumValue = EditorGUI.EnumPopup(position, (ConditionGroup.ConditionGroupOperator)enumIntValue);
            if (maybeEnumValue is ConditionGroup.ConditionGroupOperator op)
            {
                operatorProperty.intValue = (int)op;
            }

            cachedPosition.width = position.width;
            return cachedPosition;
        }

        private static Rect DrawConditionItem(Rect position, SerializedProperty conditionItemProperty, int depth)
        {
            var cachedPosition = position;
            conditionItemProperty.isExpanded = true;

            position.height = EditorGUI.GetPropertyHeight(conditionItemProperty, true);
            EditorGUI.PropertyField(position, conditionItemProperty, GUIContent.none, true);

            cachedPosition.y += position.height;
            cachedPosition.height = position.height;
            return cachedPosition;
        }

        private static Rect DrawAddToGroupButton(Rect position, SerializedProperty conditionGroupProperty, int depth)
        {
            var cachedPosition = position;
            position.width = 20;
            position.height = 20;
            position.x += (cachedPosition.width / 2f - position.width / 2f);
            position.y += 5;

            if (GUI.Button(position, "+") && conditionGroupProperty.boxedValue is ConditionGroup conditionGroup)
            {
                // Find possible conditions that can be added
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(type => type.IsSubclassOf(typeof(ConditionItem)))
                    .ToArray();

                if (types.Length <= 0)
                {
                    return position;
                }

                // Create text contents for each possible condition
                var contents = new GUIContent[types.Length];
                for (int i = 0; i < contents.Length; i++)
                {
                    contents[i] = new GUIContent(types[i].FullName);
                }

                // Create, populate and show the condition menu
                GenericMenu menu = new GenericMenu();

                for (int i = 0; i < contents.Length; i++)
                {
                    menu.AddItem(contents[i], false,
                        obj => HandleAddConditionToExistingGroup(types, obj, conditionGroupProperty), i);
                }

                menu.ShowAsContext();
                Event.current.Use();
            }

            cachedPosition.y += position.height + 5;
            return cachedPosition;
        }

        private static Rect DrawAddToConditionButton(Rect position, SerializedProperty conditionGroupProperty,
            SerializedProperty existingConditionProperty, int depth)
        {
            var cachedPosition = position;
            position.width = 20;
            position.height = 20;
            position.x += (cachedPosition.width / 2f - position.width / 2f);
            position.y += 5;

            if (GUI.Button(position, "+") && conditionGroupProperty.boxedValue is ConditionGroup conditionGroup)
            {
                // Find possible conditions that can be added
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(type => type.IsSubclassOf(typeof(ConditionItem)))
                    .ToArray();

                if (types.Length <= 0)
                {
                    return position;
                }

                // Create text contents for each possible condition
                var contents = new GUIContent[types.Length];
                for (int i = 0; i < contents.Length; i++)
                {
                    contents[i] = new GUIContent(types[i].FullName);
                }

                // Create, populate and show the condition menu
                GenericMenu menu = new GenericMenu();

                for (int i = 0; i < contents.Length; i++)
                {
                    menu.AddItem(contents[i], false,
                        obj => HandleAddToConditionToExistingCondition(types, obj, conditionGroupProperty,
                            existingConditionProperty), i);
                }

                menu.ShowAsContext();
                Event.current.Use();
            }

            cachedPosition.y += position.height + 5;
            return cachedPosition;
        }

        private static void DrawDeleteButton(Rect position, SerializedProperty conditionGroupProperty,
            SerializedProperty conditionItemProperty, int depth)
        {
            position.y -= position.height / 2f;
            position.x -= 25;
            position.height = 15;
            position.width = 18;
            position.y -= position.height / 2f;
            position.x -= position.width / 2f;

            if (GUI.Button(position, "-") && conditionGroupProperty.boxedValue is ConditionGroup conditionGroup)
            {
                // Remove condition from condition group
            }
        }

        private static void DrawMoveUpButton(Rect position, SerializedProperty conditionGroupProperty,
            SerializedProperty conditionItemProperty, int depth)
        {
            position.y -= 3f * position.height / 4f;
            position.x -= 25;
            position.height = 15;
            position.width = 18;
            position.y -= position.height / 2f;
            position.x -= position.width / 2f;

            if (GUI.Button(position, "^") && conditionGroupProperty.boxedValue is ConditionGroup conditionGroup)
            {
                // Remove condition from condition group
            }
        }

        private static void DrawMoveDownButton(Rect position, SerializedProperty conditionGroupProperty,
            SerializedProperty conditionItemProperty, int depth)
        {
            position.y -= position.height / 4f;
            position.x -= 25;
            position.height = 15;
            position.width = 18;
            position.y -= position.height / 2f;
            position.x -= position.width / 2f;

            if (GUI.Button(position, "v") && conditionGroupProperty.boxedValue is ConditionGroup conditionGroup)
            {
                // Remove condition from condition group
            }
        }
    }
}
#endif