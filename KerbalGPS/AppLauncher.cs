﻿/////////////////////////////////////////////////////////////////////////////////////////////
////
////   AppLauncher.cs
////
////   Kerbal Space Program AppLauncher routines
////
////   (C) Copyright 2016 Ted Thompson
////
////   This code is licensed under GPL-3.0
////
/////////////////////////////////////////////////////////////////////////////////////////////
////
////   Revision History
////
/////////////////////////////////////////////////////////////////////////////////////////////
////
////   Created December 5th, 2016
////
////
/////////////////////////////////////////////////////////////////////////////////////////////

using KSP.UI.Screens;
using UnityEngine;
using ToolbarControl_NS;

namespace KerbStar
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(AppLauncherKerbalGPS.MODID, AppLauncherKerbalGPS.MODNAME);
        }
    }
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AppLauncherKerbalGPS : MonoBehaviour
    {
        public static AppLauncherKerbalGPS Instance;

        //private static ApplicationLauncherButton btnLauncher;
        static internal ToolbarControl toolbarControl;
        internal const string MODID = "KerbalGPS_NS";
        internal const string MODNAME = "KerbalGPS";

        private static string kgps_button_off;
        private static string kgps_button_on_nosat;
        private static string kgps_button_on_sat;
        private static string kgps_button_Texture;
        private static string kgps_button_nogps;
        private static string tex2d;
        public bool displayGUI;

        public enum rcvrStatus
        {
            OFF = 0,
            SATS = 1,
            NOSATS = 2,
            NONE = 3
        }

        public  void Awake()
        {
            Instance = this;
        }

        public  void Start()
        {
            kgps_button_off = "KerbalGPS/PluginData/Icon/GPSIconOff";
            kgps_button_on_sat = "KerbalGPS/PluginData/Icon/GPSIconSat";
            kgps_button_on_nosat = "KerbalGPS/PluginData/Icon/GPSIconNoSat";
            kgps_button_nogps = "KerbalGPS/PluginData/Icon/GPSIconNoGPS";
            tex2d = kgps_button_nogps;

            if (toolbarControl == null)
            {
                toolbarControl = gameObject.AddComponent<ToolbarControl>();
                toolbarControl.AddToAllToolbars(OnToggleTrue, OnToggleFalse,
                    ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
                    MODID,
                    "kerbalgpsButton",
                    kgps_button_off + "-38",
                    kgps_button_off + "-24",
                    MODNAME
                );

            }
        }

        private  void OnToggleTrue()
        {
            displayGUI = true;
        }

        private  void OnToggleFalse()
        {
            displayGUI = false;
        }

        public  void setBtnState(bool state, bool click = false)
        {
            if (state)
                toolbarControl.SetTrue(click);
            else
                toolbarControl.SetFalse(click);
        }

        public  void SetAppLauncherButtonTexture(rcvrStatus status)
        {
            switch (status)
            {
                case rcvrStatus.OFF:
                    tex2d = kgps_button_off;
                    break;
                case rcvrStatus.SATS:
                    tex2d = kgps_button_on_sat;
                    break;
                case rcvrStatus.NOSATS:
                    tex2d = kgps_button_on_nosat;
                    break;
                case rcvrStatus.NONE:
                    tex2d = kgps_button_nogps;
                    break;
            }

            // Set new Launcher Button texture
            if (toolbarControl != null)
            {
                if (tex2d != kgps_button_Texture)
                {
                    kgps_button_Texture = tex2d;
                    toolbarControl.SetTexture(tex2d + "-38", tex2d + "-24");
                }
            }
        }

        public  void OnDestroy()
        {
            if (toolbarControl != null)
            {
                toolbarControl.OnDestroy();
                Destroy(toolbarControl);
                toolbarControl = null;
            }
        }
    }
}