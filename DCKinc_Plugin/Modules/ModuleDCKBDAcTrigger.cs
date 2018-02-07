using BDArmory.Parts;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace DCKinc.Parts
{
    public class ModuleDCKTrigger : ModuleECMJammer
    {
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Auto Deploy"),
         UI_Toggle(controlEnabled = true, scene = UI_Scene.All, disabledText = "Off", enabledText = "On")]
        public bool autoDeploy = false;

        public override void OnStart(StartState state)
        {
        }

        public override void OnUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (autoDeploy)
                {
                    CheckShields();
                }
            }
            base.OnUpdate();
        }

        public void CheckShields()
        {
            if (jammerEnabled)
            {
                List<ModuleDCKShields> shieldParts = new List<ModuleDCKShields>(200);
                foreach (Part p in vessel.Parts)
                {
                    shieldParts.AddRange(p.FindModulesImplementing<ModuleDCKShields>());
                }
                foreach (ModuleDCKShields shieldPart in shieldParts)
                {
                    if (!shieldPart.shieldsEnabled)
                    {
                        shieldPart.EnableShields();
                    }
                }
            }
        }
    }
}