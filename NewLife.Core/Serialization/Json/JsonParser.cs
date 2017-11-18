using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NewLife.Collections;

namespace NewLife.Serialization
{

    /// <summary>Json������</summary>
    public class JsonParser
    {
        #region �ڲ�
        /// <summary>��ʶ��</summary>
        enum Token
        {
            None = -1,

            /// <summary>�������</summary>
            Curly_Open,

            /// <summary>�Ҵ�����</summary>
            Curly_Close,

            /// <summary>������</summary>
            Squared_Open,

            /// <summary>�ҷ�����</summary>
            Squared_Close,

            /// <summary>ð��</summary>
            Colon,

            /// <summary>����</summary>
            Comma,

            /// <summary>�ַ���</summary>
            String,

            /// <summary>����</summary>
            Number,

            /// <summary>������</summary>
            True,

            /// <summary>������</summary>
            False,

            /// <summary>��ֵ</summary>
            Null
        }
        #endregion

        #region ����
        readonly String _json;
        readonly StringBuilder _builder = new StringBuilder();
        Token _Ahead = Token.None;
        Int32 index;
        #endregion

        /// <summary>ʵ����</summary>
        /// <param name="json"></param>
        public JsonParser(String json)
        {
            _json = json;
        }

        /// <summary>����</summary>
        /// <returns></returns>
        public Object Decode() { return ParseValue(); }

        private Dictionary<String, Object> ParseObject()
        {
            var dic = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);

            SkipToken(); // {

            while (true)
            {
                switch (LookAhead())
                {

                    case Token.Comma:
                        SkipToken();
                        break;

                    case Token.Curly_Close:
                        SkipToken();
                        return dic;

                    default:
                        {
                            // ����
                            var name = ParseName();

                            // :
                            if (NextToken() != Token.Colon) throw new XException("�� {0} ��Ҫð��");

                            // ֵ
                            dic[name] = ParseValue();
                        }
                        break;
                }
            }
        }

        private List<Object> ParseArray()
        {
            var arr = new List<Object>();
            SkipToken(); // [

            while (true)
            {
                switch (LookAhead())
                {
                    case Token.Comma:
                        SkipToken();
                        break;

                    case Token.Squared_Close:
                        SkipToken();
                        return arr;

                    default:
                        arr.Add(ParseValue());
                        break;
                }
            }
        }

        private Object ParseValue()
        {
            switch (LookAhead())
            {
                case Token.Number:
                    return ParseNumber();

                case Token.String:
                    var str = ParseString();
                    // �п������ַ�����ʱ������
                    var str2 = str.Substring("/Date(", ")/");
                    if (str2.IsNullOrEmpty()) return str;

                    return new DateTime(1970, 1, 1).AddMilliseconds(Convert.ToInt64(str2));

                case Token.Curly_Open:
                    return ParseObject();

                case Token.Squared_Open:
                    return ParseArray();

                case Token.True:
                    SkipToken();
                    return true;

                case Token.False:
                    SkipToken();
                    return false;

                case Token.Null:
                    SkipToken();
                    return null;
            }

            throw new XException("�� {0} �ı�ʶ���޷�ʶ��", index);
        }

