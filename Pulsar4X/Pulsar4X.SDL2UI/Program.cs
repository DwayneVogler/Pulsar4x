﻿using System;
using System.Runtime.InteropServices;
using SDL2;
using ImGuiNET;
using ImGuiSDL2CS;
using System.Drawing;

namespace Pulsar4X.SDL2UI
{
    public class Program
    {
        static SDL2Window Instance;
        [STAThread]
        public static void Main()
        {

            Instance = new PulsarMainWindow();
            Instance.Run();
            Instance.Dispose();
        }
    }

    public class PulsarMainWindow : ImGuiSDL2CSWindow
    {
        private GlobalUIState _state; // = new GlobalUIState(new Camera(this));


        private MemoryEditor _MemoryEditor = new MemoryEditor();
        private byte[] _MemoryEditorData;

        private FileDialog _Dialog = new FileDialog(false, false, true, false, false, false);

        ImVec3 backColor = new ImVec3(0 / 255f, 0 / 255f, 28 / 255f);
         

        public PulsarMainWindow()
            : base("Pulsar4X")
        {
            _state = new GlobalUIState(this);
            //_state.MainWinSize = this.Size;

            // Create any managed resources and set up the main game window here.
            _MemoryEditorData = new byte[1024];
            Random rnd = new Random();
            for (int i = 0; i < _MemoryEditorData.Length; i++)
            {
                _MemoryEditorData[i] = (byte)rnd.Next(255);
            }
            backColor = new ImVec3(0 / 255f, 0 / 255f, 28 / 255f);

            _state.MapRendering = new SystemMapRendering(this, _state);
            OnEvent = MyEventHandler;
        }

        private bool MyEventHandler(SDL2Window window, SDL.SDL_Event e)
        {
            
            if (!ImGuiSDL2CSHelper.HandleEvent(e, ref g_MouseWheel, g_MousePressed))
                return false;

            if (e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN && e.button.button == 1)
            { 
                _state.Camera.IsGrabbingMap = true;
                _state.Camera.MouseFrameIncrementX = e.motion.x;
                _state.Camera.MouseFrameIncrementY = e.motion.y;
            }
            if (e.type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP && e.button.button == 1)
            {
                _state.Camera.IsGrabbingMap = false;

            }
                 


           
            /*
            if (g_MousePressed[1])
                SDL.SDL_ShowSimpleMessageBox(0, "Mouse", "Right button was pressed!", window.Handle);
            if (g_MousePressed[2])
                SDL.SDL_ShowSimpleMessageBox(0, "Mouse", "Middle button was pressed!", window.Handle);
            */
            if (_state.Camera.IsGrabbingMap)
            {
                int deltaX = _state.Camera.MouseFrameIncrementX - e.motion.x;
                int deltaY = _state.Camera.MouseFrameIncrementY - e.motion.y;
                _state.Camera.WorldOffset(deltaX, deltaY);

                _state.Camera.MouseFrameIncrementX = e.motion.x;
                _state.Camera.MouseFrameIncrementY = e.motion.y;

            }

            int mouseX;
            int mouseY;
            SDL.SDL_GetMouseState(out mouseX, out mouseY);

            if (e.type == SDL.SDL_EventType.SDL_MOUSEWHEEL)
            {
                if (e.wheel.y > 0)
                {
                    _state.Camera.ZoomIn(0, 0);//mouseX, mouseY);
                }
                else if (e.wheel.y < 0)
                {
                    _state.Camera.ZoomOut(0, 0);//mouseX, mouseY);
                }
            }
            return true;
        }

        public unsafe override void ImGuiLayout()
        {

            foreach (var item in _state.OpenWindows.ToArray())
            {
                item.Display();
            }
        }


        public override void ImGuiRender()
        {
            GL.ClearColor(backColor.X, backColor.Y, backColor.Z, 1f);
            GL.Clear(GL.Enum.GL_COLOR_BUFFER_BIT);

            _state.MapRendering.Display();

            // Render ImGui on top of the rest.
            base.ImGuiRender();
        }

    }

    public abstract class PulsarGuiWindow
    {
        protected ImGuiWindowFlags _flags = ImGuiWindowFlags.Default;
        internal bool IsActive = false;
        internal abstract void Display();
    }
}