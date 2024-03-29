﻿using LuminaxLanguage.Constants;
using LuminaxLanguage.Dto;

namespace LuminaxLanguage.Processors
{
    public class SyntaxAnalyzer
    {
        // ReSharper disable once InconsistentNaming
        private readonly List<SymbolInformation>? SymbolsInformation;
        // ReSharper disable once InconsistentNaming
        private AnalysisInformation AnalysisInformation;
        public List<TokenInformation> PostfixCode = new();
        private readonly BracketsProcessor _bracketsProcessor;
        private int _iterator;

        private int Iterator
        {
            get
            {
                if (++_iterator > SymbolsInformation!.Count - 1)
                    throw new Exception("Parser: ");
                return _iterator;
            }
            set => _iterator = value;
        }

        public SyntaxAnalyzer(AnalysisInformation analysisInformation, BracketsProcessor bracketsProcessor)
        {
            AnalysisInformation = analysisInformation;
            SymbolsInformation = analysisInformation.SymbolsInformation.Values.ToList();
            _bracketsProcessor = bracketsProcessor;
            _iterator = -1;
        }

        public List<string> ParseProgram()
        {
            var resultLists = new List<string>();

            if (SymbolsInformation is not null)
            {
                if (ProcessProgramStartSection())
                {
                    _bracketsProcessor.ControlBracketsFlow(SymbolsInformation[Iterator], "{");

                    ProcessDeclarationSection();

                    ProcessDoSection();

                    _bracketsProcessor.ControlBracketsFlow(SymbolsInformation[Iterator], "}");

                    _bracketsProcessor.CheckStackStatus();
                }
                else
                {
                    throw new Exception("Parser: unexpected error");
                }
            }

            return resultLists;
        }

        #region Token Parsers

        private static bool ParseToken((string, string) expectedToken, SymbolInformation currentInformation)
        {
            if (expectedToken == (currentInformation.Lexeme, currentInformation.LexemeToken))
            {
                Console.WriteLine(ParserMessages.Information,
                    currentInformation.LineNumber,
                    currentInformation.Lexeme,
                    currentInformation.LexemeToken);
            }
            else
            {
                throw new Exception(string.Format(ParserMessages.ErrorWithUnexpectedElements,
                    currentInformation.LineNumber,
                    currentInformation.Lexeme,
                    currentInformation.LexemeToken,
                    expectedToken.Item1,
                    expectedToken.Item2));
            }

            return true;
        }

        private static bool ParseToken(string expectedToken, SymbolInformation currentInformation)
        {
            if (expectedToken == currentInformation.LexemeToken)
            {
                Console.WriteLine(ParserMessages.Information,
                    currentInformation.LineNumber,
                    currentInformation.Lexeme,
                    currentInformation.LexemeToken);
            }
            else
            {
                throw new Exception(string.Format(ParserMessages.ErrorWithUnexpectedToken,
                    currentInformation.LineNumber,
                    currentInformation.LexemeToken,
                    expectedToken));
            }

            return true;
        }

        private bool ParseIdentToken(SymbolInformation symbolInformation)
        {
            if (symbolInformation.LexemeToken == "ident" && ParseToken("ident", symbolInformation))
            {
                PostfixCode.Add(new TokenInformation(symbolInformation.Lexeme, symbolInformation.LexemeToken));
            }

            return true;
        }

        private static bool ParseTypeToken(SymbolInformation symbolInformation) =>
            new[] { "int", "float", "boolean" }.Contains(symbolInformation.Lexeme) &&
            ParseToken("keyword", symbolInformation);

        #endregion

        #region Main Parts Processors

        private bool ProcessProgramStartSection()
        {
            var result = false;

            var symbolInformation = SymbolsInformation![Iterator];

            if (ParseToken(("program", "keyword"), symbolInformation))
            {
                symbolInformation = SymbolsInformation![Iterator];

                if (ParseIdentToken(symbolInformation))
                {
                    result = true;
                }
            }

            return result;
        }

        private void ProcessDeclarationSection()
        {
            _bracketsProcessor.ControlBracketsFlow(SymbolsInformation![Iterator], "{");

            var symbolInformation = ProcessIdentList();

            _bracketsProcessor.ControlBracketsFlow(symbolInformation, "}");
        }

