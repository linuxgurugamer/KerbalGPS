using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace KerbStar
{
    class ModuleGPSTransmitter: PartModule, IModuleInfo
    {
        const string MODULETITLE = "GPS Transmitter";
        int ElectricityId;

        [KSPField(isPersistant = true)]
        public double gpsRange = 500000f; // in meters


        [KSPField(isPersistant = true)]
        public bool gpsActive = false;

        [KSPEvent(active = true, guiActive = true, guiActiveEditor = true, guiActiveUnfocused = false, guiName = "Turn on GPS")]
        public void ToggleGPS()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                gpsActive = !gpsActive;
                UpdateLabels();
            }
        }

        void UpdateLabels()
        {
            if (gpsActive)
            {
                Events["ToggleGPS"].guiName = "Turn off GPS";
            }
            else
            {
                Events["ToggleGPS"].guiName = "Turn on GPS";
            }

        }
        void Start()
        {
             ElectricityId = PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id;
        }

        public void FixedUpdate()
        {
            if (gpsActive)
            {
                double amount, maxAmount;
                part.GetConnectedResourceTotals(ElectricityId, out amount, out maxAmount);

                if (amount < 0.001)
                {
                    gpsActive = false;                    
                }
                UpdateLabels();
            }
        }
        
        // IModuleInfo follows
        public string GetModuleTitle()
        {
            return MODULETITLE;
        }
        public override string GetModuleDisplayName()
        {
            return Localizer.Format(MODULETITLE);
        }

        public Callback<Rect> GetDrawModulePanelCallback()
        {
            return null;
        }

        public string GetPrimaryField()
        {
            return "";
        }
        public override string GetInfo()
        {
            return "GPS Range: " + gpsRange + "m" + "\n" + base.resHandler.PrintModuleResources(1); 
        }

    }
}
