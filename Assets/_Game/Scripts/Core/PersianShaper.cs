using System.Collections.Generic;
using System.Text;

namespace MyFutureCity.Core
{
    /// <summary>
    /// Prepares Persian/Arabic-script text for renderers that draw glyphs strictly left-to-right
    /// without shaping (Unity's legacy UI Text / TextMesh): joins letters using Arabic Presentation
    /// Forms, handles lam-alef ligatures, converts digits and reorders RTL runs for display.
    /// Requires a font that contains the presentation-form glyphs (Vazirmatn does).
    /// </summary>
    public static class PersianShaper
    {
        private class Forms
        {
            public char Iso, Fin, Ini, Med;
            public bool Dual;              // can connect to the NEXT letter (left side)
            public bool JoinsPrev = true;  // can connect to the PREVIOUS letter (right side)
        }

        private static readonly Dictionary<char, Forms> Table = BuildTable();

        private static void Add(Dictionary<char, Forms> t, int ch, int iso, int fin, int ini, int med)
        {
            t[(char)ch] = new Forms { Iso = (char)iso, Fin = (char)fin, Ini = (char)ini, Med = (char)med, Dual = ini != 0 };
        }

        private static Dictionary<char, Forms> BuildTable()
        {
            var t = new Dictionary<char, Forms>();
            // Right-joining letters (no initial / medial form)
            Add(t, 0x0627, 0xFE8D, 0xFE8E, 0, 0); // ا
            Add(t, 0x0622, 0xFE81, 0xFE82, 0, 0); // آ
            Add(t, 0x0623, 0xFE83, 0xFE84, 0, 0); // أ
            Add(t, 0x0625, 0xFE87, 0xFE88, 0, 0); // إ
            Add(t, 0x0624, 0xFE85, 0xFE86, 0, 0); // ؤ
            Add(t, 0x0629, 0xFE93, 0xFE94, 0, 0); // ة
            Add(t, 0x062F, 0xFEA9, 0xFEAA, 0, 0); // د
            Add(t, 0x0630, 0xFEAB, 0xFEAC, 0, 0); // ذ
            Add(t, 0x0631, 0xFEAD, 0xFEAE, 0, 0); // ر
            Add(t, 0x0632, 0xFEAF, 0xFEB0, 0, 0); // ز
            Add(t, 0x0698, 0xFB8A, 0xFB8B, 0, 0); // ژ
            Add(t, 0x0648, 0xFEED, 0xFEEE, 0, 0); // و
            // Dual-joining letters: isolated, final, initial, medial
            Add(t, 0x0626, 0xFE89, 0xFE8A, 0xFE8B, 0xFE8C); // ئ
            Add(t, 0x0628, 0xFE8F, 0xFE90, 0xFE91, 0xFE92); // ب
            Add(t, 0x067E, 0xFB56, 0xFB57, 0xFB58, 0xFB59); // پ
            Add(t, 0x062A, 0xFE95, 0xFE96, 0xFE97, 0xFE98); // ت
            Add(t, 0x062B, 0xFE99, 0xFE9A, 0xFE9B, 0xFE9C); // ث
            Add(t, 0x062C, 0xFE9D, 0xFE9E, 0xFE9F, 0xFEA0); // ج
            Add(t, 0x0686, 0xFB7A, 0xFB7B, 0xFB7C, 0xFB7D); // چ
            Add(t, 0x062D, 0xFEA1, 0xFEA2, 0xFEA3, 0xFEA4); // ح
            Add(t, 0x062E, 0xFEA5, 0xFEA6, 0xFEA7, 0xFEA8); // خ
            Add(t, 0x0633, 0xFEB1, 0xFEB2, 0xFEB3, 0xFEB4); // س
            Add(t, 0x0634, 0xFEB5, 0xFEB6, 0xFEB7, 0xFEB8); // ش
            Add(t, 0x0635, 0xFEB9, 0xFEBA, 0xFEBB, 0xFEBC); // ص
            Add(t, 0x0636, 0xFEBD, 0xFEBE, 0xFEBF, 0xFEC0); // ض
            Add(t, 0x0637, 0xFEC1, 0xFEC2, 0xFEC3, 0xFEC4); // ط
            Add(t, 0x0638, 0xFEC5, 0xFEC6, 0xFEC7, 0xFEC8); // ظ
            Add(t, 0x0639, 0xFEC9, 0xFECA, 0xFECB, 0xFECC); // ع
            Add(t, 0x063A, 0xFECD, 0xFECE, 0xFECF, 0xFED0); // غ
            Add(t, 0x0641, 0xFED1, 0xFED2, 0xFED3, 0xFED4); // ف
            Add(t, 0x0642, 0xFED5, 0xFED6, 0xFED7, 0xFED8); // ق
            Add(t, 0x06A9, 0xFB8E, 0xFB8F, 0xFB90, 0xFB91); // ک (Persian kaf)
            Add(t, 0x06AF, 0xFB92, 0xFB93, 0xFB94, 0xFB95); // گ
            Add(t, 0x0644, 0xFEDD, 0xFEDE, 0xFEDF, 0xFEE0); // ل
            Add(t, 0x0645, 0xFEE1, 0xFEE2, 0xFEE3, 0xFEE4); // م
            Add(t, 0x0646, 0xFEE5, 0xFEE6, 0xFEE7, 0xFEE8); // ن
            Add(t, 0x0647, 0xFEE9, 0xFEEA, 0xFEEB, 0xFEEC); // ه
            Add(t, 0x06CC, 0xFBFC, 0xFBFD, 0xFBFE, 0xFBFF); // ی (Persian yeh)
            Add(t, 0x0640, 0x0640, 0x0640, 0x0640, 0x0640); // ـ tatweel
            // Hamza never connects
            t['\u0621'] = new Forms { Iso = '\uFE80', Fin = '\uFE80', Ini = '\uFE80', Med = '\uFE80', Dual = false, JoinsPrev = false };
            return t;
        }

