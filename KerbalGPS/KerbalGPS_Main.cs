/////////////////////////////////////////////////////////////////////////////////////////////
////
////   KerbalGPS_Main.cs
////
////   Kerbal Space Program GPS math library
////
////   (C) Copyright 2012-2013, Kevin Wilder (a.k.a. PakledHostage)
////
////   This code is licensed under the Attribution-NonCommercial-ShareAlike 3.0 (CC BY-NC-SA 3.0) 
////   creative commons license. See <http://creativecommons.org/licenses/by-nc-sa/3.0/legalcode> 
////   for full details.
////
////   Attribution — You are free to modify this code, so long as you mention that the resulting
////                 work is based upon or adapted from this library. This KerbalGPS_Main.cs
////                 code library is the original work of Kevin Wilder.
////
////   Non-commercial - You may not use this work for commercial purposes.
////
////   Share Alike — If you alter, transform, or build upon this work, you may distribute the 
////                 resulting work only under the same or similar license to the CC BY-NC-SA 3.0
////                 license.
////
////
/////////////////////////////////////////////////////////////////////////////////////////////
////
////   Revision History
////
/////////////////////////////////////////////////////////////////////////////////////////////
////
////   Created November 10th, 2012
////
////   Revised October 26, 2013 by Kevin Wilder to incorporate changes suggested by m4v.
////
////   Revised December 1, 2016 by Ted Thompson to incorporate remove obsolete RenderManager refs
////
////
/////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using System.Linq;
using ClickThroughFix;

namespace KerbStar
{
    public class KerbalGPS : PartModule
    {
        /////////////////////////////////////////////////////////////////////////////////////////////
        //
        //    Public Variables
        //
        /////////////////////////////////////////////////////////////////////////////////////////////

        [KSPField]
        public string GNSSacronym = NULL_ACRONYM;

        [KSPField]
        public string SBASacronym = NULL_ACRONYM;

        [KSPField]
        public string EarthTime = "FALSE";

        [KSPField(isPersistant = false, guiActive = true, guiName = "Position")]
        public string gsPosition;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Altitude")]
        public string gsAltitude;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Visible Satellites")]
        public UInt16 guNumSats;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Accuracy")]
        public string gsAccuracy;

        public class GNSSSatelliteInfo
        {
            public string name;
            public Guid guid;
            public double gpsRange;

            public GNSSSatelliteInfo(string n, Guid g, double r)
            {
                name = n;
                guid = g;
                gpsRange = r;
            }
        }
        public List<GNSSSatelliteInfo> GNSSSatteliteInfoList = new List<GNSSSatelliteInfo>();
        //public List<string> GNSSSatelliteNames = new List<string>();
        //public List<Guid> GNSSSatelliteIDs = new List<Guid>();

        //public bool displayGUI;

        static internal KerbalGPS Instance;

        /////////////////////////////////////////////////////////////////////////////////////////////
        //
        //    Private Variables
        //
        /////////////////////////////////////////////////////////////////////////////////////////////

        private GPS_Calculations clsGPSMath = new GPS_Calculations();
        internal Rect varWindowPos;
        internal Rect varDestWindowPos;

        private Vector3 gfPosition;
        private DateTime gLastSVCheckTime;
        private float gfPositionErrorEstimate = 999.9f;
        private float gfDeltaTime = 0.0f;
        private float gfFilteredAltitude = 0.0f;

        internal const float DEF_DESTLAT = -0.1033f;
        internal const float DEF_DESTLON = -74.575f;

        private float gfDestLat = DEF_DESTLAT;
        private float gfDestLon = DEF_DESTLON;
        private float gfOrigLat = DEF_DESTLAT;
        private float gfOrigLon = DEF_DESTLON;

        private bool gyKerbalGPSInitialised = false;
        private bool gyReceiverOn = true;
        private uint gbDisplayMode = MODE_GPS_POSITION;
        private int giWindowID;
        private int giDestWinID;
        private int giLastVesselCount = 0;
        private int giTransmitterID = FIGARO_TRANSMITTER_PART_NAME.GetHashCode();
        // private int gpsMaster;

        private System.String gsLat;
        private System.String gsLon;
        private System.String gsTime;
        private System.String gsDistance;
        private System.String gsHeading;
        private System.String gsLatDeg = "0";
        private System.String gsLatMin = "06.2";
        private System.String gsLatSec = "0";
        private System.String gsLatNS = "S";
        private System.String gsLonDeg = "74";
        private System.String gsLonMin = "34.5";
        private System.String gsLonSec = "0";
        private System.String gsLonEW = "W";
        private System.String gsModeString = "Position";

        const string NONAME = "No Name";
        private string gsDestName = NONAME;

        private NumberStyles varStyle = NumberStyles.Any;
        private CultureInfo varCulture = CultureInfo.CreateSpecificCulture("en-US");

        private static bool masterSet;
        private bool amIMaster;

        /////////////////////////////////////////////////////////////////////////////////////////////
        //
        //    Constants
        //
        /////////////////////////////////////////////////////////////////////////////////////////////

