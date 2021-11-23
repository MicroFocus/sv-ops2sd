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
using System.Collections.Generic;

namespace ops2sd {
    public class Specification {

        public enum MessageDirection {
            Request,
            Response,
            Both
        };

        public string mid;
        public string revision;
        public string operationName;
        public Parameter correlateBy;
        public MessageDirection direction;

        public List<Parameter> parameters = new List<Parameter>();

        public Specification(string mid, string revision, string operationName, MessageDirection direction) {
            this.mid = mid;
            this.revision = revision;
            this.operationName = operationName;
            this.direction = direction;
        }

        public List<string> GetServiceDescriptionTypes() {
            List<string> types = new List<string>();

            if (revision == "000") {
                types.Add(mid);
            } else if (revision == "001") {
                types.Add(mid + "   ");
                types.Add(mid + "001");
            } else {
                types.Add(mid + revision);
            }

            return types;
        }

        public bool HasUnknownLengthParameter() {
            foreach (Parameter p in parameters) {
                if (p.UnknownLength) {
                    return true;
                }
            }
            return false;
        }

        override
        public string ToString() {
            return mid + "." + revision;
        }

    }
}
