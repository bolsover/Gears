using System;
using Bolsover.Gears.Calculator.Alibre;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// Tests of the Alibre save target classification and refusal messages. These need no running Alibre; reading the real
    /// user profile is not covered here.
    /// </summary>
    [TestFixture]
    public class TestAlibreSaveTarget
    {
        private static readonly SaveTargetCodes Codes = SaveTargetCodes.Documented;

        [Test]
        public void Classify_Windows_IsSafe()
        {
            var target = AlibreSaveTargetReader.Classify(1, Codes);

            Assert.AreEqual(AlibreSaveTarget.Windows, target);
            Assert.IsTrue(AlibreSaveTargetReader.IsSafe(target));
        }

        [TestCase(2, AlibreSaveTarget.Vault)]
        [TestCase(3, AlibreSaveTarget.AdRepository)]
        [TestCase(4, AlibreSaveTarget.PdmRepository)]
        public void Classify_OtherTargets_AreNotSafe(int code, AlibreSaveTarget expected)
        {
            var target = AlibreSaveTargetReader.Classify(code, Codes);

            Assert.AreEqual(expected, target);
            Assert.IsFalse(AlibreSaveTargetReader.IsSafe(target));
        }

        [TestCase(0)]
        [TestCase(99)]
        [TestCase(-1)]
        public void Classify_UnknownOrOutOfRangeCode_IsUnknownAndNotSafe(int code)
        {
            var target = AlibreSaveTargetReader.Classify(code, Codes);

            Assert.AreEqual(AlibreSaveTarget.Unknown, target);
            Assert.IsFalse(AlibreSaveTargetReader.IsSafe(target));
        }

        [Test]
        public void Classify_UsesTheCodesTableItIsGiven()
        {
            var shifted = new SaveTargetCodes(10, 20, 30, 40);

            Assert.AreEqual(AlibreSaveTarget.PdmRepository, AlibreSaveTargetReader.Classify(40, shifted));
            Assert.AreEqual(AlibreSaveTarget.Unknown, AlibreSaveTargetReader.Classify(4, shifted));
            Assert.Throws<ArgumentNullException>(() => AlibreSaveTargetReader.Classify(1, null));
        }

        [Test]
        public void IsSafe_OnlyForWindows()
        {
            foreach (AlibreSaveTarget target in Enum.GetValues(typeof(AlibreSaveTarget)))
            {
                Assert.AreEqual(target == AlibreSaveTarget.Windows, AlibreSaveTargetReader.IsSafe(target), target.ToString());
            }
        }

        [Test]
        public void DescribeRefusal_NamesTheTargetAndTheRemedy()
        {
            var vault = AlibreSaveTargetReader.DescribeRefusal(AlibreSaveTarget.Vault);
            var pdm = AlibreSaveTargetReader.DescribeRefusal(AlibreSaveTarget.PdmRepository);

            StringAssert.Contains("Vault", vault);
            StringAssert.Contains("PDM", pdm);
            StringAssert.Contains("Windows file system", vault);
            StringAssert.Contains("Windows file system", pdm);
            Assert.AreNotEqual(vault, pdm);
        }

        [Test]
        public void DescribeRefusal_EveryUnsafeTargetHasADistinctMessage()
        {
            var messages = new[]
            {
                AlibreSaveTargetReader.DescribeRefusal(AlibreSaveTarget.Vault),
                AlibreSaveTargetReader.DescribeRefusal(AlibreSaveTarget.PdmRepository),
                AlibreSaveTargetReader.DescribeRefusal(AlibreSaveTarget.AdRepository),
                AlibreSaveTargetReader.DescribeRefusal(AlibreSaveTarget.Unknown)
            };

            CollectionAssert.AllItemsAreUnique(messages);
            foreach (var message in messages)
            {
                StringAssert.Contains("Windows file system", message);
            }
        }

        [Test]
        public void DocumentedCodes_MatchTheValuesFoundInAlibreClient()
        {
            Assert.AreEqual(1, Codes.Windows);
            Assert.AreEqual(2, Codes.Vault);
            Assert.AreEqual(3, Codes.AdRepository);
            Assert.AreEqual(4, Codes.PdmRepository);
        }
    }
}
