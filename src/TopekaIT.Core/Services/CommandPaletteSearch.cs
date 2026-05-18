using System.Text.RegularExpressions;

namespace TopekaIT.Core.Services;

public static partial class CommandPaletteSearch
{
    public static int Score(string? query, params string?[] fields)
    {
        var queryTokens = Tokenize(query).ToArray();
        if (queryTokens.Length == 0)
        {
            return 0;
        }

        var fieldTokens = fields
            .SelectMany((field, index) => Tokenize(field).Select(token => new SearchToken(token, FieldWeight(index))))
            .ToArray();

        if (fieldTokens.Length == 0)
        {
            return 0;
        }

        var score = 0;
        foreach (var queryToken in queryTokens)
        {
            var tokenScore = fieldTokens.Max(candidate => CandidateScore(queryToken, candidate));
            if (tokenScore <= 0)
            {
                return 0;
            }

            score += tokenScore;
        }

        return score + (queryTokens.Length * 5);
    }

    private static int CandidateScore(string queryToken, SearchToken candidate)
    {
        var matchScore = MatchScore(queryToken, candidate.Value);
        return matchScore <= 0 ? 0 : matchScore + candidate.Weight;
    }

    private static int FieldWeight(int index) => index switch
    {
        0 => 18,
        1 => 14,
        2 => 6,
        3 => 10,
        _ => 0,
    };

    private static int MatchScore(string queryToken, string candidate)
    {
        if (candidate.Equals(queryToken, StringComparison.Ordinal))
        {
            return 100;
        }

        if (Singular(candidate).Equals(Singular(queryToken), StringComparison.Ordinal))
        {
            return 92;
        }

        if (candidate.StartsWith(queryToken, StringComparison.Ordinal))
        {
            return 78;
        }

        if (queryToken.StartsWith(candidate, StringComparison.Ordinal) && candidate.Length >= 2)
        {
            return 70;
        }

        if (queryToken.Length >= 2 && candidate.Contains(queryToken, StringComparison.Ordinal))
        {
            return 45;
        }

        if (candidate.Length >= 3 && queryToken.Contains(candidate, StringComparison.Ordinal))
        {
            return 36;
        }

        return 0;
    }

    private static string Singular(string token)
    {
        if (token.Length > 3 && token.EndsWith("ies", StringComparison.Ordinal))
        {
            return token[..^3] + "y";
        }

        if (token.Length > 3 && token.EndsWith("es", StringComparison.Ordinal))
        {
            return token[..^2];
        }

        if (token.Length > 2 && token.EndsWith('s'))
        {
            return token[..^1];
        }

        return token;
    }

    private static IEnumerable<string> Tokenize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        foreach (Match match in SearchTokenRegex().Matches(value.ToLowerInvariant()))
        {
            yield return match.Value;
        }
    }

    [GeneratedRegex("[a-z0-9]+")]
    private static partial Regex SearchTokenRegex();

    private sealed record SearchToken(string Value, int Weight);
}
