namespace Sentry.Internal.DataCollection;

internal static class SqlSanitizer
{
    [return: NotNullIfNotNull(nameof(sql))]
    internal static string? Sanitize(string? sql, string? dbSystem = null) =>
        sql is null ? null : new Scanner(sql, DialectFor(dbSystem)).Run();

    private static SqlDialect DialectFor(string? dbSystem) => dbSystem switch
    {
        "mssql" or "mssqlcompact" or "sqlite" => SqlDialect.SqlServer,
        "mysql" or "spanner" => SqlDialect.MySql,
        _ => SqlDialect.Generic
    };

    private enum SqlDialect
    {
        Generic,  // Postgres compatible
        SqlServer,
        MySql
    }

    private sealed class Scanner(string sql, SqlDialect dialect)
    {
        private const char Placeholder = '?';

        private readonly StringBuilder _output = new(sql.Length);
        private int _position;

        internal string Run()
        {
            CopyLeadingLineComments();
            while (_position < sql.Length)
            {
                ScanToken();
            }

            return _output.ToString().Trim();
        }

        private char Current => sql[_position];

        private char Peek(int offset = 1) => _position + offset < sql.Length ? sql[_position + offset] : '\0';

        // EF Core writes TagWith() and TagWithCallSite() as "-- " lines ahead of the statement
        private void CopyLeadingLineComments()
        {
            var commentEnd = 0;
            while (_position < sql.Length)
            {
                if (char.IsWhiteSpace(Current))
                {
                    _position++;
                }
                else if (Current == '-' && Peek() == '-')
                {
                    SkipLineComment();
                    commentEnd = _position;
                }
                else
                {
                    break;
                }
            }

            _output.Append(sql, 0, commentEnd);
            _position = commentEnd;
        }

        private void ScanToken()
        {
            var current = Current;
            var next = Peek();
            switch (current)
            {
                case '-' when next == '-':
                case '#' when dialect == SqlDialect.MySql:
                    SkipLineComment();
                    return;
                case '/' when next == '*':
                    SkipBlockComment();
                    return;
                case '\'':
                    SkipString('\'', backslashEscapes: dialect == SqlDialect.MySql);
                    return;
                case 'N' or 'n' or 'X' or 'x' or 'B' or 'b' when next == '\'':
                    _position++;
                    return;
                case 'E' or 'e' when next == '\'' && dialect == SqlDialect.Generic:
                    _position++;
                    SkipString('\'', backslashEscapes: true);
                    return;
                case '"' when dialect == SqlDialect.MySql:
                    SkipString('"', backslashEscapes: true);
                    return;
                case '"':
                case '`':
                    CopyQuotedIdentifier(current);
                    return;
                case '[' when dialect == SqlDialect.SqlServer:
                    CopyQuotedIdentifier(']');
                    return;
                case '$':
                    ScanDollar();
                    return;
                case '.' when IsDigit(next) && !FollowsWord():
                    SkipNumber();
                    return;
                case '@' or '#' when dialect == SqlDialect.Generic && IsDigit(next):
                    _output.Append(current);
                    _position++;
                    return;
            }

            if (IsDigit(current))
            {
                SkipNumber();
            }
            else if (IsWordStart(current))
            {
                CopyWord();
            }
            else
            {
                _output.Append(current);
                _position++;
            }
        }

        private void SkipLineComment()
        {
            while (_position < sql.Length && Current is not ('\n' or '\r'))
            {
                _position++;
            }
        }

        private void SkipBlockComment()
        {
            _position += 2;
            var depth = 1;
            while (_position < sql.Length && depth > 0)
            {
                if (Current == '/' && Peek() == '*')
                {
                    depth++;
                    _position += 2;
                }
                else if (Current == '*' && Peek() == '/')
                {
                    depth--;
                    _position += 2;
                }
                else
                {
                    _position++;
                }
            }

            if (_output.Length > 0 && !char.IsWhiteSpace(_output[_output.Length - 1]) &&
                _position < sql.Length && !char.IsWhiteSpace(Current))
            {
                _output.Append(' ');
            }
        }

