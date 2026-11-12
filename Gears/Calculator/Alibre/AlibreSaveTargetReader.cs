using System;
using System.Runtime.CompilerServices;
using AlibreX;
using com.alibre.client;
using com.alibre.engineering.client;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>
    /// Works out where Alibre is configured to save designs. An assembly export saves every part to a Windows folder before it adds
    /// the part to the assembly, so only <see cref="AlibreSaveTarget.Windows"/> is safe; the Vault, a PDM repository, an AD repository
    /// and an unknown target are all refused.
    /// </summary>
    public static class AlibreSaveTargetReader
    {
        /// <summary>Maps the numeric save target of the Alibre user profile to <see cref="AlibreSaveTarget"/>; a code that is not in <paramref name="codes"/> is Unknown.</summary>
        public static AlibreSaveTarget Classify(int code, SaveTargetCodes codes)
        {
            if (codes == null) throw new ArgumentNullException(nameof(codes));

            if (code == codes.Windows) return AlibreSaveTarget.Windows;
            if (code == codes.Vault) return AlibreSaveTarget.Vault;
            if (code == codes.PdmRepository) return AlibreSaveTarget.PdmRepository;
            if (code == codes.AdRepository) return AlibreSaveTarget.AdRepository;
            return AlibreSaveTarget.Unknown;
        }

        /// <summary>
        /// Reads where Alibre is configured to save designs. The value comes from the Alibre user profile (the "default save target"
        /// system option). When that cannot be read, <c>IADVaultInfo.IsUsingVault</c> decides: true is the Vault, false the Windows file system.
        /// <paramref name="source"/> says which of the two was used, for the export log. An unreadable option and an unreadable Vault flag
        /// together give <see cref="AlibreSaveTarget.Unknown"/>, which is refused.
        /// </summary>
        public static AlibreSaveTarget Read(IADRoot root, out string source)
        {
            if (TryReadProfile(out var code, out var codes, out var profileError))
            {
                source = "Alibre user profile, value " + code;
                return Classify(code, codes);
            }

            try
            {
                var usingVault = root.VaultInfo.IsUsingVault;
                source = "fallback to IADVaultInfo.IsUsingVault = " + usingVault + " (user profile not readable: " + profileError + ")";
                return usingVault ? AlibreSaveTarget.Vault : AlibreSaveTarget.Windows;
            }
            catch (Exception vaultError)
            {
                source = "not readable (user profile: " + profileError + "; vault info: " + vaultError.Message + ")";
                return AlibreSaveTarget.Unknown;
            }
        }

        /// <summary>
        /// The only place that uses alibre-client.dll types. Never throws: a missing library, an uninitialised client context or a missing
        /// or non-numeric profile value all report false and the reason in <paramref name="error"/>. Not inlined so that the caller can be
        /// jitted (and unit tested) without alibre-client.dll being loaded.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TryReadProfile(out int code, out SaveTargetCodes codes, out string error)
        {
            code = 0;
            codes = null;
            try
            {
                var value = ClientContext.Singleton.UserProfile.getValue(SaveWorkspaceMediator.DEFAULT_SAVE_TARGET_KEY);
                if (value == null)
                {
                    error = "no value for " + SaveWorkspaceMediator.DEFAULT_SAVE_TARGET_KEY;
                    return false;
                }

                code = Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
                codes = new SaveTargetCodes(
                    SaveWorkspaceMediator.SAVE_TARGET_WINDOWS,
                    SaveWorkspaceMediator.SAVE_TARGET_VAULT,
                    SaveWorkspaceMediator.SAVE_TARGET_AD_REPOSITORY,
                    SaveWorkspaceMediator.SAVE_TARGET_PDM_REPOSITORY);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        /// <summary>True only for the Windows file system.</summary>
        public static bool IsSafe(AlibreSaveTarget target)
        {
            return target == AlibreSaveTarget.Windows;
        }

        /// <summary>The user-facing display name of the target, as used in messages and the export log.</summary>
        public static string DescribeTarget(AlibreSaveTarget target)
        {
            switch (target)
            {
                case AlibreSaveTarget.Windows: return "the Windows file system";
                case AlibreSaveTarget.Vault: return "the Alibre Vault";
                case AlibreSaveTarget.AdRepository: return "an Alibre AD repository";
                case AlibreSaveTarget.PdmRepository: return "a PDM repository";
                default: return "an unknown location";
            }
        }

        /// <summary>The message of the exception thrown when the export is refused because Alibre does not save to the Windows file system.</summary>
        public static string DescribeRefusal(AlibreSaveTarget target)
        {
            const string advice = " The Alibre API cannot change this option, and the parts of the assembly cannot be saved to a Windows folder " +
                                  "while it is on. In Alibre Design, change the system option to save files to the Windows file system, then export again.";

            switch (target)
            {
                case AlibreSaveTarget.Windows:
                    return "Alibre is set to save designs to the Windows file system.";
                case AlibreSaveTarget.Unknown:
                    return "The Alibre save location could not be determined." + advice;
                default:
                    return "Alibre is set to save designs to " + DescribeTarget(target) + "." + advice;
            }
        }
    }
}