        private String ParseName()
        {
            SkipToken(); // "

            _builder.Length = 0;

            var runIndex = -1;

            while (index < _json.Length)
            {
                var c = _json[index++];

                if (c == '"')
                {
                    if (runIndex != -1)
                    {
                        if (_builder.Length == 0)
                            return _json.Substring(runIndex, index - runIndex - 1);

                        _builder.Append(_json, runIndex, index - runIndex - 1);
                    }
                    return _builder.ToString();
                }
                else if (c == ':')
                {
                    // �����û��˫���ŵ����֣����˻�һ���ַ�
                    index--;

                    if (runIndex != -1)
                    {
                        if (_builder.Length == 0)
                            return _json.Substring(runIndex, index + 1 - runIndex - 1);

                        _builder.Append(_json, runIndex, index + 1 - runIndex - 1);
                    }
                    return _builder.ToString();
                }

                if (c != '\\')
                {
                    if (runIndex == -1) runIndex = index - 1;

                    continue;
                }

                if (index == _json.Length) break;

                if (runIndex != -1)
                {
                    _builder.Append(_json, runIndex, index - runIndex - 1);
                    runIndex = -1;
                }

                switch (_json[index++])
                {
                    case '"':
                        _builder.Append('"');
                        break;

                    case '\\':
                        _builder.Append('\\');
                        break;

                    case '/':
                        _builder.Append('/');
                        break;

                    case 'b':
                        _builder.Append('\b');
                        break;

                    case 'f':
                        _builder.Append('\f');
                        break;

                    case 'n':
                        _builder.Append('\n');
                        break;

                    case 'r':
                        _builder.Append('\r');
                        break;

                    case 't':
                        _builder.Append('\t');
                        break;

                    case 'u':
                        {
                            var remainingLength = _json.Length - index;
                            if (remainingLength < 4) break;

                            // ����32λʮ����������
                            var codePoint = ParseUnicode(_json[index], _json[index + 1], _json[index + 2], _json[index + 3]);
                            _builder.Append((Char)codePoint);

                            index += 4;
                        }
                        break;
                }
            }

            throw new Exception("�ѵ����ַ�����β");
        }

        private String ParseString()
        {
            SkipToken(); // "

            _builder.Length = 0;

            var runIndex = -1;

            while (index < _json.Length)
            {
                var c = _json[index++];

                if (c == '"')
                {
                    if (runIndex != -1)
                    {
                        if (_builder.Length == 0)
                            return _json.Substring(runIndex, index - runIndex - 1);

                        _builder.Append(_json, runIndex, index - runIndex - 1);
                    }
                    return _builder.ToString();
                }

                if (c != '\\')
                {
                    if (runIndex == -1) runIndex = index - 1;

                    continue;
                }

                if (index == _json.Length) break;

                if (runIndex != -1)
                {
                    _builder.Append(_json, runIndex, index - runIndex - 1);
                    runIndex = -1;
                }

                switch (_json[index++])
                {
                    case '"':
                        _builder.Append('"');
                        break;

                    case '\\':
                        _builder.Append('\\');
                        break;

                    case '/':
                        _builder.Append('/');
                        break;

                    case 'b':
                        _builder.Append('\b');
                        break;

                    case 'f':
                        _builder.Append('\f');
                        break;

                    case 'n':
                        _builder.Append('\n');
                        break;

                    case 'r':
                        _builder.Append('\r');
                        break;

                    case 't':
                        _builder.Append('\t');
                        break;

                    case 'u':
                        {
                            var remainingLength = _json.Length - index;
                            if (remainingLength < 4) break;

                            // ����32λʮ����������
                            var codePoint = ParseUnicode(_json[index], _json[index + 1], _json[index + 2], _json[index + 3]);
                            _builder.Append((Char)codePoint);

                            index += 4;
                        }
                        break;
                }
            }

            throw new Exception("�ѵ����ַ�����β");
        }

        private UInt32 ParseSingleChar(Char c1, UInt32 multipliyer)
        {
            UInt32 p1 = 0;
            if (c1 >= '0' && c1 <= '9')
                p1 = (UInt32)(c1 - '0') * multipliyer;
            else if (c1 >= 'A' && c1 <= 'F')
                p1 = (UInt32)((c1 - 'A') + 10) * multipliyer;
            else if (c1 >= 'a' && c1 <= 'f')
                p1 = (UInt32)((c1 - 'a') + 10) * multipliyer;
            return p1;
        }

        private UInt32 ParseUnicode(Char c1, Char c2, Char c3, Char c4)
        {
            var p1 = ParseSingleChar(c1, 0x1000);
            var p2 = ParseSingleChar(c2, 0x100);
            var p3 = ParseSingleChar(c3, 0x10);
            var p4 = ParseSingleChar(c4, 1);

            return p1 + p2 + p3 + p4;
        }

