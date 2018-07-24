//
// LuaException.cs
//
// Author:
//       Chris Howie <me@chrishowie.com>
//
// Copyright (c) 2013 Chris Howie
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Text;

namespace Eluant
{
    public class LuaException : Exception
    {
        internal string forcedStackTrace;

        internal string tracebackString;
        public LuaValue Value;
        private int tracebackHashCode;
        private string [] cachedTracebackArray;
        private string cachedTraceback;
        private string cachedStackTrace;

        private string[] getTracebackArray() {
            var traceback = new List<string>();

            if (tracebackString != null && tracebackString.Trim() != string.Empty) {
                foreach (var l in tracebackString.Split('\n')) {
                    traceback.Add(l);
                }
                tracebackHashCode = tracebackString.GetHashCode();
            }

            if (StackTrace != null) {
                var split = StackTrace.Split('\n');
                foreach (var l in split) {
                    traceback.Add(l.Replace("at", "[clr]:").Trim());
                }
            }

            return cachedTracebackArray = traceback.ToArray();
        }

        private string getTraceback() {
            var s = new StringBuilder();

            if (tracebackString != null) {
                foreach (var l in tracebackString.Split('\n')) {
                    s.AppendLine(l);
                }
                tracebackHashCode = tracebackString.GetHashCode();
            }

            if (base.StackTrace != null) {
                var split = base.StackTrace.Split('\n');
                foreach (var l in split) {
                    s.AppendLine(l.Replace("  at", "[clr]:"));
                }
            }

            return cachedTraceback = s.ToString();
        }

        public string[] TracebackArray {
            get {
                if (tracebackString?.GetHashCode() == tracebackHashCode) {
                    return cachedTracebackArray;
                } else {
                    return getTracebackArray();
                }
            }
        }

        public string Traceback {
            get {
                if (tracebackString?.GetHashCode() == tracebackHashCode) {
                    return cachedTraceback;
                } else {
                    return getTraceback();
                }
            }
        }


        public LuaException(string message, string traceback = null) : this(message, null, null, traceback) { }

        public LuaException(string message, Exception inner, LuaValue value = null, string traceback = null) : base(value?.ToString() ?? message, inner) {
            tracebackString = traceback;
            tracebackHashCode = -1;
            Value = value;
        }

        public override string ToString()
        {
            var s = new StringBuilder();
            s.Append(GetType().FullName).Append(": ").Append(Message);
            if (Value != null) s.Append(" [").Append(Value.ToString()).Append("]");
            s.AppendLine();
            for (int i = 0; i < TracebackArray.Length; i++) {
                s.Append("  ").AppendLine(TracebackArray [i]);
            }
            return s.ToString();
        }

        public override string StackTrace {
            get {
                return forcedStackTrace ?? base.StackTrace;
            }
        }
    }
}

