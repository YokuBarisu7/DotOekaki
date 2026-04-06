using System.Text;

public static class ShiritoriKana
{
    // しりとり判定に使う：単語の先頭（しりとり文字）
    public static char GetHead(string word)
    {
        var s = NormalizeWord(word);
        if (string.IsNullOrEmpty(s)) return '\0';

        // 先頭から「ー」など無視文字を飛ばす
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (IsIgnored(c)) continue;
            return CanonicalKana(c);
        }
        return '\0';
    }

    // しりとり判定に使う：単語の末尾（しりとり文字）
    public static char GetTail(string word)
    {
        var s = NormalizeWord(word);
        if (string.IsNullOrEmpty(s)) return '\0';

        bool skippedTerminalN = false;

        // 末尾から有効文字を探す
        for (int i = s.Length - 1; i >= 0; i--)
        {
            char c = s[i];
            if (IsIgnored(c)) continue;

            char canon = CanonicalKana(c);
            if (canon == '\0') continue;

            // 一番最後の有効文字が「ん」なら、それは飛ばして一個前を見る
            if (!skippedTerminalN && canon == 'ん')
            {
                skippedTerminalN = true;
                continue;
            }

            return canon;
        }

        return '\0';
    }

    // 単語そのものの正規化：
    // - Unicode正規化で濁点を結合（は゛ → ば 等）
    // - 半角カナ→全角、互換文字の寄せ（NFKC）
    // - カタカナ→ひらがな
    // - 余計な空白を除去
    public static string NormalizeWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return "";

        // 互換正規化（半角ｶﾅ等の寄せ）
        string s = word.Trim().Normalize(NormalizationForm.FormKC);

        // 濁点など結合（分解形が混ざっても寄せる）
        s = s.Normalize(NormalizationForm.FormC);

        var sb = new StringBuilder(s.Length);
        foreach (char raw in s)
        {
            char c = raw;

            // 全角スペース/空白/改行などは無視
            if (char.IsWhiteSpace(c)) continue;

            // カタカナ→ひらがな
            c = ToHiragana(c);

            // ひらがな・長音・記号は一旦通す（後で末尾/先頭で処理）
            // それ以外（英数や絵文字等）はここで落とすなら落とす
            // 今回は「ひらがな＋ー＋゛゜」以外は捨てる方針（ガチ寄り）
            if (IsHiragana(c) || c == 'ー')
                sb.Append(c);
        }

        return sb.ToString();
    }

    // しりとり用に1文字を正規化（小書き→大、っ→つ、ゎ→わ 等）
    public static char CanonicalKana(char c)
    {
        // 長音は“文字”としては判定に使わない（GetHead/Tail側で飛ばす）
        if (c == 'ー') return '\0';

        // 小書き→大書き（ぁ→あ、ゃ→や、ゎ→わ、ゕ/ゖなども）
        c = ToLargeKana(c);

        // 促音：っ は つ 扱い（一般的にこのルールが多い）
        if (c == 'っ') return 'つ';

        // ヴ系：ゔ は “う” 扱いに寄せる（好み）
        if (c == 'ゔ') return 'う';

        return c;
    }

    // 「ー」など無視する文字（末尾に付く想定）
    private static bool IsIgnored(char c) => c == 'ー';

    private static bool IsHiragana(char c) => (c >= 'ぁ' && c <= 'ゖ') || c == 'ゔ';

    // カタカナ→ひらがな（Unicodeの差分）
    private static char ToHiragana(char c)
    {
        // ァ(30A1)〜ヶ(30F6) を ぁ(3041)〜ゖ(3096) に寄せる（差は0x60）
        if (c >= 'ァ' && c <= 'ヶ')
            return (char)(c - 0x60);

        // ヴ(30F4) → ゔ(3094)
        if (c == 'ヴ') return 'ゔ';

        return c;
    }

    private static char ToLargeKana(char c)
    {
        return c switch
        {
            'ぁ' => 'あ',
            'ぃ' => 'い',
            'ぅ' => 'う',
            'ぇ' => 'え',
            'ぉ' => 'お',
            'ゃ' => 'や',
            'ゅ' => 'ゆ',
            'ょ' => 'よ',
            'ゎ' => 'わ',
            'ゕ' => 'か',
            'ゖ' => 'け',
            _ => c
        };
    }
}