        private Int64 CreateLong(String s)
        {
            Int64 num = 0;
            var neg = false;
            foreach (var cc in s)
            {
                if (cc == '-')
                    neg = true;
                else if (cc == '+')
                    neg = false;
                else
                {
                    num *= 10;
                    num += (Int32)(cc - '0');
                }
            }

            return neg ? -num : num;
        }

        private Object ParseNumber()
        {
            SkipToken();

            // ��Ҫ�ع�1��λ�ã���Ϊ��һ������Ҳ��Toekn�����ܱ�������
            var startIndex = index - 1;
            var dec = false;
            do
            {
                if (index == _json.Length)
                    break;
                var c = _json[index];

                if ((c >= '0' && c <= '9') || c == '.' || c == '-' || c == '+' || c == 'e' || c == 'E')
                {
                    if (c == '.' || c == 'e' || c == 'E')
                        dec = true;
                    if (++index == _json.Length) break;

                    continue;
                }
                break;
            } while (true);

            if (dec)
            {
                var s = _json.Substring(startIndex, index - startIndex);
                return Double.Parse(s, NumberFormatInfo.InvariantInfo);
            }

            return CreateLong(out var num, _json, startIndex, index - startIndex);
        }

        private Token LookAhead()
        {
            if (_Ahead != Token.None) return _Ahead;

            return _Ahead = NextTokenCore();
        }

        /// <summary>��ȡһ��Token</summary>
        private void SkipToken()
        {
            _Ahead = Token.None;
        }

        private Token NextToken()
        {
            var rs = _Ahead != Token.None ? _Ahead : NextTokenCore();

            _Ahead = Token.None;

            return rs;
        }

        private Token NextTokenCore()
        {
            Char ch;

            // �����հ׷�
            do
            {
                ch = _json[index];

                if (ch > ' ') break;
                if (ch != ' ' && ch != '\t' && ch != '\n' && ch != '\r') break;

            } while (++index < _json.Length);

            if (index == _json.Length) throw new Exception("�ѵ����ַ�����β");

            ch = _json[index];

            index++;

            switch (ch)
            {
                case '{':
                    return Token.Curly_Open;

                case '}':
                    return Token.Curly_Close;

                case '[':
                    return Token.Squared_Open;

                case ']':
                    return Token.Squared_Close;

                case ',':
                    return Token.Comma;

                case '"':
                    return Token.String;

                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                case '+':
                case '.':
                    return Token.Number;

                case ':':
                    return Token.Colon;

                case 'f':
                    if (_json.Length - index >= 4 &&
                        _json[index + 0] == 'a' &&
                        _json[index + 1] == 'l' &&
                        _json[index + 2] == 's' &&
                        _json[index + 3] == 'e')
                    {
                        index += 4;
                        return Token.False;
                    }
                    break;

                case 't':
                    if (_json.Length - index >= 3 &&
                        _json[index + 0] == 'r' &&
                        _json[index + 1] == 'u' &&
                        _json[index + 2] == 'e')
                    {
                        index += 3;
                        return Token.True;
                    }
                    break;

                case 'n':
                    if (_json.Length - index >= 3 &&
                        _json[index + 0] == 'u' &&
                        _json[index + 1] == 'l' &&
                        _json[index + 2] == 'l')
                    {
                        index += 3;
                        return Token.Null;
                    }
                    break;

                // Ĭ����û��˫���ŵ�key
                default: index--; return Token.String;
            }
            throw new XException("�޷��� {0} �ҵ�Token", --index);
        }

        static Int64 CreateLong(out Int64 num, String s, Int32 index, Int32 count)
        {
            num = 0;
            var neg = false;
            for (var x = 0; x < count; x++, index++)
            {
                var cc = s[index];

                if (cc == '-')
                    neg = true;
                else if (cc == '+')
                    neg = false;
                else
                {
                    num *= 10;
                    num += (Int32)(cc - '0');
                }
            }
            if (neg) num = -num;

            return num;
        }
    }
}