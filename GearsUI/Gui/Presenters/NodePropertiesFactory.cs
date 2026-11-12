using System;
using System.Linq;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>Chooses the property-grid object for the selected item of a <see cref="GearTrainPresenter"/>.</summary>
    internal sealed class NodePropertiesFactory
    {
        private readonly GearTrainPresenter _presenter;

        public NodePropertiesFactory(GearTrainPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        }

        /// <summary>The properties of <paramref name="selected"/>, or null when it has none or is no longer part of the assembly.</summary>
        public object Create(object selected)
        {
            var assembly = _presenter.Assembly;
            var planetary = _presenter.Planetary;
            if (planetary != null) return new PlanetaryNodeProperties(_presenter, planetary);

            if (selected is RackComponent || selected is BevelGearComponent || selected is WormComponent || selected is WormWheelComponent
                || selected is HtdPulleyComponent || selected is CycloidalGearComponent)
            {
                var component = (GearComponent)selected;
                var componentMount = assembly.FindMount(component, out _);
                if (componentMount == null) return null;
                var relation = assembly.FindDrivingRelation(component);
                if (component is RackComponent rack) return new RackNodeProperties(_presenter, rack, componentMount, relation);
                if (component is HtdPulleyComponent pulley) return new PulleyNodeProperties(_presenter, pulley, componentMount, relation);
                if (component is CycloidalGearComponent cycloidal) return new CycloidalNodeProperties(_presenter, cycloidal, componentMount, relation);
                if (component is HypoidGearComponent hypoid) return new HypoidNodeProperties(_presenter, hypoid, componentMount, relation);
                if (component is SpiralBevelGearComponent spiral) return new SpiralBevelNodeProperties(_presenter, spiral, componentMount, relation);
                if (component is BevelGearComponent bevel) return new BevelNodeProperties(_presenter, bevel, componentMount, relation);
                if (component is WormComponent worm) return new WormNodeProperties(_presenter, worm, componentMount, relation);
                return new WormWheelNodeProperties(_presenter, (WormWheelComponent)component, componentMount, relation);
            }

            if (selected is InvoluteGearComponent gear)
            {
                var mount = assembly.FindMount(gear, out _);
                if (mount == null) return null;
                return new GearNodeProperties(_presenter, gear, mount, assembly.FindDrivingRelation(gear));
            }

            if (selected is Shaft shaft && assembly.Shafts.Contains(shaft))
            {
                return new ShaftNodeProperties(_presenter, shaft);
            }

            if (selected is TimingBelt belt && assembly.Belts.Contains(belt))
            {
                return new BeltNodeProperties(belt);
            }

            return null;
        }
    }
}