        private SymbolInformation ProcessIdentList()
        {
            SymbolInformation symbolInformation;
            var uniquenessList = new List<string>();

            while ((symbolInformation = SymbolsInformation![Iterator]).Lexeme != "}")
            {
                if (ParseTypeToken(symbolInformation))
                {
                    var type = TypeConverter.ConvertType(symbolInformation.Lexeme);

                    symbolInformation = SymbolsInformation[Iterator];

                    symbolInformation = ParseIdentList(symbolInformation, ";", uniquenessList, type);

                    ParseToken((";", "punct"), symbolInformation);
                }
            }

            return symbolInformation;
        }

        private SymbolInformation ParseIdentList(SymbolInformation symbolInformation, string endOfIdent, List<string>? uniquenessList, Type type)
        {
            HandleVariableUniqueness(symbolInformation, uniquenessList);

            if (ParseIdentToken(symbolInformation))
            {
                HandleVariableType(symbolInformation, type);

                symbolInformation = SymbolsInformation![Iterator];

                while (symbolInformation.Lexeme != endOfIdent)
                {
                    if (ParseToken((",", "punct"), symbolInformation))
                    {
                        symbolInformation = SymbolsInformation![Iterator];

                        HandleVariableUniqueness(symbolInformation, uniquenessList);

                        HandleVariableType(symbolInformation, type);

                        ParseIdentToken(symbolInformation);
                    }

                    symbolInformation = SymbolsInformation[Iterator];
                }
            }
            else
            {
                throw new Exception("Parser: expected list of identifiers");
            }

            return symbolInformation;
        }

        private SymbolInformation ParseIdentList(SymbolInformation symbolInformation, string endOfIdent)
        {
            if (ParseIdentToken(symbolInformation))
            {
                symbolInformation = SymbolsInformation![Iterator];

                while (symbolInformation.Lexeme != endOfIdent)
                {
                    if (ParseToken((",", "punct"), symbolInformation))
                    {
                        symbolInformation = SymbolsInformation![Iterator];

                        ParseIdentToken(symbolInformation);
                    }

                    symbolInformation = SymbolsInformation[Iterator];
                }
            }
            else
            {
                throw new Exception("Parser: expected list of identifiers");
            }

            return symbolInformation;
        }

        private void HandleVariableType(SymbolInformation symbolInformation, Type type)
        {
            var value = AnalysisInformation.Ids[symbolInformation.Lexeme];

            AnalysisInformation.Ids[symbolInformation.Lexeme] =
                value with { Type = type };
        }

        private static void HandleVariableUniqueness(SymbolInformation symbolInformation, List<string>? uniquenessList)
        {
            if (uniquenessList != null && uniquenessList.Contains(symbolInformation.Lexeme))
            {
                throw new Exception(
                    $"The variable {symbolInformation.Lexeme} was already declared in this scope");
            }

            uniquenessList?.Add(symbolInformation.Lexeme);
        }

        private void ProcessDoSection()
        {
            _bracketsProcessor.ControlBracketsFlow(SymbolsInformation![Iterator], "{");

            while (ProcessStatementList()) { }

            _bracketsProcessor.ControlBracketsFlow(SymbolsInformation![_iterator], "}");
        }

        private bool ProcessStatementList()
        {
            var symbolInformation = SymbolsInformation![Iterator];

            var result = symbolInformation switch
            {
                { LexemeToken: "ident" } => ProcessIdentExpression(symbolInformation),
                { LexemeToken: "keyword", Lexeme: "input" } => ProcessInputExpression(symbolInformation),
                { LexemeToken: "keyword", Lexeme: "print" } => ProcessPrintExpression(symbolInformation),
                { LexemeToken: "keyword", Lexeme: "if" } => ProcessIfExpression(symbolInformation),
                { LexemeToken: "keyword", Lexeme: "do" } => ProcessDoWhileExpression(symbolInformation),
                { LexemeToken: "par_op", Lexeme: "}" } => false,
                _ => throw new Exception($"Parser: unexpected token {symbolInformation.Lexeme}")
            };

            return result;
        }

        private bool ProcessIdentExpression(SymbolInformation symbolInformation)
        {
            var result = false;

            if (ParseIdentToken(symbolInformation))
            {
                symbolInformation = SymbolsInformation![Iterator];

                if (ParseToken(("=", "assign_op"), symbolInformation))
                {
                    result = ParseExpression();

                    if (result)
                    {
                        result = ParseToken((";", "punct"), SymbolsInformation![_iterator]);
                    }

                    PostfixCode.Add(new TokenInformation("=", "assign_op"));
                }
                else
                {
                    throw new Exception(string.Format(ParserMessages.ErrorExpectedAssignToken,
                        symbolInformation.LineNumber));
                }
            }

            return result;
        }

