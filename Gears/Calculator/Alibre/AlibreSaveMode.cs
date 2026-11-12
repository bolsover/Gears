namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>Where Alibre saves the parts of an exported assembly, which decides how <see cref="AlibreGearExporter.ExportAssembly"/> builds it.</summary>
    public enum AlibreSaveMode
    {
        /// <summary>Read the Alibre option (<c>IADVaultInfo.IsUsingVault</c>) and choose the matching strategy.</summary>
        Auto,

        /// <summary>Alibre saves to the Windows file system: build and save every part to a folder, then create the assembly and add the parts.</summary>
        WindowsFolder,

        /// <summary>Alibre saves to the Alibre Vault (PDM): create the assembly first and every part inside it.</summary>
        Vault
    }
}