        private const string strVersion = "2";
        private const string strKSPVersion = "1.2.1";
        private const string strSubVersion = "00";

        private const float MIN_CALCULATION_INTERVAL = 0.25f; // 4 Hz GPS
        internal const float GPS_GUI_WIDTH = 200.0f;
        internal const float GPS_GUI_HEIGHT = 152.0f;
        internal float GPS_DEST_GUI_HEIGHT = Screen.height / 3f;

        private const uint MODE_GPS_POSITION = 0;
        private const uint MODE_GPS_DESTINATION = 1;
        private const uint MODE_GPS_STATUS = 2;

        private const string FIGARO_TRANSMITTER_PART_NAME = "FigaroTransmitter";

        private const string NULL_ACRONYM = "NONE";


        /////////////////////////////////////////////////////////////////////////////////////////////
        //
        //    Implementation - Public functions
        //
        /////////////////////////////////////////////////////////////////////////////////////////////

        /********************************************************************************************
        Function Name: OnLoad
        Parameters: see function definition
        Return: void
         
        Description:  Called when PartModule is loaded.
         
        *********************************************************************************************/

        public override void OnLoad(ConfigNode node)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            clsGPSMath.Reset();
            gyKerbalGPSInitialised = false;

            giWindowID = DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond;
            giDestWinID = DateTime.Now.Minute + DateTime.Now.Second + DateTime.Now.Millisecond + 1;
            gLastSVCheckTime = DateTime.Now;

            base.OnLoad(node);
        }


        /********************************************************************************************
        Function Name: OnSave
        Parameters: see function definition
        Return: void
         
        Description:  Called when PartModule is saved.
         
        *********************************************************************************************/

        public override void OnSave(ConfigNode node)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;


            this.part.customPartData = "[GPS Dest:," + gfDestLat.ToString() + "," + gfDestLon.ToString() + "]";

