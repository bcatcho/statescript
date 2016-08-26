// This file is AUTOGENERATED with RAGEL
// !!DO NOT EDIT!! Change the RL file and compile with Ragel
// See the ScannerGenerator directory.
// http://www.colm.net/open-source/ragel/
using System.Collections.Generic;
using Transition.Compiler.Tokens;

namespace Transition.Compiler
{
   /// <summary>
   /// Scanner performs lexical analysis on a string of characters and
   /// and emits a string of tokens for the Parser to analyze.
   /// </summary>
   public class Scanner
   {
      int _lineNumber = 0;
      bool _tokenUncommitted;
      int _tokenStart { get { return _token.StartIndex; } }
      Token _token;
      private List<Token> _tokens;

      // ragel properties
      private int eof;
      private int cs;
      private int p;

      private void StartToken(TokenType tokenType)
      {
        #if PARSER_LOGGING
        Log(string.Format("start {0}", tokenType));
        #endif
        _token = new Token {
            LineNumber = _lineNumber,
            StartIndex = p,
            TokenType = tokenType
        };
        _tokenUncommitted = true;
      }

      private void StartOperatorToken(TokenOperator tokenOperator)
      {
        #if PARSER_LOGGING
        Log(string.Format("start {0}", tokenOperator));
        #endif
        _token = new Token {
            LineNumber = _lineNumber,
            StartIndex = p,
            Operator = tokenOperator,
            TokenType = TokenType.Operator,
        };
        _tokenUncommitted = true;
      }

      #if PARSER_LOGGING
      private void Log(string msg) {
        Console.WriteLine(string.Format("{0} {1}", p, msg));
      }
      #endif

      private void EmitToken() {
        #if PARSER_LOGGING
        Log(string.Format("emit {0}", _token.TokenType));
        #endif
        _token.Length = p - _tokenStart;
        _tokens.Add(_token);
        _tokenUncommitted = false;
      }

      private void EmitNewLine() {
        // collapse newline tokens down to one
        if (_tokens.Count > 0 && _tokens[_tokens.Count-1].TokenType == TokenType.NewLine)
           return;
        _token.TokenType = TokenType.NewLine;
        #if PARSER_LOGGING
        Log(string.Format("emit {0}", _token.TokenType));
        #endif
        _tokens.Add(_token);
        _tokenUncommitted = false;
      }

      private void SetKeyword(TokenKeyword tokenKeyword) {
        _token.Keyword = tokenKeyword;
      }

      private void CommitLastToken() {
        if (_tokenUncommitted) {
          EmitToken();
        }
      }

      %%{
        machine Scanner;
        include ScannerDef "ScannerDef.rl";
        write data;
      }%%

      ///<summary>
      /// This method will perform lexical analysis on the character sequence input
      //  and will return a sequence of tokens for the Parser to analyze.
      ///</summary>
      ///<returns>
      /// A sequence of tokens that the Parser can use to Analyze.
      ///</returns>
      public List<Token> Scan(char[] data, int len)
      {
         %% write init;
         if (_tokens == null) {
           _tokens = new List<Token>(128);
         }
         _tokens.Clear();
         _lineNumber = 1; // start at line 1 like most text editors
         p = 0;
         int pe = len;
         eof = len;
         %% write exec;
         CommitLastToken();
         return _tokens;
      }

      ///<summary>
      /// Call this method after Scan to know if the parser exited prematurely
      /// due to an error.
      ///</summary>
      ///<returns>
      /// A boolean indicating if the Scanner made it to the end of the input or not.
      ///</returns>
      public bool DidReachEndOfInput()
      {
         return (p >= eof);
      }

      /// <summary>
      /// Returns the error location
      /// </summary>
      public string GetErrorLocation(char[] input, int charCount)
      {
         var end = p;
         var start = p;
         while (start > 0 && end - start < charCount && input[start] != '\n') {
            start--;
         }
         if (input[start] == '\n') {
            start++;
         }

         return "(line " + _lineNumber + "): " + new string(input, start, end - start + 1);
      }
   }
}
