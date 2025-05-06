using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace Compiler
{
    public partial class Form1 : Form
    {
        private static readonly string[] ReservedWord = {
            "int", "char", "bool", "double", "float", "if", "else", "switch",
            "for", "while", "do", "class", "struct", "namespace", "public",
            "private", "protected", "const", "static", "override", "auto", "void", "real", "return"
        };

        private static readonly Dictionary<string, string> multiCharOperators = new Dictionary<string, string>
        {
            {"++", "increment"}, {"--", "decrement"}, {"!=", "not equal"}, {"==", "equal equal"},
            {"<=", "less than or equal"}, {">=", "greater than or equal"}, {"<<", "left shift"},
            {">>", "right shift"}, {"+=", "add assign"}, {"-=", "subtract assign"},
            {"*=", "multiply assign"}, {"/=", "divide assign"}, {"%=", "mod assign"},
            {"||", "logical or"}, {"&&", "logical and"}
        };

        private static readonly Dictionary<char, string> singleCharOperators = new Dictionary<char, string>
        {
            {'=', "assign"}, {'+', "plus"}, {'-', "minus"}, {'*', "multiply"},
            {'/', "divide"}, {'<', "less than"}, {'>', "greater than"}, {'!', "not"},
            {'%', "mod"}, {'&', "bitwise and"}, {'|', "bitwise or"}, {'^', "xor"}
        };

        private static readonly Dictionary<char, string> delimiters = new Dictionary<char, string>
        {
            {';', "semicolon"}, {',', "comma"}, {'(', "left parenthesis"},
            {')', "right parenthesis"}, {'{', "left brace"}, {'}', "right brace"},
            {'[', "left bracket"}, {']', "right bracket"}
        };

        private static readonly HashSet<string> declaredFunctions = new HashSet<string>();

        public Form1()
        {
            InitializeComponent();
            this.BackColor = Color.FromArgb(45, 45, 48);
            listBox1.BackColor = Color.FromArgb(240, 240, 240);
            textBox1.BackColor = Color.FromArgb(240, 240, 240);
            button1.BackColor = Color.FromArgb(0, 150, 136);
            button1.ForeColor = Color.White;
            listBox1.BorderStyle = BorderStyle.FixedSingle;
            textBox1.BorderStyle = BorderStyle.FixedSingle;
            button1.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
        }

        private List<Tuple<int, string, string>> Scan(string inputStr)
        {
            var tokens = new List<Tuple<int, string, string>>();
            string[] lines = inputStr.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            int lineNumber = 1;

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                int position = 0;

                while (position < line.Length)
                {
                    if (char.IsWhiteSpace(line[position]))
                    {
                        position++;
                        continue;
                    }

                    if (position + 1 < line.Length && line[position] == '/' && line[position + 1] == '/')
                    {
                        break;
                    }

                    if (line[position] == '"')
                    {
                        int start = position;
                        position++;
                        while (position < line.Length && line[position] != '"')
                        {
                            if (line[position] == '\\') position++;
                            position++;
                        }
                        if (position >= line.Length)
                        {
                            tokens.Add(Tuple.Create(lineNumber, "error", "Unterminated string literal"));
                            break;
                        }
                        position++;
                        string literal = line.Substring(start, position - start);
                        tokens.Add(Tuple.Create(lineNumber, "string", literal));
                        continue;
                    }

                    bool opFound = false;
                    foreach (var op in multiCharOperators)
                    {
                        if (position + op.Key.Length <= line.Length &&
                            line.Substring(position, op.Key.Length) == op.Key)
                        {
                            tokens.Add(Tuple.Create(lineNumber, multiCharOperators[op.Key], op.Key));
                            position += op.Key.Length;
                            opFound = true;
                            break;
                        }
                    }
                    if (opFound) continue;

                    if (singleCharOperators.ContainsKey(line[position]))
                    {
                        tokens.Add(Tuple.Create(lineNumber, singleCharOperators[line[position]], line[position].ToString()));
                        position++;
                        continue;
                    }

                    if (delimiters.ContainsKey(line[position]))
                    {
                        tokens.Add(Tuple.Create(lineNumber, delimiters[line[position]], line[position].ToString()));
                        position++;
                        continue;
                    }

                    if (char.IsLetter(line[position]) || line[position] == '_')
                    {
                        int start = position;
                        while (position < line.Length &&
                               (char.IsLetterOrDigit(line[position]) || line[position] == '_'))
                            position++;

                        string lexeme = line.Substring(start, position - start);
                        string type = Array.Exists(ReservedWord, x => x == lexeme) ? "keyword" : "id";
                        tokens.Add(Tuple.Create(lineNumber, type, lexeme));
                        continue;
                    }

                    if (char.IsDigit(line[position]) || line[position] == '-' || line[position] == '.')
                    {
                        int start = position;
                        bool hasDecimal = line[position] == '.';
                        if (line[position] == '-') position++;
                        while (position < line.Length &&
                               (char.IsDigit(line[position]) || line[position] == '.' || line[position] == 'e' || line[position] == 'E' || line[position] == '-'))
                        {
                            if (line[position] == '.') hasDecimal = true;
                            if (line[position] == 'e' || line[position] == 'E')
                            {
                                position++;
                                if (position < line.Length && (line[position] == '+' || line[position] == '-')) position++;
                                while (position < line.Length && char.IsDigit(line[position])) position++;
                                break;
                            }
                            position++;
                        }
                        string number = line.Substring(start, position - start);
                        if (Regex.IsMatch(number, @"^-?\d*(\.\d+)?(e[+-]?\d+)?$", RegexOptions.IgnoreCase))
                            tokens.Add(Tuple.Create(lineNumber, "number", number));
                        else
                            tokens.Add(Tuple.Create(lineNumber, "error", $"Invalid number: {number}"));
                        continue;
                    }

                    tokens.Add(Tuple.Create(lineNumber, "error", $"Unrecognized character: {line[position]}"));
                    position++;
                }
                lineNumber++;
            }
            return tokens;
        }

        private string Parse(List<Tuple<int, string, string>> tokens)
        {
            int currentIndex = 0;
            string errorMessage = "";
            declaredFunctions.Clear();

            bool Match(string expected)
            {
                if (currentIndex < tokens.Count && tokens[currentIndex].Item2 == expected)
                {
                    currentIndex++;  // Move to the next token after a match
                    return true;
                }
                else
                {
                    // Debug output to see what's going wrong
                    Console.WriteLine($"Match failed at index {currentIndex}: Expected {expected}, but found {tokens[currentIndex].Item2} ({tokens[currentIndex].Item3})");
                    return false;
                }
            }


            bool MatchKeyword(string keyword)
            {
                if (currentIndex < tokens.Count &&
                    tokens[currentIndex].Item2 == "keyword" &&
                    tokens[currentIndex].Item3 == keyword)
                {
                    currentIndex++;
                    return true;
                }
                return false;
            }

            bool TypeSpecifier()
            {
                if (Match("keyword"))
                {
                    string currentToken = tokens[currentIndex - 1].Item3; // Get the token's value
                    return currentToken == "int" || currentToken == "void" || currentToken == "float" || currentToken == "char";
                }
                return false;
            }



            bool Declaration()
            {
                int backup = currentIndex;

                if (!TypeSpecifier())
                {
                    currentIndex = backup;
                    return false;
                }

                if (!Match("id"))
                {
                    return false;
                }

                // Support optional initialization
                if (Match("assign"))
                {
                    if (!Expression())
                    {
                        return false;
                    }
                }

                // Support multiple declarations separated by commas
                while (Match("comma"))
                {
                    if (!Match("id"))
                    {
                        return false;
                    }

                    // Optional initialization for subsequent declarations
                    if (Match("assign"))
                    {
                        if (!Expression())
                        {
                            return false;
                        }
                    }
                }

                if (!Match("semicolon"))
                {
                    return false;
                }

                return true;
            }


            bool FunctionDeclaration()
            {
                int backup = currentIndex;

                // Check the type specifier (e.g., int, float, void)
                if (!TypeSpecifier())  // Make sure this is correctly matching 'int'
                {
                    currentIndex = backup;
                    return false;
                }

                // Check for function name (id)
                if (!Match("id"))
                {
                    errorMessage = $"Expected function name at line {tokens[currentIndex].Item1}";
                    return false;
                }

                string funcName = tokens[currentIndex - 1].Item3;

                // Check for left parenthesis after function name
                if (!Match("left parenthesis"))
                {
                    errorMessage = $"Expected '(' after function name at line {tokens[currentIndex].Item1}";
                    return false;
                }

                // Parameter list (optional if empty)
                if (!Match("right parenthesis"))
                {
                    // Handle parameters inside the function declaration
                    while (true)
                    {
                        if (!TypeSpecifier())  // Ensure we can match the parameter types
                        {
                            errorMessage = $"Expected parameter type at line {tokens[currentIndex].Item1}";
                            return false;
                        }

                        if (!Match("id"))
                        {
                            errorMessage = $"Expected parameter name at line {tokens[currentIndex].Item1}";
                            return false;
                        }

                        if (Match("right parenthesis")) break;

                        if (!Match("comma"))
                        {
                            errorMessage = $"Expected ',' or ')' in parameter list at line {tokens[currentIndex].Item1}";
                            return false;
                        }
                    }
                }

                // Function body
                if (!Match("left brace"))
                {
                    errorMessage = $"Expected '{{' for function body at line {tokens[currentIndex].Item1}";
                    return false;
                }

                // Handle function body
                while (currentIndex < tokens.Count && !Match("right brace"))
                {
                    if (!Statement() && !Declaration())
                    {
                        errorMessage = $"Syntax error in function body at line {tokens[currentIndex].Item1}";
                        return false;
                    }
                }

                return true;
            }




            bool IfStatement()
            {
                int backup = currentIndex;
                if (MatchKeyword("if"))
                {
                    if (!Match("left parenthesis")) return false;
                    if (!Expression()) return false;
                    if (!Match("right parenthesis")) return false;
                    if (!Statement()) return false;
                    if (MatchKeyword("else"))
                    {
                        if (!Statement()) return false;
                    }
                    return true;
                }
                currentIndex = backup;
                return false;
            }

            bool ReturnStatement()
            {
                int backup = currentIndex;
                if (MatchKeyword("return"))
                {
                    if (!Expression() && !Match("semicolon")) return false;
                    if (!Match("semicolon")) return false;
                    return true;
                }
                currentIndex = backup;
                return false;
            }

            bool ExpressionStatement()
            {
                int backup = currentIndex;

                // Handle simple expression or assignment
                if (Match("id"))
                {
                    if (Match("assign"))
                    {
                        if (!Expression()) return false;  // Ensure we can process the right side expression of the assignment
                    }

                    if (!Match("semicolon")) return false;  // Ensure the statement ends with a semicolon
                    return true;
                }

                currentIndex = backup;
                return false;
            }

            bool Expression()
            {
                int backup = currentIndex;

                if (!Primary())  // Handles id, number, string, (expr), function calls
                {
                    currentIndex = backup;
                    return false;
                }

                // Handle binary operations
                while (IsBinaryOperator(Peek()))
                {
                    Match(Peek());  // consume operator
                    if (!Primary())
                    {
                        currentIndex = backup;
                        return false;
                    }
                }

                return true;
            }

            bool Primary()
            {
                int backup = currentIndex;

                if (Match("number") || Match("string"))
                    return true;

                if (Match("id"))
                {
                    // Check for function call
                    if (Match("left parenthesis"))
                    {
                        if (!Match("right parenthesis"))
                        {
                            do
                            {
                                if (!Expression())
                                {
                                    currentIndex = backup;
                                    return false;
                                }
                            } while (Match("comma"));

                            if (!Match("right parenthesis"))
                            {
                                currentIndex = backup;
                                return false;
                            }
                        }
                    }

                    return true;  // It's either a variable or a function call
                }

                if (Match("left parenthesis"))
                {
                    if (!Expression()) { currentIndex = backup; return false; }
                    if (!Match("right parenthesis")) { currentIndex = backup; return false; }
                    return true;
                }

                currentIndex = backup;
                return false;
            }

            bool IsBinaryOperator(string tokenType)
            {
                return tokenType == "plus" || tokenType == "minus" || tokenType == "multiply" || tokenType == "divide" ||
                       tokenType == "less than" || tokenType == "greater than" || tokenType == "equal equal" ||
                       tokenType == "not equal" || tokenType == "logical and" || tokenType == "logical or" ||
                       tokenType == "bitwise and" || tokenType == "bitwise or" || tokenType == "xor" ||
                       tokenType == "mod" || tokenType == "left shift" || tokenType == "right shift";
            }

            string Peek()
            {
                return currentIndex < tokens.Count ? tokens[currentIndex].Item2 : "";
            }



            bool Statement()
            {
                int backup = currentIndex;

                // Handle block statement
                if (Match("left brace"))
                {
                    while (!Check("right brace") && currentIndex < tokens.Count)
                    {
                        if (!Declaration() && !ReturnStatement() && !IfStatement() && !ExpressionStatement())
                        {
                            errorMessage = $"Syntax error in block at line {tokens[currentIndex].Item1}";
                            return false;
                        }
                    }

                    if (!Match("right brace"))
                    {
                        errorMessage = $"Expected '}}' at line {tokens[currentIndex].Item1}";
                        return false;
                    }

                    return true;
                }

                currentIndex = backup;

                // Try other statement types
                return Declaration() || IfStatement() || ReturnStatement() || ExpressionStatement();
            }

            bool Check(string expectedType)
            {
                if (currentIndex >= tokens.Count) return false;
                return tokens[currentIndex].Item2 == expectedType;
            }



            bool Program()
            {
                while (currentIndex < tokens.Count)
                {
                    if (tokens[currentIndex].Item2 == "error")
                    {
                        errorMessage = $"Scanner error at line {tokens[currentIndex].Item1}: {tokens[currentIndex].Item3}";
                        return false;
                    }

                    int backupIndex = currentIndex;

                    if (Declaration() || FunctionDeclaration() || Statement())
                    {
                        continue;
                    }

                    // restore index in case of failure
                    currentIndex = backupIndex;
                    errorMessage = $"Syntax error at line {tokens[currentIndex].Item1}";
                    return false;
                }

                return true;
            }


            bool success = Program();
            return success ? "Parsing successful" : $"Parsing failed: {errorMessage}";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                listBox1.Items.Clear();
                var tokens = Scan(textBox1.Text);

                foreach (var token in tokens)
                {
                    Console.WriteLine($"{token.Item1}: {token.Item2} => {token.Item3}");
                }


                int maxTokens = 1000;
                for (int i = 0; i < Math.Min(tokens.Count, maxTokens); i++)
                {
                    var token = tokens[i];
                    listBox1.Items.Add($"Line {token.Item1}: {token.Item2.PadRight(12)} {token.Item3}");
                }
                if (tokens.Count > maxTokens)
                {
                    listBox1.Items.Add($"... (Truncated, total tokens: {tokens.Count})");
                }

                string parseResult = Parse(tokens);
                MessageBox.Show(parseResult, "Parsing Result",
                              MessageBoxButtons.OK,
                              parseResult.Contains("failed") ? MessageBoxIcon.Error : MessageBoxIcon.Information);
            }
            catch (OutOfMemoryException)
            {
                MessageBox.Show("Error: Input is too large to process.", "Memory Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}", "Runtime Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}