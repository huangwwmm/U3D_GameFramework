using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GFEditor.Renderer.LightmapAttach.Exporter
{
    public abstract class BaseExporter : ScriptableObject
    {
        public abstract bool CanExport(out string message);
        public abstract void Export(Scene scene);
        public abstract void OnGUI();

        public abstract void OnInitialize();
        public abstract void OnRelease();

    }
}