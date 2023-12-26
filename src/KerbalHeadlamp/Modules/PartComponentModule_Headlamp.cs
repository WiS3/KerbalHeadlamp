using KSP.Sim.impl;

namespace KerbalHeadlamp.Modules
{
    internal class PartComponentModule_Headlamp : PartComponentModule
    {
        public override Type PartBehaviourModuleType => typeof(Module_Headlamp);
    }
}