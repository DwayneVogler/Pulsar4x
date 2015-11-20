﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Json;
using Pulsar4X.ViewModel;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

//#TODO Move the actual rendering stuff out into a partial and separate it from the VMs
//I might also want to make windows render with DirectX so that it plays nicer with wpf, 
//so we'll have to see if we can abstract that too
namespace Pulsar4X.CrossPlatformUI.Views
{
    public class SystemView : Panel
    {
        protected ListBox Systems;
        protected SystemVM CurrentSystem;
        protected RenderVM RenderVM;

        protected Splitter Body;

        protected GLSurface gl_context;

        private UITimer timDraw;

        private bool drawPending = false;

		private OpenGLRenderer Renderer;



        public SystemView(GameVM GameVM)
        {
            DataContext = GameVM;
            gl_context = new GLSurface(GraphicsMode.Default, 3, 3, GraphicsContextFlags.Default);
            RenderVM = new RenderVM();
			Renderer = new OpenGLRenderer ();
            JsonReader.Load(this);

            Systems.BindDataContext(c => c.DataStore, (GameVM c) => c.StarSystems);
            Systems.ItemTextBinding = Binding.Property((SystemVM vm) => vm.Name);
            Systems.ItemKeyBinding = Binding.Property((SystemVM vm) => vm.ID).Convert((Guid ID) => ID.ToString());
            Systems.SelectedIndexChanged += loadSystem;

            Body.Panel2 = gl_context;
            gl_context.GLInitalized += Initialize;
            gl_context.GLDrawNow += DrawNow;
            gl_context.GLShuttingDown += Teardown;
            gl_context.GLResize += Resize;
			gl_context.MouseMove += Gl_context_MouseMove;
        }

        void Gl_context_MouseMove (object sender, MouseEventArgs e)
        {
			Console.WriteLine (e.Location);
        }

        private void Draw()
        {
            drawPending = true;
        }

		public void loadSystem(object sender, EventArgs e)
		{
			CurrentSystem = (SystemVM)((ListBox)sender).SelectedValue;
			Draw();
		}

		public void Initialize(object sender, EventArgs e) {
			timDraw = new UITimer { Interval = 0.013 }; // Every Millisecond.
			timDraw.Elapsed += timDraw_Elapsed;
			timDraw.Start();

			gl_context.MakeCurrent();
			Renderer.Initialize();
		}

		private void timDraw_Elapsed(object sender, EventArgs e)
		{
			if (!drawPending || !gl_context.IsInitialized)
			{
				return;
			}

			gl_context.MakeCurrent();

			Renderer.Draw();

			gl_context.SwapBuffers();

			drawPending = false;
		}

        public void DrawNow(object sender, EventArgs e)
        {
            Draw();
        }

        public void Resize(object sender, EventArgs e)
        {
			gl_context.MakeCurrent();
			Renderer.Resize();
        }

        public void Teardown(object sender, EventArgs e)
        {
			Renderer.Destroy();
        }
    }
}
