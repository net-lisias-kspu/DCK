using System.Collections.Generic;
using System.Collections;
using System.Linq;
using BDArmory.Parts;
using UnityEngine;

namespace DCKinc.Parts
{
    public class ModuleDCKShields : PartModule
    {
        const string modName = "[DCK_Shields]";

        public double totalAmount = 0;
        public double maxAmount = 0;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Auto Deploy"),
         UI_Toggle(controlEnabled = true, scene = UI_Scene.All, disabledText = "Off", enabledText = "On")]
        public bool autoDeploy = false;

        [KSPField(isPersistant = true)]
        private bool resourceAvailable;

        [KSPField(isPersistant = true)]
        private bool resourceCheck;

        [KSPField(isPersistant = true)]
        private bool shieldsEnabled;

        [KSPField(isPersistant = true)]
        private bool pauseRoutine = false;

        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                RetractShields();
            }
        }

        public override void OnUpdate()
        {
            CheckRA();
            checkDeployState();
            if (autoDeploy)
            {
                BDAcJammerCheck();
            }

            if (!resourceAvailable)
            {
                lowEC();
            }
            base.OnUpdate();
        }

        private void ScreenMsg(string msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 4, ScreenMessageStyle.UPPER_CENTER));
        }

        private void lowEC()
        {
            ScreenMsg("EC too low ...");
            DisableShields();
            PauseRoutine();
        }

        private void checkDeployState()
        {
            List<ModuleActiveRadiator> shieldParts = new List<ModuleActiveRadiator>(200);
            foreach (Part p in vessel.Parts)
            {
                shieldParts.AddRange(p.FindModulesImplementing<ModuleActiveRadiator>());
            }
            foreach (ModuleActiveRadiator shieldPart in shieldParts)
            {
                if (shieldPart.IsCooling)
                {
                    shieldsEnabled = true;
                }
                else
                {
                    shieldsEnabled = false;
                }
            }
        }

        private void CheckRA()
        {
            foreach (var p in vessel.parts)
            {
                PartResource r = p.Resources.Where(n => n.resourceName == "ElectricCharge").FirstOrDefault();
                if (r != null)
                {
                    totalAmount += r.amount;
                    maxAmount += r.maxAmount;
                    if (totalAmount < maxAmount * 0.1)
                    {
                        resourceAvailable = false;
                    }
                    else
                    {
                        resourceAvailable = true;
                    }
                }
            }
        }

        private void CheckRA2()
        {
            foreach (var p in vessel.parts)
            {
                PartResource r = p.Resources.Where(n => n.resourceName == "ElectricCharge").FirstOrDefault();
                if (r != null)
                {
                    totalAmount += r.amount;
                    maxAmount += r.maxAmount;
                    if (totalAmount < maxAmount * 0.35)
                    {
                        resourceCheck = false;
                    }
                    else
                    {
                        resourceCheck = true;
                    }
                }
            }
        }

        public void BDAcJammerCheck()
        {
            List<ModuleECMJammer> jp = new List<ModuleECMJammer>(200);
            foreach (Part p in vessel.Parts)
            {
                jp.AddRange(p.FindModulesImplementing<ModuleECMJammer>());
            }
            foreach (ModuleECMJammer j in jp)
            {
                if (j.jammerEnabled && !shieldsEnabled)
                {
                    EnableShields();
                }
            }
        }

        IEnumerator PauseRoutine()
        {
            pauseRoutine = true;
            yield return new WaitForSeconds(5);
            pauseRoutine = false;
        }

        public void EnableShields()
        {
            CheckRA2();
            if (!pauseRoutine)
            {
                List<ModuleDeployableRadiator> shieldParts = new List<ModuleDeployableRadiator>(200);
                foreach (Part p in vessel.Parts)
                {
                    shieldParts.AddRange(p.FindModulesImplementing<ModuleDeployableRadiator>());
                }
                foreach (ModuleDeployableRadiator shieldPart in shieldParts)
                {
                    if (resourceCheck)
                    {
                        shieldPart.Extend();
                        ScreenMsg("Shields deploying");
                        shieldsEnabled = true;
                    }
                    else
                    {
                        ScreenMsg("EC too low ... Shields unable to deploy");
                        shieldsEnabled = false;
                        PauseRoutine();
                    }
                }
            }
            else
            {
                ScreenMsg("Shields Re-Initializing");
            }
        }

        public void RetractShields()
        {
            List<ModuleDeployableRadiator> shieldParts = new List<ModuleDeployableRadiator>(200);
            foreach (Part p in vessel.Parts)
            {
                shieldParts.AddRange(p.FindModulesImplementing<ModuleDeployableRadiator>());
            }
            foreach (ModuleDeployableRadiator shieldPart in shieldParts)
            {
                shieldPart.Retract();
                shieldsEnabled = false;
            }
        }

        public void DisableShields()
        {
            List<ModuleActiveRadiator> shieldParts = new List<ModuleActiveRadiator>(200);
            foreach (Part p in vessel.Parts)
            {
                shieldParts.AddRange(p.FindModulesImplementing<ModuleActiveRadiator>());
            }
            foreach (ModuleActiveRadiator shieldPart in shieldParts)
            {
                if (shieldPart.IsCooling)
                {
                    ScreenMsg("Shields Disabled ... Retracting");
                    RetractShields();
                }
            }
        }

    }
}
