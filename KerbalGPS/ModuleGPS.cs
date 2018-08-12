using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if false
namespace KerbStar
{
    class ModuleGPSTransmitter: PartModule
    {
        [KSPField]
        public float ecUsageRate = 0.1f;

        [KSPField(isPersistant = true)]
        public bool gpsActive = false;

        [KSPEvent(active = true, guiActive = true, guiActiveEditor = true, guiActiveUnfocused = false, guiName = "Turn on GPS")]
        public void ToggleGPS()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                gpsActive = !gpsActive;
                UpdateLabels();
                UpdateBackgroundProcessing();
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

        void UpdateBackgroundProcessing()
        {
            for (int i = part.Modules.Count - 1; i >= 0; i--)
            if (part.Modules[i].moduleName == "ModuleBackgroundProcessing")
            {
                part.Modules[i].enabled = gpsActive;
            }
        }

        public void FixedUpdate()
        {
            if (gpsActive)
            {
                var ec = part.RequestResource("ElectricCharge", ecUsageRate);
                if (ec < ecUsageRate)
                {
                    gpsActive = false;
                    UpdateLabels();
                }
            }
        }



    }
}
#endif