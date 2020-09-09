using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace PlaylistDownLoader.Utilites
{
    public class Utility : MonoBehaviour
    {
        public static TextMeshProUGUI CreateNotificationText(string text)
        {
            var gameObject = new GameObject();
            GameObject.DontDestroyOnLoad(gameObject);
            gameObject.transform.position = new Vector3(0, 0f, 2.5f);
            gameObject.transform.eulerAngles = new Vector3(0, 0, 0);
            gameObject.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rectTransform = canvas.transform as RectTransform;
            rectTransform.sizeDelta = new Vector2(200, 50);

            var notificationText = BSUI.CreateText(canvas.transform as RectTransform, text, new Vector2(0f, -70f), new Vector2(400f, 20f));

            notificationText.text = text;
            notificationText.fontSize = 10f;
            notificationText.alignment = TextAlignmentOptions.Center;
            return notificationText;
        }
    }
}
