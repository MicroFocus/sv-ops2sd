/*
 * © 2021 EntIT Software LLC, a Micro Focus company, L.P.
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *   http://www.apache.org/licenses/LICENSE-2.0
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ops2sd {
    public class Parameter {
        public string Name;
        public string Description;
        public int StartByte;
        public int EndByte;
        public string CorrelationId;
        public bool UnknownLength;

        public Parameter(string correlationId) {
            CorrelationId = correlationId;
        }

        public Parameter(string name, string desc, int start, int end, string correlationId, bool variableLength) {
            Name = name;
            Description = desc;
            StartByte = start;
            EndByte = end;
            CorrelationId = correlationId;
            UnknownLength = variableLength;
        }

        public Parameter(string name, string desc, int start, string correlationId, bool variableLength) {
            Name = name;
            Description = desc;
            StartByte = start;
            EndByte = start;
            CorrelationId = correlationId;
            UnknownLength = variableLength;
        }

        public bool IsCorrelation() {
            return CorrelationId != null;
        }

        override
        public string ToString() {
            return Name + " [" + StartByte + "-" + EndByte + "]";
        }
    }
}
