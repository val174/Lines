using System.Globalization;
using Lines.Shared.Localization;
using Xunit;

namespace LogicLines.Tests
{
    // Локализация: выбор языка и плюрализация. Тесты не трогают
    // глобальный Strings.Current - вызываются статические/экземплярные
    // члены напрямую, чтобы не влиять друг на друга
    public class StringsTests
    {
        [Fact]
        public void Resolve_ExplicitRu_ReturnsRussian()
        {
            Assert.Same(Strings.Ru, Strings.Resolve("ru"));
            Assert.IsType<StringsRu>(Strings.Resolve("ru"));
        }

        [Fact]
        public void Resolve_ExplicitEn_ReturnsEnglish()
        {
            Assert.Same(Strings.En, Strings.Resolve("en"));
        }

        [Fact]
        public void Resolve_UnknownLanguage_FallsBackToEnglish()
        {
            Assert.Same(Strings.En, Strings.Resolve("de"));
            Assert.Same(Strings.En, Strings.Resolve("xx"));
        }

        [Fact]
        public void Resolve_Auto_FollowsCurrentUICulture()
        {
            // "auto"/пусто - по локали системы: русская локаль -> Ru,
            // остальные -> En (та же логика, что и в Strings.Resolve)
            Strings expected =
                CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru"
                    ? Strings.Ru
                    : Strings.En;

            Assert.Same(expected, Strings.Resolve("auto"));
            Assert.Same(expected, Strings.Resolve(null));
            Assert.Same(expected, Strings.Resolve(""));
        }

        // русская плюрализация: 1 минута, 2 минуты, 5 минут,
        // 11 минут (исключение 11-14), 21 минута, 25 минут
        [Theory]
        [InlineData(1, "1 минута назад")]
        [InlineData(2, "2 минуты назад")]
        [InlineData(5, "5 минут назад")]
        [InlineData(11, "11 минут назад")]
        [InlineData(21, "21 минута назад")]
        [InlineData(25, "25 минут назад")]
        public void Plural_Russian_ThreeForms(int n, string expected)
        {
            Assert.Equal(expected, Strings.Ru.MinutesAgo(n));
        }

        [Theory]
        [InlineData(1, "1 minute ago")]
        [InlineData(2, "2 minutes ago")]
        public void Plural_English_TwoForms(int n, string expected)
        {
            Assert.Equal(expected, Strings.En.MinutesAgo(n));
        }
    }
}
