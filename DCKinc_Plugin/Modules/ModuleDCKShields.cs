using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace DCKinc.Parts
{
    public class ModuleDCKShields : PartModule
    {
        const string modName = "[DCK_Shields]";

        public double totalAmount = 0;
        public double maxAmount = 0;

        [KSPField(isPersistant = true)]
        private bool resourceAvailable;

        [KSPField(isPersistant = true)]
        private bool resourceCheck;

        [KSPField(isPersistant = true)]
        public bool shieldsEnabled;

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
            if (HighLogic.LoadedSceneIsFlight)
            {
                CheckRA();
                checkDeployState();

                if (!resourceAvailable)
                {
                    lowShieldPlasma();
                }
            }
            base.OnUpdate();
        }

        IEnumerator PauseRoutine()
        {
            pauseRoutine = true;
            yield return new WaitForSeconds(5);
            pauseRoutine = false;
        }

        private void ScreenMsg(string msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 4, ScreenMessageStyle.UPPER_CENTER));
        }

        /// <summary>
        /// Checks
        /// </summary>
        private void checkDeployState()
        {
            List<ModuleDCKShields> sParts = new List<ModuleDCKShields>(200);
            foreach (Part p in vessel.Parts)
            {
                sParts.AddRange(p.FindModulesImplementing<ModuleDCKShields>());
            }
            foreach (ModuleDCKShields sPart in sParts)
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
        }

        private void CheckRA()
        {
            foreach (var p in vessel.parts)
            {
                PartResource r = p.Resources.Where(n => n.resourceName == "ShieldPlasma").FirstOrDefault();
                if (r != null)
                {
                    totalAmount += r.amount;
                    maxAmount += r.maxAmount;
                    if (totalAmount < maxAmount * 0.05)
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
                PartResource r = p.Resources.Where(n => n.resourceName == "ShieldPlasma").FirstOrDefault();
                if (r != null)
                {
                    totalAmount += r.amount;
                    maxAmount += r.maxAmount;
                    if (totalAmount < maxAmount * 0.2)
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

        public void BDAcTriggerCheck()
        {
            List<ModuleDCKTrigger> jp = new List<ModuleDCKTrigger>(200);
            foreach (Part p in vessel.Parts)
            {
                jp.AddRange(p.FindModulesImplementing<ModuleDCKTrigger>());
            }
            foreach (ModuleDCKTrigger j in jp)
            {
                if (j.jammerEnabled)
                {
                    EnableShields();
                }
            }
        }

        /// <summary>
        /// Control
        /// </summary>
        private void lowShieldPlasma()
        {
            ScreenMsg("Shield Plasma too low ...");
            DisableShields();
            PauseRoutine();
        }

        public void EnableShields()
        {
            if (!pauseRoutine && !shieldsEnabled)
            {
                CheckRA2();
                List<ModuleDCKShields> shieldParts = new List<ModuleDCKShields>(200);
                foreach (Part p in vessel.Parts)
                {
                    shieldParts.AddRange(p.FindModulesImplementing<ModuleDCKShields>());
                }
                foreach (ModuleDCKShields shieldPart in shieldParts)
                {
                    if (resourceCheck)
                    {
                        ScreenMsg("Deploying Shields");
                        DeployShields();
                    }
                    else
                    {
                        ScreenMsg("Shield Plasma too low ... Shields unable to deploy");
                        PauseRoutine();
                    }
                }
            }
            else
            {
                ScreenMsg("Shields Re-Initializing");
            }
        }

        public void DisableShields()
        {
            if (shieldsEnabled)
            {
                ScreenMsg("Retracting Shields");
                RetractShields();
            }
        }

        public void DeployShields()
        {
            List<ModuleDCKShields> sParts = new List<ModuleDCKShields>(200);
            foreach (Part p in vessel.Parts)
            {
                sParts.AddRange(p.FindModulesImplementing<ModuleDCKShields>());
            }
            foreach (ModuleDCKShields sPart in sParts)
            {
                List<ModuleDeployableRadiator> shieldParts = new List<ModuleDeployableRadiator>(200);
                foreach (Part p in vessel.Parts)
                {
                    shieldParts.AddRange(p.FindModulesImplementing<ModuleDeployableRadiator>());
                }
                foreach (ModuleDeployableRadiator shieldPart in shieldParts)
                {
                    if (!shieldsEnabled)
                    {
                        shieldPart.Extend();
                        shieldsEnabled = true;
                    }
                }
            }
        }

        public void RetractShields()
        {
            List<ModuleDCKShields> sParts = new List<ModuleDCKShields>(200);
            foreach (Part p in vessel.Parts)
            {
                sParts.AddRange(p.FindModulesImplementing<ModuleDCKShields>());
            }
            foreach (ModuleDCKShields sPart in sParts)
            {
                List<ModuleDeployableRadiator> shieldParts = new List<ModuleDeployableRadiator>(200);
                foreach (Part p in vessel.Parts)
                {
                    shieldParts.AddRange(p.FindModulesImplementing<ModuleDeployableRadiator>());
                }
                foreach (ModuleDeployableRadiator shieldPart in shieldParts)
                {
                    if (shieldsEnabled)
                    {
                        shieldPart.Retract();
                        shieldsEnabled = false;
                    }
                }
            }
        }
    }
}
