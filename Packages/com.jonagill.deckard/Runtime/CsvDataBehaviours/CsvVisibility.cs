using System;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deckard.Data
{
    public class CsvVisibility : CsvDataBehaviour
    {
        [Serializable]
        public struct VisibilityCondition
        {
            public VisbilityBehavior behavior;
            public string stringComparator;
            public float floatComparator;

            public bool ShouldBeVisible(CsvSheet sheet, int index, string key)
            {
                switch (behavior)
                {
                    case VisbilityBehavior.Empty:
                    case VisbilityBehavior.NotEmpty:
                    case VisbilityBehavior.Match:
                    case VisbilityBehavior.Different:
                        return ProcessStringValue(sheet, index, key);
                    case VisbilityBehavior.Equals:
                    case VisbilityBehavior.NotEqual:
                    case VisbilityBehavior.GreaterThan:
                    case VisbilityBehavior.LessThan:
                    case VisbilityBehavior.GreaterThanOrEqualTo:
                    case VisbilityBehavior.LessThanOrEqualTo:
                        return ProcessFloatValue(sheet, index, key);
                    default:
                        throw new InvalidOperationException($"CsvVisbility does not support behavior type {behavior}");
                }
            }

            private bool ProcessStringValue(CsvSheet sheet, int index, string key)
            {
                if (!sheet.TryGetStringValue(key, index, out var value))
                {
                    value = "";
                }

                bool visible;
                switch (behavior)
                {
                    case VisbilityBehavior.Empty:
                        visible = string.IsNullOrEmpty(value);
                        break;
                    case VisbilityBehavior.NotEmpty:
                        visible = !string.IsNullOrEmpty(value);
                        break;
                    case VisbilityBehavior.Match:
                        visible = string.Equals(stringComparator, value);
                        break;
                    case VisbilityBehavior.Different:
                        visible = !string.Equals(stringComparator, value);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"ProcessStringValue does not support behavior type {behavior}");
                }

                return visible;
            }

            private bool ProcessFloatValue(CsvSheet sheet, int index, string key)
            {
                if (!sheet.TryGetFloatValue(key, index, out var value))
                {
                    value = 0f;
                }

                bool visible;
                switch (behavior)
                {
                    case VisbilityBehavior.Equals:
                        visible = value == floatComparator;
                        break;
                    case VisbilityBehavior.NotEqual:
                        visible = value != floatComparator;
                        break;
                    case VisbilityBehavior.GreaterThan:
                        visible = value > floatComparator;
                        break;
                    case VisbilityBehavior.LessThan:
                        visible = value < floatComparator;
                        break;
                    case VisbilityBehavior.GreaterThanOrEqualTo:
                        visible = value >= floatComparator;
                        break;
                    case VisbilityBehavior.LessThanOrEqualTo:
                        visible = value <= floatComparator;
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"ProcessFloatValue does not support behavior type {behavior}");
                }

                return visible;
            }
        }

        public enum VisbilityBehavior
        {
            Empty = 0,
            NotEmpty = 1,
            
            Match = 10,
            Different = 11,
            
            Equals = 20,
            NotEqual = 21,
            GreaterThan = 22,
            GreaterThanOrEqualTo = 23,
            LessThan = 24,
            LessThanOrEqualTo = 25
        }

        public enum MultipleConditionsBehavior
        {
            Any,
            All
        }

        public override PriorityType Priority => PriorityType.Default;

        [SerializeField] private VisibilityCondition[] conditions = new []
        {
            new VisibilityCondition()
            {
                behavior = VisbilityBehavior.NotEmpty
            } 
        };

        [SerializeField]
        private MultipleConditionsBehavior multipleConditionsBehavior = MultipleConditionsBehavior.Any;

        private bool wasVisible;
        
        public override void Process(CsvSheet sheet, int index)
        {
            wasVisible = gameObject.activeSelf;

            if (multipleConditionsBehavior == MultipleConditionsBehavior.Any)
            {
                var visible = false;
                foreach (var condition in conditions)
                {
                    visible |= condition.ShouldBeVisible(sheet, index, key);
                }

                SetVisible(visible);
            }
            else
            {
                var visible = true;
                foreach (var condition in conditions)
                {
                    visible &= condition.ShouldBeVisible(sheet, index, key);
                }

                SetVisible(visible);
            }

        }

        public override void Cleanup()
        {
            gameObject.SetActive(wasVisible);
        }

        private void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
