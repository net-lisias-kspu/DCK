using Firespitter;
using Firespitter.customization;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DCKinc
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class DCKPaintshop : MonoBehaviour
    {

        public static DCKPaintshop Instance = null;
        private ApplicationLauncherButton toolbarButton = null;
        private bool showWindow = false;
        private Rect windowRect;

        void Awake()
        {
        }

        void Start()
        {
            Instance = this;
            windowRect = new Rect(Screen.width - 250, Screen.height - 100, 200, 75);  //default size and coordinates, change as suitable
            AddToolbarButton();
        }

        private void OnDestroy()
        {
            if (toolbarButton)
            {
                ApplicationLauncher.Instance.RemoveModApplication(toolbarButton);
                toolbarButton = null;
            }
        }

        void AddToolbarButton()
        {
            string textureDir = "DCK/DCKinc/Plugin/";

            if (HighLogic.LoadedSceneIsEditor)
            {
                if (toolbarButton == null)
                {
                    Texture buttonTexture = GameDatabase.Instance.GetTexture(textureDir + "DCK_selected", false); //texture to use for the button
                    toolbarButton = ApplicationLauncher.Instance.AddModApplication(ShowToolbarGUI, HideToolbarGUI, Dummy, Dummy, Dummy, Dummy, ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB, buttonTexture);
                }
            }
        }

        public void ShowToolbarGUI()
        {
            showWindow = true;
        }

        public void HideToolbarGUI()
        {
            showWindow = false;
        }

        void Dummy()
        { }

        void OnGUI()
        {
            if (showWindow)
            {
                windowRect = GUI.Window(this.GetInstanceID(), windowRect, DCKWindow, "DCK Paintshop", HighLogic.Skin.window);   //change title as suitable
            }
        }

        void DCKWindow(int windowID)
        {
            if (GUI.Button(new Rect(10, 30, 75, 25), "DCK Prev", HighLogic.Skin.button))    //change rect here for button size, position and text
            {
                SendEventDCK(false);
            }

            if (GUI.Button(new Rect(100, 30, 75, 25), "DCK Next", HighLogic.Skin.button))       //change rect here for button size, position and text
            {
                SendEventDCK(true);
            }

            GUI.DragWindow();
        }


        void SendEventDCK(bool next)  //true: next texture, false: previous texture
        {
            Part root = EditorLogic.RootPart;
            if (!root)
                return;

            // find all FStextureSwitch2 modules on all parts
            List<FStextureSwitch2> dckParts = new List<FStextureSwitch2>(200);
            foreach (Part p in EditorLogic.fetch.ship.Parts)
                dckParts.AddRange(p.FindModulesImplementing<FStextureSwitch2>());

            foreach (FStextureSwitch2 dckPart in dckParts)
            {
                // send previous or next command
                if (next)
                    dckPart.nextTextureEvent();
                else
                    dckPart.previousTextureEvent();
            }

        }


    }
}
