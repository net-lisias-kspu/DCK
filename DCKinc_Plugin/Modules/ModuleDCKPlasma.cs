using System.Collections.Generic;
namespace DCKinc.Parts
{
    public class ModuleDCKPlasma : PartModule
    {
        [KSPField(isPersistant = true)]
        public bool plasmaEnabled;

        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                checkState();
            }
        }
        /*
        public override void OnUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (!plasmaEnabled)
                {
                    checkState();
                }
            }
            base.OnUpdate();
        }
        */
        private void checkState()
        {
            List<ModuleDCKPlasma> sParts = new List<ModuleDCKPlasma>(200);
            foreach (Part p in vessel.Parts)
            {
                sParts.AddRange(p.FindModulesImplementing<ModuleDCKPlasma>());
            }
            foreach (ModuleDCKPlasma sPart in sParts)
            {
                List<ModuleResourceConverter> shieldParts = new List<ModuleResourceConverter>(200);
                foreach (Part p in vessel.Parts)
                {
                    shieldParts.AddRange(p.FindModulesImplementing<ModuleResourceConverter>());
                }
                foreach (ModuleResourceConverter plasmaPart in shieldParts)
                {
                    if (!plasmaPart.IsActivated || !plasmaPart.AlwaysActive)
                    {
                        plasmaPart.AlwaysActive = true;
                        plasmaPart.IsActivated = true;
                        plasmaEnabled = true;
                    }
                    else
                    {
                        plasmaEnabled = true;
                    }
                }
            }
        }
    }
}