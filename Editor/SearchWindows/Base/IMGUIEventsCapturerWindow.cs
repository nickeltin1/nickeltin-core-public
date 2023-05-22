using System;
using UnityEditor;
using UnityEngine;

namespace nickeltin.Core.Editor
{
    /// <summary>
    /// Hackaround to capture IMGUI event <see cref="Event.current"/> while not inside OnGUI loop.
    /// Otherwise <see cref="Event.current"/> is always will be null.
    /// This window opens and instantly closes on the first GUI frame.
    /// </summary>
    internal class IMGUIEventsCapturerWindow : EditorWindow
    {
        public Action<Event> OnGUIUpdate;

        private void OnGUI()
        {
            OnGUIUpdate?.Invoke(Event.current);
            this.Close();
        }

        public static void CaptureEvent(Action<Event> onEventCaptured)
        {
            var window = CreateWindow<IMGUIEventsCapturerWindow>();
            window.OnGUIUpdate = onEventCaptured;
            window.ShowAsDropDown(Rect.zero, Vector2.zero);
        }
    }
}