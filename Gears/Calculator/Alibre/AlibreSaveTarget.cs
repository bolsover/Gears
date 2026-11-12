namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>Where Alibre is configured to save designs (the system option "default save target").</summary>
    public enum AlibreSaveTarget
    {
        /// <summary>The option could not be read or holds a value that is not known.</summary>
        Unknown,

        /// <summary>The Windows file system: the only target that an assembly export can work with.</summary>
        Windows,

        /// <summary>The Alibre Vault.</summary>
        Vault,

        /// <summary>An Alibre AD repository.</summary>
        AdRepository,

        /// <summary>A PDM repository.</summary>
        PdmRepository
    }

    /// <summary>The numeric values of <c>SaveWorkspaceMediator.SAVE_TARGET_*</c> in alibre-client.dll.</summary>
    public sealed class SaveTargetCodes
    {
        public SaveTargetCodes(int windows, int vault, int adRepository, int pdmRepository)
        {
            Windows = windows;
            Vault = vault;
            AdRepository = adRepository;
            PdmRepository = pdmRepository;
        }

        /// <summary>Value of <c>SAVE_TARGET_WINDOWS</c>.</summary>
        public int Windows { get; }

        /// <summary>Value of <c>SAVE_TARGET_VAULT</c>.</summary>
        public int Vault { get; }

        /// <summary>Value of <c>SAVE_TARGET_AD_REPOSITORY</c>.</summary>
        public int AdRepository { get; }

        /// <summary>Value of <c>SAVE_TARGET_PDM_REPOSITORY</c>.</summary>
        public int PdmRepository { get; }

        /// <summary>The values found in alibre-client.dll (<c>SAVE_TARGET_UNKNOWN</c> is 0): Windows 1, Vault 2, AD repository 3, PDM repository 4.</summary>
        public static SaveTargetCodes Documented { get; } = new SaveTargetCodes(1, 2, 3, 4);
    }
}
