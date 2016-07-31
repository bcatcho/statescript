// This file is AUTOGENERATED with RAGEL
// !!DO NOT EDIT!! Change the RL file and compile with Ragel
// http://www.colm.net/open-source/ragel/
// This RL file is the RAGEL file that generates the parser
// Build and test:
// alias mcs=/Library/Frameworks/Mono.framework/Versions/Current/bin/mcs
// ragel -A Parser.rl && mcs *.cs -define:RAGEL_TEST && mono parser.exe
// ragel -o Parser.cs -A ParserDefinition.rl.cs && mcs Parser.cs -define:RAGEL_TEST && mono parser.exe
// make sure AstNode.cs and AstParam.cs are in the same folder
namespace Statescript.Compiler
{
   using System;
   using System.Collections.Generic;

   public enum TokenType
   {
     Keyword,
     Identifier,
     Value,
     TransitionValue,
     MessageValue,
     Operator,
     NewLine
   }

   public enum TokenOperator
   {
     Set,
     Transition
   }

   public struct Token
   {
     public string Value;
     public int StartIndex;
     public int Length;
     public int LineNumber;
     public TokenType TokenType;
     public TokenOperator Operator;
   }

   public class Tokenizer
   {
      int _lineNumber = 0;
      int _tokenStart = 0;
      private List<Token> _tokens;
      char[] _data;
      // ragel properties
      private int cs;
      int p;

      private void StartToken()
      {
        _tokenStart = p;
      }

      private void log(string msg) {
        Console.WriteLine(string.Format("{0} {1}", p, msg));
      }

      private void logEnd(string msg) {
        var token = new String(_data, _tokenStart, p - _tokenStart);
        Console.WriteLine(string.Format("{0} {1}: {2}", p, msg, token));
      }

      private void EmitOperator(TokenOperator tokenOperator) {
        _tokens.Add(new Token {
          LineNumber = _lineNumber,
          Operator = tokenOperator,
          TokenType = TokenType.Operator
        });
      }

      private void EmitToken(TokenType tokenType) {
        var token = new Token {
          LineNumber = _lineNumber,
          StartIndex = _tokenStart,
          Length = p - _tokenStart,
          Value = new String(_data, _tokenStart, p - _tokenStart),
          TokenType = tokenType
        };

        if (tokenType == TokenType.TransitionValue
            || tokenType == TokenType.Value
            || tokenType == TokenType.MessageValue) {
          // remove quotes
          token.StartIndex = token.StartIndex + 1;
          token.Length = token.Length - 2;
        }

        _tokens.Add(token);
      }

      private void EmitNewLine() {
        _tokens.Add(new Token {
          LineNumber = _lineNumber,
          TokenType = TokenType.NewLine
        });
      }

      %%{
        machine Tokenizer;
        include TokenizerDef "TokenizerDef.rl";
        write data;
      }%%

      public void Init()
      {
         %% write init;
      }

      public List<Token> Tokenize(char[] data, int len)
      {
         if (_tokens == null) {
           _tokens = new List<Token>(128);
         }
         _tokens.Clear();
         _lineNumber = 1; // start at line 1 like most text editors
         _data = data;
         _tokenStart = 0;
         p = 0;
         int pe = len;
         int eof = len;
         %% write exec;
         return _tokens;
      }

      public bool Finish()
      {
         return (cs >= Tokenizer_first_final);
      }
   }
}