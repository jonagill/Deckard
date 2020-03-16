using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deckard.Data
{
    public class CsvVisibility : CsvDataBehaviour
    {
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

        public override PriorityType Priority => PriorityType.Default;
        
        public VisbilityBehavior Behavior => behavior;

        [SerializeField] private VisbilityBehavior behavior = VisbilityBehavior.NotEmpty;
        [SerializeField] private string stringComparator;
        [SerializeField] private float floatComparator;

        private bool wasActive;
        
        
        public override void Process(CsvSheet sheet, int index)
        {
            wasActive = gameObject.activeSelf;

            switch (behavior)
            {
                case VisbilityBehavior.Empty:
                case VisbilityBehavior.NotEmpty:
                case VisbilityBehavior.Match:
                case VisbilityBehavior.Different:
                    ProcessStringValue(sheet, index);                
                    break;
                case VisbilityBehavior.Equals:
                case VisbilityBehavior.NotEqual:
                case VisbilityBehavior.GreaterThan:
                case VisbilityBehavior.LessThan:
                case VisbilityBehavior.GreaterThanOrEqualTo:
                case VisbilityBehavior.LessThanOrEqualTo:
                    ProcessFloatValue(sheet, index);    
                    break;
                default:
                    throw new InvalidOperationException($"CsvVisbility does not support behavior type {behavior}");
            }
        }

        private void ProcessStringValue(CsvSheet sheet, int index)
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
                    throw new InvalidOperationException($"ProcessStringValue does not support behavior type {behavior}");
            }
            
            SetVisible(visible);
        }
        
        private void ProcessFloatValue(CsvSheet sheet, int index)
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
                    throw new InvalidOperationException($"ProcessFloatValue does not support behavior type {behavior}");
            }

            SetVisible(visible);
        }

        public override void Cleanup()
        {
            gameObject.SetActive(wasActive);
        }

        private void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
