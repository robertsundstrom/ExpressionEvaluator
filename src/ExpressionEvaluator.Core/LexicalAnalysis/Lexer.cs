﻿using ExpressionEvaluator.Diagnostics;
using ExpressionEvaluator.Properties;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace ExpressionEvaluator.LexicalAnalysis
{
    /// <summary>
    /// Lexer.
    /// </summary>
    public class Lexer
    {
        private LexerState? state;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ExpressionEvaluator.Lexer"/> class.
        /// </summary>
        /// <param name="textReader">Text reader.</param>
        public Lexer(TextReader textReader)
        {
            TextReader = textReader;

            Line = 1;
            Column = 1;

            Diagnostics = new DiagnosticsBag();
        }

        /// <summary>
        /// Gets the diagnostics bag.
        /// </summary>
        /// <value>The diagnostics bag.</value>
        public DiagnosticsBag Diagnostics { get; }

        /// <summary>
        /// Gets the current level of indentation.
        /// </summary>
        /// <value>The indenation.</value>
        public int Indentation { get; private set; }

        /// <summary>
        /// Gets the text reader.
        /// </summary>
        /// <value>The text reader.</value>
        public TextReader TextReader
        {
            get;
        }

        /// <summary>
        /// Peek a token.
        /// </summary>
        /// <returns>The token.</returns>
        public TokenInfo PeekToken()
        {
            if (state != null)
            {
                var s = (LexerState)state;
                var peekedToken = s.Token;
                Line = s.Line;
                Column = s.Column;
                return peekedToken;
            }
            else
            {
                var peekToken = ReadTokenCore();
                state = new LexerState(peekToken, Line, Column);
                return peekToken;
            }
        }

        /// <summary>
        /// Read a token.
        /// </summary>
        /// <returns>The token.</returns>
        public TokenInfo ReadToken()
        {
            if (state != null)
            {
                var token = state.Value.Token;
                state = null;
                return token;
            }
            return ReadTokenCore();
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:ExpressionEvaluator.Lexer"/> has reached EOF.
        /// </summary>
        /// <value><c>true</c> if is EOF; otherwise, <c>false</c>.</value>
        public bool IsEof
        {
            get
            {
                return PeekToken().Kind == TokenKind.EndOfFile;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:ExpressionEvaluator.Lexer"/> has reached EOL.
        /// </summary>
        /// <value><c>true</c> if is EOL; otherwise, <c>false</c>.</value>
        public bool IsEol
        {
            get;
            private set;
        }

        /// <summary>
        /// Internal method for reading a token.
        /// </summary>
        /// <returns>The token core.</returns>
        internal TokenInfo ReadTokenCore()
        {
            while (!IsEofCore)
            {
                int line = Line;
                int column = Column;

                var c = PeekChar();

                if (char.IsLetter(c))
                {
                    var stringBuilder = new StringBuilder();
                    do
                    {
                        ReadChar();

                        stringBuilder.Append(c);

                        c = PeekChar();
                    } while (char.IsLetterOrDigit(c));

                    var str = stringBuilder.ToString();
                    if(Enum.TryParse<TokenKind>(string.Format($"{str.Capitalize()}Keyword"), false, out var result))
                    {
                        return new TokenInfo(result, line, column, str.Length, str);
                    }

                    return new TokenInfo(TokenKind.Identifier, line, column, str.Length, str);
                }
                else if (char.IsDigit(c))
                {
                    var stringBuilder = new StringBuilder();
                    do
                    {
                        ReadChar();

                        stringBuilder.Append(c);

                        c = PeekChar();
                    } while (char.IsDigit(c));

                    return new TokenInfo(TokenKind.Number, line, column, stringBuilder.Length, stringBuilder.ToString());
                }
                else
                {
                    ReadChar();

                    char c2 = ' ';

                    switch (c)
                    {
                        case '+':
                            return new TokenInfo(TokenKind.Plus, line, column, 1, "+");

                        case '-':
                            return new TokenInfo(TokenKind.Minus, line, column, 1, "-");

                        case '*':
                            return new TokenInfo(TokenKind.Star, line, column, 1, "*");

                        case '/':
                            return new TokenInfo(TokenKind.Slash, line, column, 1, "/");

                        case '%':
                            return new TokenInfo(TokenKind.Percent, line, column, 1, "%");

                        case '=':
                            c2 = PeekChar();
                            if (c2 == '=')
                            {
                                ReadChar();
                                return new TokenInfo(TokenKind.Equal, line, column, 1, "==");
                            }
                            return new TokenInfo(TokenKind.Assign, line, column, 1, "=");

                        case '^':
                            return new TokenInfo(TokenKind.Caret, line, column, 1, "^");

                        case ',':
                            return new TokenInfo(TokenKind.Comma, line, column, 1, ",");

                        case '.':
                            return new TokenInfo(TokenKind.Period, line, column, 1, ".");

                        case ';':
                            return new TokenInfo(TokenKind.Semicolon, line, column, 1, ";");

                        case ':':
                            return new TokenInfo(TokenKind.Colon, line, column, 1, ":");

                        case '!':
                            c2 = PeekChar();
                            if (c2 == '=')
                            {
                                ReadChar();
                                return new TokenInfo(TokenKind.NotEquals, line, column, 1);
                            }
                            return new TokenInfo(TokenKind.Negate, line, column, 1);

                        case '&':
                            c2 = ReadChar();
                            if (c2 == '&')
                            {
                                return new TokenInfo(TokenKind.And, line, column, 1, "&&");
                            }
                            goto default;

                        case '|':
                            c2 = ReadChar();
                            if (c2 == '|')
                            {
                                return new TokenInfo(TokenKind.Or, line, column, 1, "||");
                            }
                            goto default;

                        case '<':
                            c2 = PeekChar();
                            if (c2 == '=')
                            {
                                ReadChar();
                                return new TokenInfo(TokenKind.OpenAngleBracket, line, column, 1, "<=");
                            }
                            return new TokenInfo(TokenKind.Less, line, column, 1, "<");

                        case '>':
                            c2 = PeekChar();
                            if (c2 == '=')
                            {
                                ReadChar();
                                return new TokenInfo(TokenKind.GreaterOrEqual, line, column, 1, ">=");
                            }
                            return new TokenInfo(TokenKind.CloseAngleBracket, line, column, 1, ">");

                        case '(':
                            return new TokenInfo(TokenKind.OpenParen, line, column, 1, "(");

                        case ')':
                            return new TokenInfo(TokenKind.CloseParen, line, column, 1, ")");

                        case '\t':
                            ReadIndentation(column);
                            break;

                        case ' ':
                            break;

                        case '\r':
                            break;

                        case '\n':
                            Line++;
                            Column = 1;
                            Indentation = 0;

                            ReadIndentation();
                            break;

                        default:
                            Diagnostics.AddError(string.Format(Strings.Error_InvalidToken, c), new SourceSpan(new SourceLocation(line, column), new SourceLocation(Line, Column)));
                            return new TokenInfo(TokenKind.Invalid, line, column, 1, c.ToString());
                    }
                }
            }

            return new TokenInfo(TokenKind.EndOfFile, Line, Column, 0);
        }

        public void ReadIndentation()
        {
            ReadIndentation(Column);
        }

        private void ReadIndentation(int column)
        {
            if (column == 1)
            {
                char c2 = PeekChar();
                if (c2 == '\t')
                {
                    Indentation += 4;
                }
                else if (c2 == ' ')
                {
                    c2 = PeekChar();

                    while (c2 == ' ')
                    {
                        ReadChar();

                        Indentation++;

                        c2 = PeekChar();
                    }
                }
            }
        }

        private bool IsEofCore
        {
            get
            {
                return TextReader.Peek() == -1;
            }
        }

        private int Line { get; set; }
        private int Column { get; set; }

        private char ReadChar()
        {
            Column++;
            return (char)TextReader.Read();
        }

        private char PeekChar()
        {
            var ch = (char)TextReader.Peek();
            if(ch == '\n' || ch == '\r')
            {
                IsEol = true;
            }
            else
            {
                IsEol = false;
            }
            return ch;
        }

        /// <summary>
        /// Encapsulates a lookahead state for the Lexer..
        /// </summary>
        struct LexerState
        {
            public LexerState(TokenInfo token, int line, int column)
            {
                Token = token;
                Line = line;
                Column = column;
            }

            public TokenInfo Token { get; }

            public int Line { get; }

            public int Column { get; }
        }
    }
}