            base.OnSave(node);
        }


        /********************************************************************************************
        Function Name: OnUpdate
        Parameters: void
        Return: void
         
        Description: Called on non-physics update cycle
         
        *********************************************************************************************/

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            checkMaster();

            if (amIMaster)
            {
                gfDeltaTime += TimeWarp.deltaTime;

                if ((this.vessel.rootPart.isControllable) && (this.vessel.isActiveVessel) && (gfDeltaTime > MIN_CALCULATION_INTERVAL))
                {
                    if (gyKerbalGPSInitialised)
                    {
                        if (gyReceiverOn)
                        {
                            // Search for new GNSS satellites every 30 seconds, and then only if the number of vessels has changed:
                            TimeSpan varCheckInterval = DateTime.Now - gLastSVCheckTime;
                            if (varCheckInterval.Seconds > 30)
                            {
                                Find_GNSS_Satellites();
                                gLastSVCheckTime = DateTime.Now;
                            }

                            if (clsGPSMath.Calculate_GPS_Position(out gfPosition, out guNumSats, out gfPositionErrorEstimate, out gfFilteredAltitude))
                            {
                                // Sats found, set icon
                                //GPSToolbar.AppLauncherKerbalGPS.SetAppLauncherButtonTexture(GPSToolbar.AppLauncherKerbalGPS.rcvrStatus.SATS);
                                // Use the built-in GetLatitude and GetLongitude functions to compute the latitude and longitude
                                gfOrigLat = (float)vessel.mainBody.GetLatitude(gfPosition);
                                gfOrigLon = (float)vessel.mainBody.GetLongitude(gfPosition);

                                gsLat = clsGPSMath.Lat_to_String(gfOrigLat);
                                gsLon = clsGPSMath.Lon_to_String(gfOrigLon);

                                gsPosition = gsLat + " " + gsLon;
                                gsAltitude = Math.Round(gfFilteredAltitude, 1).ToString("#0.0") + " m";
                                gsAccuracy = Math.Round(gfPositionErrorEstimate, 1).ToString("#0.0") + " m";

                                if (gbDisplayMode == MODE_GPS_POSITION)
                                {
                                    gsTime = clsGPSMath.Time_to_String(Planetarium.GetUniversalTime(), (EarthTime == "TRUE"));
                                }
                                else if (gbDisplayMode == MODE_GPS_DESTINATION)
                                {
                                    gsDistance = clsGPSMath.Great_Circle_Distance(gfOrigLat, gfOrigLon, gfDestLat, gfDestLon, gfFilteredAltitude);
                                    gsHeading = clsGPSMath.Great_Circle_Heading(gfOrigLat, gfOrigLon, gfDestLat, gfDestLon);
                                }
                            }
                            else
                            {
                                // Sats not found, set icon
                                //GPSToolbar.AppLauncherKerbalGPS.SetAppLauncherButtonTexture(GPSToolbar.AppLauncherKerbalGPS.rcvrStatus.NOSATS);

                                gsTime = clsGPSMath.Time_to_String(Planetarium.GetUniversalTime(), (EarthTime == "TRUE"));
                                gsLat = "N/A";
                                gsLon = "N/A";
                                gsAltitude = "N/A";
                                gsAccuracy = "N/A";
                                gsHeading = "N/A";
                                gsDistance = "N/A";
                                gsPosition = gsLat;
                                gsAccuracy = "N/A";
                            }

                            gfDeltaTime = 0.0f;
                        }


                        if (gyReceiverOn)
                        {
                            if (guNumSats >= 4)
                            {
                                AppLauncherKerbalGPS.Instance.SetAppLauncherButtonTexture(AppLauncherKerbalGPS.rcvrStatus.SATS);
                            }
                            else
                            {
                                AppLauncherKerbalGPS.Instance.SetAppLauncherButtonTexture(AppLauncherKerbalGPS.rcvrStatus.NOSATS);
                            }
                        }
                        else
                        {
                            AppLauncherKerbalGPS.Instance.SetAppLauncherButtonTexture(AppLauncherKerbalGPS.rcvrStatus.OFF);
                        }

                    }
                    else
                    {
                        Initialise_KerbalGPS();
                    }
                }
            }
            else
            {
                if (vessel.isActiveVessel)
                    AppLauncherKerbalGPS.Instance.SetAppLauncherButtonTexture(AppLauncherKerbalGPS.rcvrStatus.OFF);
            }

            //base.OnUpdate();
        }


        /********************************************************************************************
        Function Name: Find_GNSS_Satellites
        Parameters: void
        Return: void
         
        Description:  Checks if the number of vessels has changed and if so, finds GNSS satellites 
        among the list of existing vessels. 
         
        *********************************************************************************************/

        public void Find_GNSS_Satellites()
        {
            if (GNSSacronym != NULL_ACRONYM) return;

            if (this.vessel == null) return;

            if (this.vessel.isActiveVessel)
            {
                if (FlightGlobals.Vessels.Count != giLastVesselCount)
                {
                    GNSSSatteliteInfoList.Clear();
                    //GNSSSatelliteIDs.Clear();
                    //GNSSSatelliteNames.Clear();
                    giLastVesselCount = FlightGlobals.Vessels.Count;

                    for (int i = FlightGlobals.Vessels.Count - 1; i >= 0; i--)
                    //foreach (Vessel varVessel in FlightGlobals.Vessels)
                    {
                        Vessel varVessel = FlightGlobals.Vessels[i];
                        // proceed if vessel being checked has a command pod, is orbiting the same celestial object and is not the active vessel
                        if ((varVessel.isCommandable) && (vessel.mainBody == varVessel.mainBody) && (varVessel != vessel))
                        {
                            for (int x = varVessel.protoVessel.protoPartSnapshots.Count - 1; x >= 0; x--)
                            //foreach (ProtoPartSnapshot varPart in varVessel.protoVessel.protoPartSnapshots)
                            {
                                ProtoPartSnapshot varPart = varVessel.protoVessel.protoPartSnapshots[x];
                                if (varPart.partName.GetHashCode() == giTransmitterID)
                                {
                                    double gpsRange = 0;
                                    for (int im = varPart.modules.Count - 1; im >= 0; im--)
                                    //foreach (ProtoPartModuleSnapshot m in varPart.modules)
                                    {
                                        ProtoPartModuleSnapshot m = varPart.modules[im];
                                        if (m.moduleName == "ModuleGPSTransmitter")
                                        {
                                            string r = "500000";
                                            m.moduleValues.TryGetValue("gpsRange", ref r);
                                            gpsRange = Double.Parse(FileIO.SafeLoad(r, 500000f));
                                        }
                                    }
                                    if (CheckForEC(varVessel))
                                    {

                                        GNSSSatteliteInfoList.Add(new GNSSSatelliteInfo(varVessel.name, varVessel.id, gpsRange));
                                        //GNSSSatelliteNames.Add(varVessel.name);
                                        //GNSSSatelliteIDs.Add(varVessel.id);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal static double doubleValue(ConfigNode node, string key)
        {
            double v = 0d;
            Double.TryParse(node.GetValue(key), out v);
            return v;
        }

        bool CheckForEC(Vessel vessel)
        {
            // Need to add a check for range here

            double ec = 0;
            if (vessel.loaded)
            {
                for (int i = vessel.parts.Count - 1; i >= 0; i--)
                {
                    Part p = vessel.parts[i];
                    for (int i1 = p.Resources.Count - 1; i1 >= 0; i1--)
                    {
                        PartResource r = p.Resources[i1];
                        if (r.resourceName == "ElectricCharge")
                        {
                            ec += r.amount;
                        }
                    }
                }
            }
            else
            {
                ProtoVessel proto = vessel.protoVessel;

                var res = new SortedDictionary<string, ResourceData>();
                for (int i = proto.protoPartSnapshots.Count - 1; i >= 0; i--)
                {
                    ProtoPartSnapshot p = proto.protoPartSnapshots[i];

                    for (int i1 = p.resources.Count - 1; i1 >= 0; i1--)
                    {
                        ProtoPartResourceSnapshot r = p.resources[i1];
                        if (r.resourceName == "ElectricCharge")
                        {
                            ec += r.amount;
                        }
                    }
                }
            }
            return (ec > 0.001);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////
        //
        //    Implementation - Private functions
        //
        /////////////////////////////////////////////////////////////////////////////////////////////

        /********************************************************************************************
        Function Name: WindowGUI
        Parameters: see function definition
        Return: see function definition
         
        Description:  Callback function to draw GUI
         
        *********************************************************************************************/
        bool loadDestination = false;
        GUIStyle varButtonStyle = null;
        static GUIStyle varTextStyle;
        static GUIStyle varHemisphereStyle;
        static GUIStyle varLabelStyle;

        private void OnGUI()
        {
            if (varButtonStyle == null)
            {
                varButtonStyle = new GUIStyle(GUI.skin.button);
                varTextStyle = new GUIStyle(GUI.skin.textField);
                varHemisphereStyle = new GUIStyle(GUI.skin.textField);
                varLabelStyle = new GUIStyle(GUI.skin.label);
                //varButtonStyle.fixedWidth = GPS_GUI_WIDTH - 5.0f;
                varButtonStyle.fixedHeight = 20.0f;
                //varButtonStyle.contentOffset = new Vector2(0, 2);
                varButtonStyle.normal.textColor = varButtonStyle.focused.textColor = Color.white;
                varButtonStyle.hover.textColor = varButtonStyle.active.textColor = Color.yellow;

                varTextStyle.alignment = TextAnchor.UpperCenter;
                varTextStyle.normal.textColor = varTextStyle.focused.textColor = Color.white;
                varTextStyle.hover.textColor = varTextStyle.active.textColor = Color.yellow;
                varTextStyle.padding = new RectOffset(0, 0, 0, 0);
                varTextStyle.fixedHeight = 16.0f;
                varTextStyle.fixedWidth = 35.0f;

                varHemisphereStyle.alignment = TextAnchor.UpperCenter;
                varHemisphereStyle.normal.textColor = varHemisphereStyle.focused.textColor = Color.white;
                varHemisphereStyle.hover.textColor = varHemisphereStyle.active.textColor = Color.yellow;
                varHemisphereStyle.padding = new RectOffset(0, 0, 0, 0);
                varHemisphereStyle.fixedHeight = 16.0f;
                varHemisphereStyle.fixedWidth = 20.0f;

                varLabelStyle.padding = new RectOffset(0, 0, 0, 7);
            }
            if (!hideUI && amIMaster && AppLauncherKerbalGPS.Instance.displayGUI)
            {
                try
                {
                    if ((this.part.State != PartStates.DEAD) && (this.vessel.isActiveVessel))
                    {
                        if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalGPSSettings>().useKSPskin)
                            GUI.skin = HighLogic.Skin;
                        varWindowPos = ClickThruBlocker.GUILayoutWindow(giWindowID, varWindowPos, WindowGUI, "KerbalGPS - " + gsModeString, GUILayout.MinWidth(GPS_GUI_WIDTH), GUILayout.MinHeight(GPS_GUI_HEIGHT));
                    }
                    else
                    {
                        //RenderingManager.RemoveFromPostDrawQueue(3, new Callback(drawGUI)); //close the GUI if part has been deleted
                        //displayGUI = false;
                        //GPSToolbar.AppLauncherKerbalGPS.setBtnState(false);
                    }
                }
                catch
                {
                    //RenderingManager.RemoveFromPostDrawQueue(3, new Callback(drawGUI)); //close the GUI if part has been deleted
                    //displayGUI = false;
                    //GPSToolbar.AppLauncherKerbalGPS.setBtnState(false);
                }

                if (loadDestination)
                {
                    //if (activeGPSmodule.part.State != PartStates.DEAD)
                    {
                        varDestWindowPos = ClickThruBlocker.GUILayoutWindow(giDestWinID, varDestWindowPos, DestWindowGUI, "KerbalGPS - " + "Destinations", GUILayout.MinWidth(GPS_GUI_WIDTH), GUILayout.Height(GPS_DEST_GUI_HEIGHT));
                    }
                }
            }
        }

        Vector2 displayScrollVector;        FileIO.GPS_Coordinates selectedCoordinate = null;        bool b;
        private void DestWindowGUI(int windowID)        {            GUILayout.BeginVertical();            displayScrollVector = GUILayout.BeginScrollView(displayScrollVector);            foreach (var entry in FileIO.gdDestinations)            {                if (entry.Value.sCelestialBodyName == FlightGlobals.ActiveVessel.mainBody.name)
                {
                    GUILayout.BeginHorizontal();
                    b = (selectedCoordinate != null && selectedCoordinate.sDestName == entry.Key);

                    if (GUILayout.Toggle(b, entry.Key))
                    {
                        selectedCoordinate = entry.Value;
                    }
                    GUILayout.EndHorizontal();
                }            }            GUILayout.EndScrollView();
            if (selectedCoordinate != null)            {                gsDestName = selectedCoordinate.sDestName;                gfDestLat = selectedCoordinate.fDestLat;                gfDestLon = selectedCoordinate.fDestLon;                gsLatDeg = Math.Floor(Math.Abs(gfDestLat)).ToString();
                double min = (Math.Abs(gfDestLat) - Math.Floor(Math.Abs(gfDestLat))) * 60.0f;
                if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalGPSSettings>().useDecimalMinutes)
                {
                    gsLatMin = ((Math.Abs(gfDestLat) - Math.Floor(Math.Abs(gfDestLat))) * 60.0f).ToString("#0.0");
                }
                else
                {
                    gsLatMin = Math.Floor(min).ToString();
                    double sec = min - Math.Floor(min);
                    gsLatSec = sec.ToString("#0.0");
                }
                gsLonDeg = Math.Floor(Math.Abs(gfDestLon)).ToString();
                min = (Math.Abs(gfDestLon) - Math.Floor(Math.Abs(gfDestLon))) * 60.0f;
                if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalGPSSettings>().useDecimalMinutes)
                {
                    gsLonMin = ((Math.Abs(gfDestLon) - Math.Floor(Math.Abs(gfDestLon))) * 60.0f).ToString("#0.0");
                }
                else
                {
                    gsLonMin = Math.Floor(min).ToString();
                    double sec = min - Math.Floor(min);
                    gsLonSec = sec.ToString("#0.0");
                }
                //                loadDestination = false;
            }            if (GUILayout.Button("Delete"))            {                FileIO.gdDestinations.Remove(selectedCoordinate.sDestName);                FileIO.SaveData(this);            }            if (GUILayout.Button("Close"))
            {
                loadDestination = false;
                FileIO.SaveData(this);
            }            GUILayout.EndVertical();            GUI.DragWindow();        }

        private void WindowGUI(int windowID)
        {


            GUILayout.BeginVertical(GUILayout.MaxHeight(GPS_GUI_HEIGHT));

            if (gyReceiverOn)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Destination", varButtonStyle))
                {
                    gbDisplayMode = MODE_GPS_DESTINATION;
                    gsModeString = "Destination";
                }
                if (GUILayout.Button("Status", varButtonStyle))
                {
                    gbDisplayMode = MODE_GPS_STATUS;
                    gsModeString = "Status";
                }
                if (GUILayout.Button("Position", varButtonStyle))
                {
                    gbDisplayMode = MODE_GPS_POSITION;
                    gsModeString = "Position";
                }
                GUILayout.EndHorizontal();

                if (gbDisplayMode == MODE_GPS_POSITION)
                {

                    GUILayout.Label("UT: " + gsTime, GUILayout.MinWidth(GPS_GUI_WIDTH - 5.0f));
                    GUILayout.Label("Latitude: " + gsLat, GUILayout.MinWidth(GPS_GUI_WIDTH - 5.0f));
                    GUILayout.Label("Longitude: " + gsLon, GUILayout.MinWidth(GPS_GUI_WIDTH - 5.0f));
                    GUILayout.Label("Altitude: " + gsAltitude, GUILayout.MinWidth(GPS_GUI_WIDTH - 5.0f));
                }
                else if (gbDisplayMode == MODE_GPS_DESTINATION)
                {
                    drawDestinationGUI();
                }
                else
                {
                    GUILayout.Label("Accuracy: " + gsAccuracy);
                    GUILayout.Label("Visible Sats: " + guNumSats.ToString());
                }
            }
            else
            {
                GUILayout.Label("POWER OFF");
                GUILayout.Label("No Data");
            }

            GUILayout.EndVertical();

            GUI.DragWindow();

        }


        /********************************************************************************************
        Function Name: drawDestiationGUI
        Parameters: void
        Return: void
         
        Description:  Draw GPS GUI's Destination  window
         
        *********************************************************************************************/


        //static bool styleInitted = false;
        private void drawDestinationGUI()
        {
#if false
            GUIStyle varTextStyle = new GUIStyle(GUI.skin.textField);
            GUIStyle varHemisphereStyle = new GUIStyle(GUI.skin.textField);
            GUIStyle varLabelStyle = new GUIStyle(GUI.skin.label);
#endif
#if false
            if (!styleInitted)
            {
                styleInitted = true;
                varTextStyle.alignment = TextAnchor.UpperCenter;
                varTextStyle.normal.textColor = varTextStyle.focused.textColor = Color.white;
                varTextStyle.hover.textColor = varTextStyle.active.textColor = Color.yellow;
                varTextStyle.padding = new RectOffset(0, 0, 0, 0);
                varTextStyle.fixedHeight = 16.0f;
                varTextStyle.fixedWidth = 35.0f;

                varHemisphereStyle.alignment = TextAnchor.UpperCenter;
                varHemisphereStyle.normal.textColor = varHemisphereStyle.focused.textColor = Color.white;
                varHemisphereStyle.hover.textColor = varHemisphereStyle.active.textColor = Color.yellow;
                varHemisphereStyle.padding = new RectOffset(0, 0, 0, 0);
                varHemisphereStyle.fixedHeight = 16.0f;
                varHemisphereStyle.fixedWidth = 20.0f;

                varLabelStyle.padding = new RectOffset(0, 0, 0, 7);
            }
#endif
            GUILayout.Label("Distance: " + gsDistance);
            GUILayout.Label("Heading: " + gsHeading);

            GUILayout.BeginVertical(GUILayout.MaxHeight(20.0f));
            GUILayout.BeginHorizontal(GUILayout.MinWidth(GPS_GUI_WIDTH - 5.0f));
            GUILayout.Label("Lat:", varLabelStyle);
            GUILayout.FlexibleSpace();
            gsLatDeg = GUILayout.TextArea(gsLatDeg, 3, varTextStyle);
            GUILayout.Label("°", varLabelStyle);
            gsLatMin = GUILayout.TextArea(gsLatMin, 4, varTextStyle);
            GUILayout.Label("'", varLabelStyle);
            if (!HighLogic.CurrentGame.Parameters.CustomParams<KerbalGPSSettings>().useDecimalMinutes)
            {
                gsLatSec = GUILayout.TextArea(gsLatSec, 4, varTextStyle);
                GUILayout.Label("\"", varLabelStyle);
            }


            //gsLatNS = GUILayout.TextArea(gsLatNS, 1, varHemisphereStyle);
            if (GUILayout.Button(gsLatNS, varButtonStyle, GUILayout.Width(24)))            {                if (gsLatNS == "N")                    gsLatNS = "S";                else                    gsLatNS = "N";            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.MaxHeight(20.0f));
            GUILayout.BeginHorizontal(GUILayout.MinWidth(GPS_GUI_WIDTH - 5.0f));
            GUILayout.Label("Lon:", varLabelStyle);
            GUILayout.FlexibleSpace();
            gsLonDeg = GUILayout.TextArea(gsLonDeg, 3, varTextStyle);
            GUILayout.Label("°", varLabelStyle);
            gsLonMin = GUILayout.TextArea(gsLonMin, 4, varTextStyle);
            GUILayout.Label("'", varLabelStyle);
            if (!HighLogic.CurrentGame.Parameters.CustomParams<KerbalGPSSettings>().useDecimalMinutes)
            {
                gsLonSec = GUILayout.TextArea(gsLonSec, 4, varTextStyle);
                GUILayout.Label("\"", varLabelStyle);
            }

            //gsLonEW = GUILayout.TextArea(gsLonEW, 1, varHemisphereStyle);
            if (GUILayout.Button(gsLonEW, varButtonStyle, GUILayout.Width(24)))            {                if (gsLonEW == "W")                    gsLonEW = "E";                else                    gsLonEW = "W";            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            if ((gsLatDeg.Length != 0) && (gsLatMin.Length > 2) && (gsLatNS.Length != 0) && (gsLonDeg.Length != 0) && (gsLonMin.Length > 2) && (gsLonEW.Length != 0))
            {
                gfDestLat = ParseNS() * (ParseNumericString(gsLatDeg) + ParseNumericString(gsLatMin) / 60.0f + ParseNumericString(gsLatSec) / 3600.0f);
                gfDestLon = ParseEW() * (ParseNumericString(gsLonDeg) + ParseNumericString(gsLonMin) / 60.0f + ParseNumericString(gsLonSec) / 3600.0f);

                if (gfDestLat > 90) gfDestLat = 90.0f;
                if (gfDestLat < -90) gfDestLat = -90.0f;
                if (gfDestLon > 180) gfDestLon = 180.0f;
                if (gfDestLon < -180) gfDestLon = -180.0f;

                gsLatDeg = Math.Floor(Math.Abs(gfDestLat)).ToString();
                double min = (Math.Abs(gfDestLat) - Math.Floor(Math.Abs(gfDestLat))) * 60.0f;
                if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalGPSSettings>().useDecimalMinutes)
                {
                    gsLatMin = min.ToString("#0.0");
                }
                else
                {

                    gsLatMin = Math.Floor(min).ToString();
                    double sec = min - Math.Floor(min);
                    gsLatSec = sec.ToString("#0.0");
                }

                gsLonDeg = Math.Floor(Math.Abs(gfDestLon)).ToString();
                min = (Math.Abs(gfDestLon) - Math.Floor(Math.Abs(gfDestLon))) * 60.0f;
                if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalGPSSettings>().useDecimalMinutes)
                {
                    gsLonMin = min.ToString("#0.0");
                }
                else
                {

                    gsLonMin = Math.Floor(min).ToString();
                    double sec = min - Math.Floor(min);
                    gsLonSec = sec.ToString("#0.0");
                }
            }
            else
            {
                if ((gsLatDeg.Length == 0) || (gsLatNS.Length == 0) || (gsLonDeg.Length == 0) || (gsLonEW.Length == 0))
                {
                    if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalGPSSettings>().useDecimalMinutes)
                    {
                        if (gsLatMin.StartsWith(".")) gsLatMin = "0" + gsLatMin;
                        if (gsLonMin.StartsWith(".")) gsLonMin = "0" + gsLonMin;
                        if (gsLatMin.Length <= 2) gsLatMin = gsLatMin + ".0";
                        if (gsLonMin.Length <= 2) gsLonMin = gsLonMin + ".0";
                    }
                    else
                    {
                        if (gsLatSec.StartsWith(".")) gsLatSec = "0" + gsLatSec;
                        if (gsLonSec.StartsWith(".")) gsLonSec = "0" + gsLonSec;
                        if (gsLatSec.Length <= 2) gsLatSec = gsLatSec + ".0";
                        if (gsLonSec.Length <= 2) gsLonSec = gsLonSec + ".0";
                    }



                }
            }

            if (GUILayout.Button("Set Destination Here", varButtonStyle))
            {
                gsLatDeg = Math.Floor(Math.Abs(gfOrigLat)).ToString();
                double min = (Math.Abs(gfOrigLat) - Math.Floor(Math.Abs(gfOrigLat))) * 60.0f;

                if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalGPSSettings>().useDecimalMinutes)
                {
                    gsLatMin = min.ToString("#0.0");
                }
                else
                {
                    gsLatMin = Math.Floor(min).ToString();
                    double sec = min - Math.Floor(min);
                    gsLatSec = sec.ToString("#0.0");
                }
                if (gfOrigLon > 180.0f) gfOrigLon -= 360.0f;
                if (gfOrigLon < -180.0f) gfOrigLon += 360.0f;

                gsLonDeg = Math.Floor(Math.Abs(gfOrigLon)).ToString();
                min = (Math.Abs(gfOrigLon) - Math.Floor(Math.Abs(gfOrigLon))) * 60.0f;
                if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalGPSSettings>().useDecimalMinutes)
                {
                    gsLonMin = min.ToString("#0.0");
                }
                else
                {
                    gsLonMin = Math.Floor(min).ToString();
                    double sec = min - Math.Floor(min);
                    gsLonSec = sec.ToString("#0.0");
                }
                gsDestName = NONAME;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Dest. name:");
            gsDestName = GUILayout.TextField(gsDestName, GUILayout.Width(GPS_GUI_WIDTH / 2));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (gsDestName == NONAME)
                GUI.enabled = false;
            if (GUILayout.Button("Save", varButtonStyle))            {                FileIO.GPS_Coordinates coordinates = new FileIO.GPS_Coordinates(gsDestName, gfDestLat, gfDestLon);                if (FileIO.gdDestinations.ContainsKey(gsDestName))                {                    FileIO.gdDestinations[gsDestName].fDestLat = gfDestLat;                    FileIO.gdDestinations[gsDestName].fDestLon = gfDestLon;                }                else                    FileIO.gdDestinations.Add(gsDestName, coordinates);                Log.Info("Save, x: " + KerbalGPS.Instance.varWindowPos.x + ",   y: " + KerbalGPS.Instance.varWindowPos.y);                Log.Info("Save, x: " + varWindowPos.x + ",   y: " + varWindowPos.y);                FileIO.SaveData(this);            }
            GUI.enabled = true;
            if (GUILayout.Button("Load", varButtonStyle))            {                loadDestination = true;            }
            GUILayout.EndHorizontal();
        }


        /********************************************************************************************
        Function Name: drawGUI
        Parameters: see function definition
        Return: see function definition
         
        Description:  Initiate an instance of the GUI and assign a callback funcction to draw it.
         
        *********************************************************************************************/

        private void drawGUI()
        {


        }


        /********************************************************************************************
        Function Name: ActivateReceiver
        Parameters: see function definition
        Return: see function definition
         
        Description:  Toggle GNSS receiver on
         
        *********************************************************************************************/

        [KSPEvent(guiActive = true, guiName = "Turn on receiver", name = "Figaro GNSS Receiver")]
        public void ActivateReceiver()
        {
            Log.Info("ActivateReceiver");
            Find_GNSS_Satellites();
            Events["DeactivateReceiver"].active = true;
            Events["ActivateReceiver"].active = false;
            gyReceiverOn = true;

            // Open the receiver UI
            //RenderingManager.AddToPostDrawQueue(3, new Callback(drawGUI)); //start the GUI
            // displayGUI = true; //Moved to toolbar
        }


        /********************************************************************************************
        Function Name: DeactivateReceiver
        Parameters: see function definition
        Return: see function definition
         
        Description:  Toggle GNSS receiver off
         
        *********************************************************************************************/

        [KSPEvent(guiActive = true, guiName = "Turn off receiver", name = "Figaro GNSS Receiver")]
        public void DeactivateReceiver()
        {
            Log.Info("DeactivateReceiver");
            Events["DeactivateReceiver"].active = false;
            Events["ActivateReceiver"].active = true;
            gyReceiverOn = false;

            // close the receiver UI and remove callback function
            //RenderingManager.RemoveFromPostDrawQueue(3, new Callback(drawGUI));
            //displayGUI = false; // moved to Toolbar
        }


        /********************************************************************************************
        Function Name: Initialise_KerbalGPS
        Parameters: see function definition
        Return: see function definition
         
        Description:  Initialises GPS part module and GUI
         
        *********************************************************************************************/

        private void Initialise_KerbalGPS()
        {
            if (!gyKerbalGPSInitialised)
            {
                Log.Error("[KerbalGPS] Loaded Version " + strVersion + "." + strKSPVersion + "." + strSubVersion);
                Log.Error("[KerbalGPS] Reference GNSS Acronym: " + GNSSacronym);

                gyKerbalGPSInitialised = true;
                giLastVesselCount = 0;

                Find_GNSS_Satellites();

                clsGPSMath.Initialise(this, GNSSacronym, SBASacronym);

                if ((varWindowPos.x == 0) && (varWindowPos.y == 0))
                {
                    varWindowPos = new Rect(Screen.width / 5, (7 * Screen.height) / 10, GPS_GUI_WIDTH, GPS_GUI_HEIGHT);
                }

                ActivateReceiver();

            }
        }

        public void Start()
        {
            Log.Info("Start");

            if (!HighLogic.LoadedSceneIsFlight)
                return;
            Instance = this;
            clsGPSMath.Reset();

            Events["DeactivateReceiver"].active = true;
            Events["ActivateReceiver"].active = false;
            gyReceiverOn = true;

            gyKerbalGPSInitialised = false;
            giLastVesselCount = 0;

            gbDisplayMode = MODE_GPS_POSITION;
            gLastSVCheckTime = DateTime.Now;

            FileIO.LoadData(this);
            // GPSToolbar.AppLauncherKerbalGPS.localStart(this.gameObject);
            GameEvents.onHideUI.Add(this.HideUI);
            GameEvents.onShowUI.Add(this.ShowUI);
            GameEvents.onGamePause.Add(this.HideUIWhenPaused);
            GameEvents.onGameUnpause.Add(this.ShowUI);
        }
        /// <summary>
        /// Hides all user interface elements.
        /// </summary>
        bool hideUI = false;
        public void HideUI()
        {
            hideUI = true;
        }
        public void HideUIWhenPaused()
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<KerbalGPSSettings>().hideWhenPaused)
                hideUI = true;
        }
        void ShowUI()
        {
            hideUI = false;
        }

        public void OnDestroy()
        {
            GameEvents.onHideUI.Remove(this.HideUI);
            GameEvents.onShowUI.Remove(this.ShowUI);
            GameEvents.onGamePause.Remove(this.HideUIWhenPaused);
            GameEvents.onGameUnpause.Remove(this.ShowUI);
            Log.Info("OnDestroy");
            CleanUp();
        }


        /********************************************************************************************
        Function Name: ParseNumericString
        Parameters: see function definition
        Return: see function definition
         
        Description:  Parses a numeric string into a floating point number. Returns 0 on failure.
         
        *********************************************************************************************/

        private float ParseNumericString(string strNumber)
        {
            float fReturn;

            if (!float.TryParse(strNumber, varStyle, varCulture, out fReturn)) fReturn = 0.0f;

            return fReturn;
        }


        /********************************************************************************************
        Function Name: ParseNS
        Parameters: see function definition
        Return: see function definition
         
        Description:  Parses a NS string into a floating point number. Returns 0 on failure.
         
        *********************************************************************************************/

        private float ParseNS()
        {
            if (gsLatNS == "N")
            {
                return 1.0f;
            }
            else
            {
                return -1.0f;
            }
        }


        /********************************************************************************************
        Function Name: ParseEW
        Parameters: see function definition
        Return: see function definition
         
        Description:  Parses EW string into a floating point number. Returns 0 on failure.
         
        *********************************************************************************************/

        private float ParseEW()
        {
            if (gsLonEW == "E")
            {
                return 1.0f;
            }
            else
            {
                return -1.0f;
            }

        }

        // attempt to eliminate cross code conflicts

        public void CleanUp()
        {
        }

        private void checkMaster()
        {
            KerbalGPS module;

            if (amIMaster && (!vessel.isActiveVessel || !gyReceiverOn))
            {
                masterSet = false;
                amIMaster = false;
            }
            for (int i = vessel.parts.Count - 1; i >= 0; i--)
            //foreach (Part part in vessel.parts)
            {
                Part part = vessel.parts[i];
                module = part.Modules.GetModule<KerbalGPS>();
                if (module != null)
                {
                    if (module.amIMaster == true)
                    {
                        masterSet = true;
                        break;
                    }
                }
                masterSet = false;
            }

            if (vessel.isActiveVessel && !masterSet)
            {
                for (int i = vessel.parts.Count - 1; i >= 0; i--)
                //foreach (Part part in vessel.parts)
                {
                    Part part = vessel.parts[i];

                    module = part.Modules.GetModule<KerbalGPS>();
                    if (module != null)
                    {
                        if (module == this && gyReceiverOn)
                        {
                            masterSet = true;
                            amIMaster = true;
                            return;
                        }
                        if (module == this && !gyReceiverOn)
                        {
                            masterSet = false;
                            amIMaster = false;
                            return;
                        }
                    }
                }
            }
        }

    }
}
//
// END OF FILE
//

