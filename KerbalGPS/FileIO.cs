using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using KSP.UI.Screens;
using UnityEngine;
using UnityEngine.UI;
using ClickThroughFix;
using ToolbarControl_NS;

namespace KerbStar
{
    public static class FileIO
    {


        static string PLUGINDATA = KSPUtil.ApplicationRootPath + "GameData/KerbalGPS/PluginData/Settings.cfg";
        const string DATANODE = "KerbalGPS";
        const string DESTNODE = "Destination";
        const string WINDOWPOSNODE = "WINDOW_POS";

        public class GPS_Coordinates
        {
            public string sDestName;
            public string sCelestialBodyName;
            public float fDestLat;
            public float fDestLon;

            public GPS_Coordinates()
            { }

            public GPS_Coordinates(string name, float lat, float lon)
            {
                sDestName = name;
                sCelestialBodyName = FlightGlobals.ActiveVessel.mainBody.name;
                fDestLat = lat;
                fDestLon = lon;
            }
        }
        static public SortedDictionary<string, GPS_Coordinates> gdDestinations = new SortedDictionary<string, GPS_Coordinates>(StringComparer.CurrentCultureIgnoreCase);


        internal static string SafeLoad(string value, double oldvalue)
        {
            if (value == null)
                return oldvalue.ToString();
            return value;
        }

        static void SaveWinPos(ConfigNode settings, string winName, Rect win)
        {
            settings.SetValue(winName + "X", win.x.ToString(), true);
            settings.SetValue(winName + "Y", win.y.ToString(), true);
        }

        static void SaveWindowPositions(KerbalGPS Instance, ConfigNode settings)
        {
            ConfigNode winPos = new ConfigNode();

            SaveWinPos(winPos, "varWindowPos", Instance.varWindowPos);
            SaveWinPos(winPos, "varDestWindowPos", Instance.varDestWindowPos);
            settings.AddNode(WINDOWPOSNODE, winPos);
        }

        static Rect GetWinPos(ConfigNode settings, string winName, float width, float height)
        {
            double x = (Screen.width - width) / 2;
            double y = (Screen.height - height) / 2;

            x = Double.Parse(SafeLoad(settings.GetValue(winName + "X"), x));
            y = Double.Parse(SafeLoad(settings.GetValue(winName + "Y"), y));
            Log.Info("GetWinPos, win: " + winName + ",    x,y: " + x.ToString("N0") + ", " + y.ToString("N0"));
            var r = new Rect((float)x, (float)y, width, height);
            return r;
        }

        static void LoadWindowPositions(KerbalGPS Instance, ConfigNode settings)
        {
            ConfigNode winPos = settings.GetNode(WINDOWPOSNODE);
            if (winPos == null)
            {
                Log.Info("Unable to load window positions");
                return;
            }
            
            Instance.varWindowPos = GetWinPos(winPos, "varWindowPos", KerbalGPS.GPS_GUI_WIDTH, KerbalGPS.GPS_GUI_HEIGHT);
            Instance.varDestWindowPos = GetWinPos(winPos, "varDestWindowPos", KerbalGPS.GPS_GUI_WIDTH, Instance.GPS_DEST_GUI_HEIGHT);
   
        }
       
        static public void LoadData(KerbalGPS Instance)
        {
            gdDestinations.Clear();
            if (File.Exists(PLUGINDATA))
            {
                ConfigNode dataFile = ConfigNode.Load(PLUGINDATA);
                if (dataFile != null)
                {

                    Log.Info("Datafile opened and loaded");
                    ConfigNode dataNode = dataFile.GetNode(DATANODE);
                    if (dataNode != null)
                    {

                        Log.Info("Nodes loaded");

                        // load screen  coordinates here
                        LoadWindowPositions(Instance, dataNode);


                        ConfigNode[] entries = dataNode.GetNodes(DESTNODE);
                        Log.Info("Entries loaded: " + entries.Length);
                        foreach (var entry in entries)
                        {
                            GPS_Coordinates coordinates = new GPS_Coordinates();

                            coordinates.sDestName = entry.GetValue("sDestName");
                            coordinates.sCelestialBodyName = entry.GetValue("sCelestialBodyName");
                            if (coordinates.sCelestialBodyName == null)
                                coordinates.sCelestialBodyName = FlightGlobals.GetHomeBody().name;
                            coordinates.fDestLat = (float)Double.Parse(SafeLoad(entry.GetValue("fDestLat"), KerbalGPS.DEF_DESTLAT));
                            coordinates.fDestLon = (float)Double.Parse(SafeLoad(entry.GetValue("fDestLon"), KerbalGPS.DEF_DESTLON));
                            gdDestinations.Add(coordinates.sDestName, coordinates);
                            Log.Info("sDestName: " + coordinates.sDestName + ",  fDestLat: " + coordinates.fDestLat + ", fDestLon: " + coordinates.fDestLon);
                        }
                    }
                }
            }
        }

        static public void SaveData(KerbalGPS Instance)
        {
            ConfigNode dataFile = new ConfigNode(DATANODE);
            ConfigNode dataNode = new ConfigNode(DATANODE);
            dataFile.AddNode(dataNode);

            // Save screen coordinates here
            SaveWindowPositions(Instance, dataNode);

            foreach (var entry in gdDestinations)
            {
                ConfigNode n = new ConfigNode();

                n.AddValue("sDestName", entry.Key);
                n.AddValue("sCelestialBodyName", entry.Value.sCelestialBodyName);
                n.AddValue("fDestLat", entry.Value.fDestLat);
                n.AddValue("fDestLon", entry.Value.fDestLon);
                dataNode.AddNode(DESTNODE, n);
            }
            dataFile.Save(PLUGINDATA);
        }


    }
}