        private bool ParseExpression()
        {
            var temp = _iterator;

            if (ParseBooleanExpression())
            {
                return true;
            }

            Iterator = temp;

            if (ParseArithmeticExpression(SymbolsInformation![Iterator]))
            {
                return true;
            }

            throw new Exception("Other operations aren't allowed there");
        }

        private bool ParseBooleanExpression()
        {
            var symbol = SymbolsInformation![Iterator];

            if (symbol.Lexeme is "true" or "false" && ParseToken("boolval", symbol))
            {
                PostfixCode.Add(new TokenInformation(symbol.Lexeme, symbol.LexemeToken));
                return true;
            }

            var tempPostfixCode = new List<TokenInformation>(PostfixCode);

            if (ParseArithmeticExpression(symbol))
            {
                symbol = SymbolsInformation[_iterator];
                var flag = false;

                while (ParseRelExpression(symbol) && ParseExpression())
                {
                    flag = true;
                    
                    PostfixCode.Add(new TokenInformation(symbol.Lexeme, symbol.LexemeToken));

                    symbol = SymbolsInformation[_iterator];
                }

                if (flag)
                {
                    return true;
                }

                PostfixCode = tempPostfixCode;
                return false;
            }

            return false;
        }

        private bool ParseRelExpression(SymbolInformation symbolInformation)
        {
            if (symbolInformation.LexemeToken is "rel_op" && ParseToken("rel_op", symbolInformation))
            {
                return true;
            }

            return false;
        }

        private bool ParseArithmeticExpression(SymbolInformation symbolInformation)
        {
            if (symbolInformation.Lexeme is "+" or "-")
                ParseToken("add_op", symbolInformation);

            if (ParseTerm(symbolInformation))
            {
                var symbol = SymbolsInformation![_iterator];
                var count = 0;

                while (symbol.Lexeme is "+" or "-" && ParseToken("add_op", symbol))
                {
                    var addOperator = symbol.Lexeme;

                    symbol = SymbolsInformation[Iterator];

                    if (!ParseTerm(symbol))
                        return false;

                    if (++count >= 1)
                    {
                        PostfixCode.Add(new TokenInformation(addOperator, "add_op"));
                    }

                    symbol = SymbolsInformation[_iterator];
                }

                return true;
            }

            return false;
        }

        private bool ParseTerm(SymbolInformation symbolInformation, bool addToPolis = true)
        {
            if (ParseChunk(symbolInformation))
            {
                symbolInformation = SymbolsInformation![_iterator];
                var count = 0;

                while (symbolInformation.Lexeme is "*" or "/" && ParseToken("mult_op", symbolInformation))
                {
                    var multperator = symbolInformation.Lexeme;

                    symbolInformation = SymbolsInformation[Iterator];

                    if (!ParseChunk(symbolInformation))
                        return false;

                    if (++count >= 1)
                    {
                        PostfixCode.Add(new TokenInformation(multperator, "mult_op"));
                    }

                    symbolInformation = SymbolsInformation![_iterator];
                }

                return true;
            }

            return false;
        }

        private bool ParseChunk(SymbolInformation symbolInformation, bool addToPolis = true)
        {
            if (ParseFactor(symbolInformation))
            {
                var currentInformation = SymbolsInformation![_iterator];
                var count = 0;

                while (currentInformation.Lexeme == "^" && ParseToken(("^", "pow_op"), currentInformation))
                {
                    symbolInformation = SymbolsInformation![Iterator];

                    if (!ParseFactor(symbolInformation))
                        return false;

                    currentInformation = SymbolsInformation![_iterator];

                    count++;
                }

                while (count + 1 != 1)
                {
                    PostfixCode.Add(new TokenInformation("^", "pow_op"));

                    count--;
                }

                return true;
            }

            return false;
        }