        private static char LamAlef(char alef, bool connectedToPrev)
        {
            switch (alef)
            {
                case '\u0627': return connectedToPrev ? '\uFEFC' : '\uFEFB';
                case '\u0622': return connectedToPrev ? '\uFEF6' : '\uFEF5';
                case '\u0623': return connectedToPrev ? '\uFEF8' : '\uFEF7';
                case '\u0625': return connectedToPrev ? '\uFEFA' : '\uFEF9';
                default: return '\0';
            }
        }

        private static bool CanJoinNext(char c)
        {
            Forms f;
            return Table.TryGetValue(c, out f) && f.Dual;
        }

        private static bool CanJoinPrev(char c)
        {
            Forms f;
            return Table.TryGetValue(c, out f) && f.JoinsPrev;
        }

        private static bool IsTransparent(char c)
        {
            return (c >= '\u064B' && c <= '\u065F') || c == '\u0670' || c == '\u200D';
        }

        private static char Normalize(char c, bool persianDigits)
        {
            switch (c)
            {
                case '\u0643': return '\u06A9'; // ك -> ک
                case '\u064A':                  // ي -> ی
                case '\u0649': return '\u06CC'; // ى -> ی
            }
            if (c >= '\u0660' && c <= '\u0669') return (char)(c - 0x0660 + 0x06F0);
            if (persianDigits && c >= '0' && c <= '9') return (char)(c - '0' + 0x06F0);
            return c;
        }

        private enum Dir { R, L, N }

        private static Dir Classify(char c)
        {
            if (c >= '\u06F0' && c <= '\u06F9') return Dir.L;
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) return Dir.L;
            if (c >= '\u0600' && c <= '\u06FF')
            {
                if (c == '\u060C' || c == '\u061B' || c == '\u061F') return Dir.N;
                return Dir.R;
            }
            if (c >= '\uFB50' && c <= '\uFDFF') return Dir.R;
            if (c >= '\uFE70' && c <= '\uFEFF') return Dir.R;
            if (c > '\u007F' && char.IsLetter(c)) return Dir.L;
            return Dir.N;
        }

        private static char Mirror(char c)
        {
            switch (c)
            {
                case '(': return ')';
                case ')': return '(';
                case '[': return ']';
                case ']': return '[';
                case '{': return '}';
                case '}': return '{';
                case '<': return '>';
                case '>': return '<';
                case '\u00AB': return '\u00BB';
                case '\u00BB': return '\u00AB';
                default: return c;
            }
        }

