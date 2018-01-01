using System.Collections.Generic;
using System.Text;
using System;
using DCKinc.AAcustomization;
using BDArmory.Radar;
using BDArmory;
using UnityEngine;

namespace DCKinc.Parts
{
    public class ModuleDCKShields : PartModule
    {
//        [KSPField] public float jammerStrength = 700;

//        [KSPField] public float lockBreakerStrength = 500;

//        [KSPField] public float rcsReductionFactor = 0.75f;

        [KSPField] public double ECMultiplier = 5;

//        [KSPField] public bool signalSpam = true;

//        [KSPField] public bool lockBreaker = true;

//        [KSPField] public bool rcsReduction = false;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Enabled")] public bool shieldsEnabled;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Auto Deploy")] public bool autoDeploy;

        [KSPEvent(guiActiveEditor = false, guiActive = true, guiName = "Auto Deploy")]
        public void AutoDeploy()
        {
            if (shieldsEnabled)
            {
                return;
            }
            else
            {
//                CheckRWR();
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight) return;
            part.force_activate();
//            List<MissileFire>.Enumerator wm = vessel.FindPartModulesImplementing<MissileFire>().GetEnumerator();
//            while (wm.MoveNext())
//            {
//                if (wm.Current == null) continue;
//                wm.Current.jammers.Add(this);
//            }
//            wm.Dispose();

            GameEvents.onVesselCreate.Add(OnVesselCreate);
        }

        void OnDestroy()
        {
            GameEvents.onVesselCreate.Remove(OnVesselCreate);
        }

        void OnVesselCreate(Vessel v)
        {
            CheckShields();
        }

        public void EnableShields()
        {
            CheckShields();
            shieldsEnabled = true;
        }

        public void DisableShields()
        {
            CheckShields();
            shieldsEnabled = false;
        }

        public void CheckRWR()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (RadarWarningReceiver.WindowRectRWRInitialized)
                {
                    EnableShields();
                }
            }
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (shieldsEnabled)
            {
                CheckShields();
                ECdrain();
            }
        }

        void CheckShields()
        {
            var thisPart = this.part.FindModuleImplementing<DCKStextureswitch2>();                  //This is so the below code knows the part it's dealing with is a cargo bay.
            if (thisPart != null)                                                               //Verify it's actually a cargo bay)
            {
                var thisPartAnimate = this.part.FindModuleImplementing<ModuleAnimateGeneric>();
                if (thisPartAnimate != null)
                {
                    if (thisPartAnimate.aniState == ModuleAnimateGeneric.animationStates.MOVING)
                    {
                        Events["TriggerAllShields"].active = false;
                    }
                    if (thisPartAnimate.animSwitch)
                    {
                        Events["TriggerAllShields"].guiName = "Retract Shields";
                        Events["TriggerAllShields"].active = true;
                    }
                    else
                    {
                        Events["TriggerAllShields"].guiName = "Deploy Shields";
                        Events["TriggerAllShields"].active = true;
                    }
                }
            }
        }

        public void TriggerAllShields()
        {
            bool ShieldsDeployed = false;

            var callingPart = this.part.FindModuleImplementing<ModuleAnimateGeneric>();   //Variable for the part doing the work.

            if (callingPart.animSwitch)
            {
                ShieldsDeployed = true;                                                     
            }

            foreach (Part eachPart in vessel.Parts)
            {
                var thisPartModule = eachPart.FindModuleImplementing<DCKStextureswitch2>();
                if (thisPartModule != null)
                {
                    var thisPartAnimate = eachPart.FindModuleImplementing<ModuleAnimateGeneric>();
                    if (thisPartAnimate != null)
                    {
                        KSPActionParam param = new KSPActionParam(KSPActionGroup.Abort, KSPActionType.Activate);
                        if (ShieldsDeployed)
                        {
                            if (thisPartAnimate.animSwitch)
                            {
                                thisPartAnimate.ToggleAction(param);
                            }
                        }
                        else
                        {
                            if (!thisPartAnimate.animSwitch)
                            {
                                thisPartAnimate.ToggleAction(param);
                            }
                        }
                    }
                }
            }
        }


        protected void ECdrain()
        {
            if (ECMultiplier <= 0)
            {
                return;
            }

            double drainAmount = ECMultiplier * TimeWarp.fixedDeltaTime;
            double chargeAvailable = part.RequestResource("ElectricCharge", drainAmount, ResourceFlowMode.ALL_VESSEL);
            if (chargeAvailable < drainAmount * 0.1f)
            {
                DisableShields();
            }
        }
    }
}