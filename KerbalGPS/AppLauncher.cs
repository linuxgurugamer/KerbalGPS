/////////////////////////////////////////////////////////////////////////////////////////////
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

namespace KerbStar.GPSToolbar
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(AppLauncherKerbalGPS.MODID, AppLauncherKerbalGPS.MODNAME);
        }
    }
    public class AppLauncherKerbalGPS : MonoBehaviour
    {
        //private static ApplicationLauncherButton btnLauncher;
        static ToolbarControl toolbarControl;
        internal const string MODID = "KerbalGPS_NS";
        internal const string MODNAME = "KerbalGPS";

        private static string kgps_button_off;
        private static string kgps_button_on_nosat;
        private static string kgps_button_on_sat;
        private static string kgps_button_Texture;
        private static string kgps_button_nogps;
        private static string tex2d;
        public static bool displayGUI;

        public enum rcvrStatus
        {
            OFF = 0,
            SATS = 1,
            NOSATS = 2,
            NONE = 3
        }

        public static void Awake()
        {
        }

        public static void localStart(GameObject gameObject)
        {
            kgps_button_off = "KerbalGPS/Icon/GPSIconOff";
            kgps_button_on_sat = "KerbalGPS/Icon/GPSIconSat";
            kgps_button_on_nosat = "KerbalGPS/Icon/GPSIconNoSat";
            kgps_button_nogps = "KerbalGPS/Icon/GPSIconNoGPS";
            tex2d = kgps_button_nogps;

            if (toolbarControl == null)
            {
                toolbarControl = gameObject.AddComponent<ToolbarControl>();
                toolbarControl.AddToAllToolbars(OnToggleTrue, OnToggleFalse,
                    ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
                    MODID,
                    "kerbalgpsButton",
                    kgps_button_nogps + "-38",
                    kgps_button_nogps + "-24",
                    MODNAME
                );

            }
        }

        private static void OnToggleTrue()
        {
            displayGUI = true;
        }

        private static void OnToggleFalse()
        {
            displayGUI = false;
        }

        public static void setBtnState(bool state, bool click = false)
        {
            if (state)
                toolbarControl.SetTrue(click);
            else
                toolbarControl.SetFalse(click);
        }

        public static void SetAppLauncherButtonTexture(rcvrStatus status)
        {
            tex2d = null;

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
                    toolbarControl.SetTexture(tex2d + "38", tex2d + "-24");
                }
            }
        }

        public static void OnDestroy()
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