        private bool ParseFactor(SymbolInformation symbol, bool addToPolis = true)
        {
            if (ParseConst(symbol, addToPolis) || ParseIdentToken(symbol))
            {
                _iterator++;
                return true;
            }

            if (symbol.Lexeme is "(" && _bracketsProcessor.ControlBracketsFlow(symbol, "("))
            {
                if (ParseArithmeticExpression(SymbolsInformation![Iterator]))
                {
                    return _bracketsProcessor.ControlBracketsFlow(SymbolsInformation![Iterator], ")");
                }
            }

            return false;
        }

        private bool ParseConst(SymbolInformation symbol, bool addToPolis)
        {
            if (symbol.LexemeToken is "boolval" && ParseToken("boolval", symbol))
            {
                if (addToPolis)
                {
                    PostfixCode.Add(new TokenInformation(symbol.Lexeme, symbol.LexemeToken));

                    return true;
                }
            }

            return (symbol.LexemeToken is "int" && ParseNumber(symbol, "int")) ||
                    (symbol.LexemeToken is "exp" && ParseNumber(symbol, "exp")) ||
                    (symbol.LexemeToken is "float" && ParseNumber(symbol, "float"));
        }

        private bool ParseNumber(SymbolInformation symbol, string typeOfNumber)
        {
            if (ParseToken(typeOfNumber, symbol))
            {
                PostfixCode.Add(new TokenInformation(symbol.Lexeme, symbol.LexemeToken));

                return true;
            }

            return false;
        }

        private bool ProcessDoWhileExpression(SymbolInformation symbolInformation)
        {
            if (ParseToken(("do", "keyword"), symbolInformation))
            {
                PostfixCode.Add(new TokenInformation("WHILE", "keyword"));

                if (_bracketsProcessor.ControlBracketsFlow(SymbolsInformation![Iterator], "{"))
                {
                    while (ProcessStatementList()) { }

                    if (_bracketsProcessor.ControlBracketsFlow(SymbolsInformation![_iterator], "}"))
                    {
                        if (ParseToken(("while", "keyword"), SymbolsInformation![Iterator]))
                        {
                            if (_bracketsProcessor.ControlBracketsFlow(SymbolsInformation[Iterator], "("))
                            {
                                if (ParseBooleanExpression())
                                {
                                    PostfixCode.Add(new TokenInformation("DO", "keyword"));

                                    return _bracketsProcessor.ControlBracketsFlow(SymbolsInformation[_iterator], ")");
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        private bool ProcessIfExpression(SymbolInformation symbolInformation)
        {
            if (ParseToken(("if", "keyword"), symbolInformation))
            {
                if (_bracketsProcessor.ControlBracketsFlow(SymbolsInformation![Iterator], "("))
                {
                    if (ParseBooleanExpression())
                    {
                        if (_bracketsProcessor.ControlBracketsFlow(SymbolsInformation![_iterator], ")"))
                        {
                            _bracketsProcessor.ControlBracketsFlow(SymbolsInformation![Iterator], "{");

                            while (ProcessStatementList()) { }

                            PostfixCode.Add(new TokenInformation("IF", "keyword"));

                            return _bracketsProcessor.ControlBracketsFlow(SymbolsInformation![_iterator], "}");
                        }
                    }
                }
            }

            return false;
        }

        private bool ProcessPrintExpression(SymbolInformation symbolInformation)
        {
            if (ParseToken(("print", "keyword"), symbolInformation))
            {
                if (_bracketsProcessor.ControlBracketsFlow(SymbolsInformation![Iterator], "("))
                {
                    symbolInformation = ParseIdentList(SymbolsInformation[Iterator], ")");

                    if (_bracketsProcessor.ControlBracketsFlow(symbolInformation, ")"))
                    {
                        if (ParseToken((";", "punct"), SymbolsInformation[Iterator]))
                        {
                            PostfixCode.Add(new TokenInformation("print", "keyword"));
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool ProcessInputExpression(SymbolInformation symbolInformation)
        {
            if (ParseToken(("input", "keyword"), symbolInformation))
            {
                if (_bracketsProcessor.ControlBracketsFlow(SymbolsInformation![Iterator], "("))
                {
                    symbolInformation = ParseIdentList(SymbolsInformation[Iterator], ")");

                    if (_bracketsProcessor.ControlBracketsFlow(symbolInformation, ")"))
                    {
                        if (ParseToken((";", "punct"), SymbolsInformation[Iterator]))
                        {
                            PostfixCode.Add(new TokenInformation("input", "keyword"));
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        #endregion
    }
}