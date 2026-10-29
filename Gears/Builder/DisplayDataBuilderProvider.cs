using Bolsover.Gears.Model;

namespace Bolsover.Gears.Builder;

public static class DisplayDataBuilderProvider
{
    public static IDisplayDataBuilder Get(Gear gear)
    {
        switch (gear.Context)
        {
            case GearTypes.ProfileShiftedExternalInvolutePair:
                return new ProfileShiftedExternalInvolutePairDisplayBuilder();
            
            case GearTypes.ProfileShiftedExternalNormalHelicalPair:
                return new  ProfileShiftedExternalNormalHelicalPairDisplayBuilder();
            
            default: throw new System.NotImplementedException();
        } 
    }
}