        private void SkipString(char quote, bool backslashEscapes)
        {
            _position++;
            while (_position < sql.Length)
            {
                var current = Current;
                if (backslashEscapes && current == '\\')
                {
                    _position = Math.Min(_position + 2, sql.Length);
                    continue;
                }

                _position++;
                if (current == quote)
                {
                    if (_position < sql.Length && Current == quote)
                    {
                        _position++;
                        continue;
                    }

                    break;
                }
            }

            _output.Append(Placeholder);
        }

        private void CopyQuotedIdentifier(char closingQuote)
        {
            var start = _position;
            _position++;
            while (_position < sql.Length)
            {
                var current = Current;
                _position++;
                if (current == closingQuote)
                {
                    if (_position < sql.Length && Current == closingQuote)
                    {
                        _position++;
                        continue;
                    }

                    _output.Append(sql, start, _position - start);
                    return;
                }
            }

            _output.Append(Placeholder);
        }

        private void ScanDollar()
        {
            if (IsDigit(Peek()))
            {
                if (dialect == SqlDialect.SqlServer)
                {
                    _position++;
                    SkipNumber();
                }
                else
                {
                    CopyWord();
                }

                return;
            }

            var tagEnd = _position + 1;
            while (tagEnd < sql.Length && (char.IsLetterOrDigit(sql[tagEnd]) || sql[tagEnd] == '_'))
            {
                tagEnd++;
            }

            if (tagEnd < sql.Length && sql[tagEnd] == '$')
            {
                var delimiter = sql.Substring(_position, tagEnd + 1 - _position);
                var closingDelimiter = sql.IndexOf(delimiter, tagEnd + 1, StringComparison.Ordinal);
                _position = closingDelimiter < 0 ? sql.Length : closingDelimiter + delimiter.Length;
                _output.Append(Placeholder);
                return;
            }

            CopyWord();
        }

        private void SkipNumber()
        {
            if (Current == '0' && Peek() is 'x' or 'X' or 'b' or 'B' or 'o' or 'O' && IsHexDigit(Peek(2)))
            {
                _position += 2;
                while (_position < sql.Length && (IsHexDigit(Current) || Current == '_'))
                {
                    _position++;
                }
            }
            else
            {
                SkipDigits();
                if (_position < sql.Length && Current == '.' && IsDigit(Peek()))
                {
                    _position++;
                    SkipDigits();
                }

                if (_position < sql.Length && Current is 'e' or 'E')
                {
                    var exponentStart = Peek() is '+' or '-' ? 2 : 1;
                    if (IsDigit(Peek(exponentStart)))
                    {
                        _position += exponentStart;
                        SkipDigits();
                    }
                }
            }

            _output.Append(Placeholder);
        }

        private void SkipDigits()
        {
            while (_position < sql.Length && (IsDigit(Current) || Current == '_'))
            {
                _position++;
            }
        }

        private void CopyWord()
        {
            var start = _position;
            _position++;
            while (_position < sql.Length && IsWordPart(Current))
            {
                _position++;
            }

            _output.Append(sql, start, _position - start);
        }

        private bool FollowsWord() => _position > 0 && IsWordPart(sql[_position - 1]);

        private static bool IsWordStart(char c) => char.IsLetter(c) || c is '_' or '@' or '#';

        private bool IsWordPart(char c) => c switch
        {
            '_' or '$' => true,
            '@' => dialect != SqlDialect.Generic,
            '#' => dialect == SqlDialect.SqlServer,
            _ => char.IsLetterOrDigit(c)
        };

        private static bool IsDigit(char c) => c is >= '0' and <= '9';

        private static bool IsHexDigit(char c) => IsDigit(c) || c is >= 'a' and <= 'f' or >= 'A' and <= 'F';
    }
}