        /// <summary>Join letters + reorder one logical line into left-to-right display order.</summary>
        public static string Shape(string logical, bool persianDigits)
        {
            if (string.IsNullOrEmpty(logical)) return "";
            var lines = logical.Replace("\r\n", "\n").Split('\n');
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(ShapeLine(lines[i], persianDigits));
            }
            return sb.ToString();
        }

        public static string Shape(string logical)
        {
            return Shape(logical, true);
        }

        private static string ShapeLine(string line, bool persianDigits)
        {
            var src = new List<char>(line.Length);
            foreach (char ch in line)
            {
                if (IsTransparent(ch)) continue;
                src.Add(Normalize(ch, persianDigits));
            }

            int n = src.Count;
            var shaped = new List<char>(n);
            for (int i = 0; i < n; i++)
            {
                char c = src[i];
                Forms f;
                if (!Table.TryGetValue(c, out f)) { shaped.Add(c); continue; }

                bool prevJoin = i > 0 && CanJoinNext(src[i - 1]);

                if (c == '\u0644' && i + 1 < n)
                {
                    char lig = LamAlef(src[i + 1], prevJoin);
                    if (lig != '\0') { shaped.Add(lig); i++; continue; }
                }

                bool nextJoin = i + 1 < n && CanJoinPrev(src[i + 1]);
                bool connPrev = prevJoin && f.JoinsPrev;
                bool connNext = f.Dual && nextJoin;

                char o;
                if (connPrev && connNext) o = f.Med;
                else if (connPrev) o = f.Fin;
                else if (connNext) o = f.Ini;
                else o = f.Iso;
                shaped.Add(o);
            }

            shaped.RemoveAll(c => c == '\u200C'); // ZWNJ only breaks joining; it has no glyph
            return Reorder(shaped);
        }

        private static string Reorder(List<char> chars)
        {
            int n = chars.Count;
            if (n == 0) return "";

            var dirs = new Dir[n];
            for (int i = 0; i < n; i++) dirs[i] = Classify(chars[i]);

            // Neutrals take the LTR direction only when sandwiched between LTR characters.
            // Resolved against the ORIGINAL classification so earlier neutrals don't act as strong.
            var orig = (Dir[])dirs.Clone();
            for (int i = 0; i < n; i++)
            {
                if (orig[i] != Dir.N) continue;
                int p = i - 1; while (p >= 0 && orig[p] == Dir.N) p--;
                int q = i + 1; while (q < n && orig[q] == Dir.N) q++;
                bool ltr = p >= 0 && q < n && orig[p] == Dir.L && orig[q] == Dir.L;
                dirs[i] = ltr ? Dir.L : Dir.R;
            }

            var runs = new List<int[]>(); // start, end(exclusive), isRtl(1/0)
            int s = 0;
            while (s < n)
            {
                int e = s + 1;
                while (e < n && dirs[e] == dirs[s]) e++;
                runs.Add(new[] { s, e, dirs[s] == Dir.R ? 1 : 0 });
                s = e;
            }

            var sb = new StringBuilder(n);
            for (int r = runs.Count - 1; r >= 0; r--)
            {
                int start = runs[r][0], end = runs[r][1];
                if (runs[r][2] == 1)
                    for (int k = end - 1; k >= start; k--) sb.Append(Mirror(chars[k]));
                else
                    for (int k = start; k < end; k++) sb.Append(chars[k]);
            }
            return sb.ToString();
        }

        /// <summary>Greedy word wrap on LOGICAL text (before shaping). maxChars &lt;= 0 disables wrapping.</summary>
        public static string Wrap(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (maxChars <= 0) return text;
            var outLines = new List<string>();
            foreach (var para in text.Replace("\r\n", "\n").Split('\n'))
            {
                var cur = new StringBuilder();
                foreach (var w in para.Split(' '))
                {
                    if (cur.Length == 0) cur.Append(w);
                    else if (cur.Length + 1 + w.Length <= maxChars) cur.Append(' ').Append(w);
                    else { outLines.Add(cur.ToString()); cur.Length = 0; cur.Append(w); }
                }
                outLines.Add(cur.ToString());
            }
            return string.Join("\n", outLines.ToArray());
        }

        /// <summary>Wrap (optional) then shape. This is what UI code should call.</summary>
        public static string Fix(string logical, int maxCharsPerLine)
        {
            return Shape(Wrap(logical, maxCharsPerLine));
        }

        public static string Fix(string logical)
        {
            return Fix(logical, 0);
        }
    }
}
