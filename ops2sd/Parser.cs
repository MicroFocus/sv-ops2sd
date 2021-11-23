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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ops2sd {
    public class Parser {

        static private string CorrelateWithLineStart = "CorrelationID";

        public static Specification ParseSpecification(string specPath) {
            string specFilename = Path.GetFileName(specPath);

            Console.WriteLine("SRC: " + Path.GetFileName(specFilename));

            Specification specs = CreateSpecification(specFilename);

            string[] content = File.ReadAllLines(specPath);

            for (int i = 0; i < content.Length; i++) {
                if (i == 0 && content[i].Trim().StartsWith(CorrelateWithLineStart)) {
                    // first line can have correlation, which needs to be placed into the header parameter
                    Parameter headerCorrelation = new Parameter(content[i].Trim());
                    specs.correlateBy = headerCorrelation;
                } else {
                    Parameter newParam = ParseLine(content[i]);
                    if (newParam != null) {
                        specs.parameters.Add(newParam);

                        if (newParam.IsCorrelation()) {
                            if (specs.correlateBy != null) {
                                throw new Exception("Correlation already set to [" + specs.correlateBy + "]");
                            }
                            specs.correlateBy = newParam;
                        }
                    }

                }
            }

            return specs;
        }

        public static Specification CreateSpecification(string filename) {
            string[] fileNameComponents = filename.Substring(0, filename.Length - 4).Split('-');
            string mid = fileNameComponents[1];
            string revision = fileNameComponents[2];
            string operationName = fileNameComponents[3];

            string requestResponse = null;
            if (fileNameComponents.Length > 4) {
                requestResponse = fileNameComponents[4];
            }

            Specification.MessageDirection direction = Specification.MessageDirection.Both;

            if (requestResponse == "request") {
                direction = Specification.MessageDirection.Request;
            } else if (requestResponse == "response") {
                direction = Specification.MessageDirection.Response;
            }

            Specification specs = new Specification(mid, revision, operationName, direction);
            return specs;
        }

        private static Parameter ParseLine(string line) {
            string[] lineComponents = line.Split('\t');

            if (lineComponents.Length == 1) {
                // this happens if we have just continuation of a description of subsequent lines, which can be ignored
                return null;
            }

            string fieldName = null;
            string fieldIdBytes = null;
            string fieldIdValue = null;
            string correlationIdValue = null;

            if (lineComponents.Length == 3 || lineComponents.Length == 4) {
                fieldName = lineComponents[0];
                fieldIdBytes = lineComponents[1];
                fieldIdValue = lineComponents[2];

                if (lineComponents.Length == 4) {
                    correlationIdValue = lineComponents[3];
                    if (!correlationIdValue.StartsWith("CorrelationID")) {
                        throw new Exception("Correlation data must start with CorrelationID string, but we have [" + correlationIdValue + "]");
                    }
                }

            } else {
                throw new Exception("Unable to parse line: [" + line + "]");
            }

            Parameter newParam = null;
            if (fieldIdBytes.Length == 0) {
                // correlation field
                newParam = new Parameter(correlationIdValue);
            } else {
                bool variableLength;
                int[] fieldIdBytesInt = ParseBytes(fieldIdBytes, fieldName, out variableLength);
                
                if (fieldIdBytesInt.Length == 2) {
                    newParam = new Parameter(fieldName, fieldIdValue, fieldIdBytesInt[0], fieldIdBytesInt[1], correlationIdValue, variableLength);
                } else {
                    newParam = new Parameter(fieldName, fieldIdValue, fieldIdBytesInt[0], correlationIdValue, variableLength);
                }
            }

            return newParam;
        }

        private static int[] ParseBytes(string bytes, string fieldName, out bool variableLength) {
            variableLength = false;

            string[] bytesComponents = bytes.Split('-');

            int[] bytesInt;

            if (bytesComponents.Length == 1) {
                bytesInt = new int[1];
                bytesInt[0] = int.Parse(bytesComponents[0]);
            } else if (bytesComponents.Length == 2) {
                bytesInt = new int[2];
                bytesInt[0] = int.Parse(bytesComponents[0]);
                if (bytesComponents[1] == "?") {
                    bytesInt[1] = 9999; // max variable length
                    variableLength = true;
                } else {
                    bytesInt[1] = int.Parse(bytesComponents[1]);
                }
            } else {
                throw new Exception("Field name " + fieldName + ", Field id bytes " + bytes);
            }

            return bytesInt;
        }
    }
}
