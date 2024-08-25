using Assets.Helpers;
using System;
using System.Drawing;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.U2D.IK;
using Color = System.Drawing.Color;

namespace WordSearchBattle.Scripts
{
    public class ColorPickerManager : MonoBehaviour
    {
        public GameObject colorChoicePrefab;

        public UnityEvent<KnownColor> onColorPicked;
        
        private KnownColor[] colors = new KnownColor[]
            {
                KnownColor.White,
                KnownColor.Red,
                KnownColor.Green,
                KnownColor.Blue,
                KnownColor.Yellow,
                KnownColor.Cyan,
                KnownColor.Magenta,
                KnownColor.Orange,
                KnownColor.Purple,
                KnownColor.Gray,
                KnownColor.Pink,
                KnownColor.Teal,
                KnownColor.Navy,
                KnownColor.Olive,
                KnownColor.Maroon,
                KnownColor.Lime
            };


        void Start()
        {
            SetupColorList();
        }

        private void SetupColorList()
        {
            

            foreach (var color in colors)
                CreateColorChoice(color);
        }

        public void CreateColorChoice(KnownColor color)
        {
            GameObject colorChoice = Instantiate(colorChoicePrefab, transform);

            var controller = colorChoice.GetComponent<ColorPickerController>();
            controller.KnownColor = color;
            var colorDrawing = Color.FromKnownColor(color);
            var unityColor = colorDrawing.ToUnityColor();
            controller.SetupColorButton(unityColor, onColorPicked);

            var trans = colorChoice.GetComponent<Transform>();
            trans.name = color.ToIntString();
        }

        public void ColorChosen(KnownColor color, bool self)
        {
            if (color == KnownColor.Transparent)
                return;

            var gameObject = transform.Find(color.ToIntString());
            var pickerController = gameObject.GetComponent<ColorPickerController>();

            pickerController.SetOutlineColor(self ? UnityEngine.Color.black : new UnityEngine.Color(255, 165, 0));
        }

        public void ColorUnChosen(KnownColor color)
        {
            if (color == KnownColor.Transparent)
                return;

            var gameObject = transform.Find(color.ToIntString());
            var pickerController = gameObject.GetComponent<ColorPickerController>();

            pickerController.SetOutlineColor(UnityEngine.Color.clear);
        }

        public void ShowHideMenu(bool show)
            => transform.parent.GetComponent<Transform>().gameObject.SetActive(show);

        internal void ClearColors()
        {
            foreach (var color in colors)
                ColorUnChosen(color);
        }
    }
}