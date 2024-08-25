using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WordSearchBattle.Scripts
{
    public class ColorPickerController : MonoBehaviour
    {
        private Image image;
        private Button button;
        private Outline outline;

        public System.Drawing.KnownColor KnownColor { get; set; }

        private void Awake()
        {
            image = GetComponent<Image>();
            button = GetComponent<Button>();
            outline = GetComponent<Outline>();
        }

        public void SetupColorButton(Color color, UnityEvent<System.Drawing.KnownColor> action)
        {
            image.color = color;
            button.onClick.AddListener(() => action.Invoke(this.KnownColor));
        }

        public void SetOutlineColor(Color color)
            => outline.effectColor = color;
        
    }
}