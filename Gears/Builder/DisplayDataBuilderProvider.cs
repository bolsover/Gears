using Bolsover.Gears.Model;

namespace Bolsover.Gears.Builder;

public class DisplayDataBuilderProvider
{
    public static IDisplayDataBuilder Get(Gear gear)
    {
        switch (gear.Context)
        {
            case GearContext.ProfileShiftedExternalInvolutePair:
                return new ProfileShiftedExternalInvolutePairDisplayBuilder();
            
            case GearContext.ProfileShiftedExternalNormalHelicalPair:
                return new  ProfileShiftedExternalNormalHelicalPairDisplayBuilder();
            
            default: throw new System.NotImplementedException();
        } 
    }
}

