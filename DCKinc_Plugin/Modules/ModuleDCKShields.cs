using System;
using System.Collections.Generic;
using System.Linq;
using BDArmory;
using BDArmory.UI;
using DCKinc.AAcustomization;
using UnityEngine;

namespace DCKinc.Parts
{
    public class ModuleDCKShields : PartModule
    {
        const string modName = "DCK_Shields";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Auto Deploy"),
         UI_Toggle(controlEnabled = true, scene = UI_Scene.All, disabledText = "Off", enabledText = "On")]
        public bool autoDeploy = false;

        public MissileFire BDAWM;


        public override void OnUpdate()
        {
            if (autoDeploy)
            {
                if (BDAWM && (BDAWM.missileIsIncoming || BDAWM.isChaffing || BDAWM.isFlaring || BDAWM.underFire))
                {
                    EnableShields();
                }
            }
            base.OnUpdate();
        }


        public void EnableShields()
        {
            List<ModuleDeployableRadiator> shieldParts = new List<ModuleDeployableRadiator>(200);
            foreach (Part p in vessel.Parts)
            {
                shieldParts.AddRange(p.FindModulesImplementing<ModuleDeployableRadiator>());
            }
            foreach (ModuleDeployableRadiator shieldPart in shieldParts)
            {
                shieldPart.Extend();
            }
        }


        public void DisableShields()
        {
            List<ModuleDeployableRadiator> shieldParts = new List<ModuleDeployableRadiator>(200);
            foreach (Part p in vessel.Parts)
            {
                shieldParts.AddRange(p.FindModulesImplementing<ModuleDeployableRadiator>());
            }
            foreach (ModuleDeployableRadiator shieldPart in shieldParts)
            {
                shieldPart.Retract();
            }
        }
    }
}
