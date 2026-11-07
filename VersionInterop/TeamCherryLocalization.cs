#if HK1578_OR_OLDER
namespace TeamCherry.Localization;

internal static class Language
{
    public static LanguageCode CurrentLanguage() => global::Language.Language.CurrentLanguage();
}
#endif
