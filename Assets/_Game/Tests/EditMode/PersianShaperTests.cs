using MyFutureCity.Core;
using NUnit.Framework;

namespace MyFutureCity.Tests
{
    [TestFixture]
    public class PersianShaperTests
    {
        [Test]
        public void SalamUsesLamAlefLigatureAndReverses()
        {
            // س (initial) + لا (final ligature) + م (isolated), shown right-to-left
            Assert.AreEqual("\uFEE1\uFEFC\uFEB3", PersianShaper.Shape("سلام"));
        }

        [Test]
        public void AsciiDigitsBecomePersianAndKeepOrder()
        {
            Assert.AreEqual("\u06F1\u06F2\u06F3", PersianShaper.Shape("123"));
            Assert.AreEqual("123", PersianShaper.Shape("123", false));
        }

        [Test]
        public void NumberInsideSentenceStaysLeftToRight()
        {
            // "تعداد: 3" -> digit ends up at the visual left end, label reversed after it
            Assert.AreEqual("\u06F3 :\uFEA9\uFE8D\uFEAA\uFECC\uFE97", PersianShaper.Shape("تعداد: 3"));
        }

        [Test]
        public void BracketsAreMirroredInRtlRun()
        {
            Assert.AreEqual("(\uFED2\uFEDF\uFE8D)", PersianShaper.Shape("(الف)"));
        }

        [Test]
        public void ZwnjBreaksJoiningAndIsRemoved()
        {
            Assert.AreEqual("\uFEA9\uFEED\uFEAD\uFBFD\uFEE3", PersianShaper.Shape("می\u200Cرود"));
        }

        [Test]
        public void ArabicKafAndYehNormalizeToPersian()
        {
            Assert.AreEqual("\uFBFD\uFB90", PersianShaper.Shape("كي"));
        }

        [Test]
        public void HarakatAreDropped()
        {
            Assert.AreEqual(PersianShaper.Shape("سلام"), PersianShaper.Shape("سَلام"));
        }

        [Test]
        public void LatinWordInsidePersianKeepsItsOrder()
        {
            string s = PersianShaper.Shape("بازی Unity خوب");
            Assert.IsTrue(s.Contains("Unity"));
        }

        [Test]
        public void EmptyAndNullAreSafe()
        {
            Assert.AreEqual("", PersianShaper.Shape(""));
            Assert.AreEqual("", PersianShaper.Shape(null));
            Assert.AreEqual("", PersianShaper.Fix(null, 10));
        }

        [Test]
        public void WrapBreaksOnWordBoundaries()
        {
            Assert.AreEqual("aa bb\ncc", PersianShaper.Wrap("aa bb cc", 5));
            Assert.AreEqual("aa bb cc", PersianShaper.Wrap("aa bb cc", 0));
            Assert.AreEqual("longword", PersianShaper.Wrap("longword", 3));
        }

        [Test]
        public void MultiLineTextShapesEachLineSeparately()
        {
            string s = PersianShaper.Shape("سلام\nسلام");
            var parts = s.Split('\n');
            Assert.AreEqual(2, parts.Length);
            Assert.AreEqual(parts[0], parts[1]);
        }
    }
}
