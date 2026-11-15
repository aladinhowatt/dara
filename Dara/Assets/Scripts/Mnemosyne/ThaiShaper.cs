using System.Collections.Generic;
using System.Text;

namespace Mnemosyne
{
    /// <summary>
    /// TextMeshPro does not run OpenType GSUB, so Noto Sans Thai cannot stack a tone mark
    /// above an upper vowel on its own. The project fonts (Assets/Font/NotoSansThai-*.ttf) were
    /// extended with private-use glyphs; this class performs the substitution before text is shown.
    ///   U+F730..F734 : tone marks raised above an upper vowel (mai ek, tho, tri, chattawa, thanthakhat)
    ///   U+F720..F724 : tone marks used over tall consonants (po pla, fo fa, fo fan)
    /// </summary>
    public static class ThaiShaper
    {
        static readonly HashSet<char> UpperVowel = new HashSet<char>
        {
            'ั', 'ิ', 'ี', 'ึ', 'ื', '็', 'ํ',
        };

        static readonly Dictionary<char, char> RaisedTone = new Dictionary<char, char>
        {
            { '่', '' }, { '้', '' }, { '๊', '' }, { '๋', '' }, { '์', '' },
        };

        static readonly Dictionary<char, char> SmallTone = new Dictionary<char, char>
        {
            { '่', '' }, { '้', '' }, { '๊', '' }, { '๋', '' }, { '์', '' },
        };

        static bool IsTallConsonant(char c)
        {
            return c == 'ป' || c == 'ฝ' || c == 'ฟ'; // ป ฝ ฟ
        }

        public static string Fix(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                char rt, st;
                if (i > 0 && UpperVowel.Contains(s[i - 1]) && RaisedTone.TryGetValue(c, out rt))
                {
                    sb.Append(rt);
                    continue;
                }
                if (i > 0 && IsTallConsonant(s[i - 1]) && SmallTone.TryGetValue(c, out st))
                {
                    sb.Append(st);
                    continue;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
