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
using System.Globalization;
using System.IO;
using System.Text;

namespace ops2sd {

    public class ServiceDescriptionCreator {
        private static TextInfo txtInfo = new CultureInfo("en-us", false).TextInfo;
        private static string templateFooter = File.ReadAllText(@"Resources\sd-template-footer.txt");
        private static string templateHeaderMid = File.ReadAllText(@"Resources\sd-template-header-mid.txt");
        private static string templateHeaderMidRevision = File.ReadAllText(@"Resources\sd-template-header-mid-revision.txt");
        private static string templateHeaderMidRevision1 = File.ReadAllText(@"Resources\sd-template-header-mid-revision1.txt");

        private static string CorrelationIDPrefix = "CorrelationID:";
        private static string CorrelationFixedPrefix = "Fixed:";
        private static string CorrelationPayloadPrefix = "Payload:";

        private static string CorrelationIDMarkerServiceDescription = "CorrelationID";

        public static void CreateServiceDescription(string targetFolder, Specification specification) {
            string dstFilename = CreateTargetFilename(specification);
            Console.WriteLine("DST: " + dstFilename);

            StringBuilder sb = new StringBuilder();

            WriteHeader(sb, specification);

            // offset where header ends 
            int offset = 20;

            foreach (Parameter param in specification.parameters) {
                offset = CreateField(sb, offset, param, specification.revision);
            }

            if (!specification.HasUnknownLengthParameter()) {
                WriteFooter(sb, specification.mid, specification.revision, offset);
            }

            File.WriteAllText(Path.Combine(targetFolder, dstFilename), sb.ToString());
        }

        private static void WriteHeader(StringBuilder sb, Specification specification) {
            string thisHeader = null;

            // choose template
            if (specification.revision == "000") {
                thisHeader = templateHeaderMid;
            } else if (specification.revision == "001") {
                thisHeader = templateHeaderMidRevision1;
            } else {
                thisHeader = templateHeaderMidRevision;
            }

            string correlationInHeader = "";
            Parameter correlationParam = specification.correlateBy;
            if (correlationParam != null && correlationParam.Name == null && correlationParam.StartByte == 0) {
                correlationInHeader = correlationParam.CorrelationId.Substring(CorrelationIDPrefix.Length);

                if (correlationInHeader.StartsWith(CorrelationFixedPrefix)) {
                    correlationInHeader = CorrelationIDMarkerServiceDescription + ':' + correlationInHeader.Substring(CorrelationFixedPrefix.Length);
                } else {
                    throw new Exception("Unsupported correlation type for header [" + correlationInHeader + "]");
                }
            }

            thisHeader = thisHeader.Replace("{MID}", specification.mid);
            thisHeader = thisHeader.Replace("{REVISION}", specification.revision);
            thisHeader = thisHeader.Replace("{CORRELATION}", correlationInHeader);

            sb.Append(thisHeader).Append("\r\n");
        }

        private static void WriteFooter(StringBuilder sb, string mid, string revision, int offset) {
            string thisFooter = templateFooter.Replace("{OFFSET}", offset.ToString());
            sb.Append(thisFooter).Append("\r\n");
        }

        private static string CreateFieldName(string specName) {
            specName = specName.ToLower();
            specName = specName.Replace('\\', ' ');
            specName = specName.Replace('/', ' ');
            specName = specName.Replace('.', ' ');
            specName = specName.Replace('-', ' ');
            specName = specName.Replace("serial number", "SN");
            specName = specName.Replace("number", "no");
            specName = specName.Replace("parameter", "param");
            specName = specName.Replace("status", "stat");
            specName = specName.Replace("monitoring", "mon");
            specName = specName.Replace("version", "ver");
            specName = specName.Replace("software", "SW");
            specName = specName.Replace("system", "Sys");
            specName = specName.Replace("sequence", "Seq");
            specName = specName.Replace("pid", "PID");
            specName = txtInfo.ToTitleCase(specName);
            specName = specName.Replace(" ", "");
            return specName;
        }


        private static int CreateField(StringBuilder sb, int offset, Parameter parameter, string revision) {
            int fieldLength = parameter.EndByte - parameter.StartByte + 1;

            string fieldName = CreateFieldName(parameter.Name);

            string type = null;
            if (parameter.Description.ToLower().Contains("binary")) {
                type = "Binary";
            } else {
                type = "String";
            }

            string description = "";
            if (parameter.UnknownLength) {
                description = "Variable/Unknown length field";
            }

            string correlationId = null;
            if (parameter.CorrelationId != null) {
                correlationId = parameter.CorrelationId.Substring(CorrelationIDPrefix.Length);
            }

            offset = CreateField(sb, offset, fieldLength, fieldName, type, description, parameter.Name.EndsWith("PID") ? parameter.Description.Trim() : "", correlationId);

            return offset;
        }

        private static int CreateField(StringBuilder sb, int offset, int fieldLength, string parameterName, string type, string description, string defaultValue, string correlationId) {
            sb.Append(parameterName);
            sb.Append('\t').Append(offset);
            sb.Append('\t').Append(fieldLength);
            sb.Append('\t').Append(type);

            sb.Append('\t');
            if (description != null) {
                sb.Append(description);
            }

            sb.Append('\t');
            // append default value
            if (defaultValue != null) {
                sb.Append(defaultValue);
            }

            sb.Append('\t');
            if (correlationId != null) {
                if (correlationId.StartsWith(CorrelationFixedPrefix)) {
                    correlationId = correlationId.Substring(CorrelationFixedPrefix.Length);
                    sb.Append(CorrelationIDMarkerServiceDescription).Append(':').Append(correlationId);
                } else if (correlationId.StartsWith(CorrelationPayloadPrefix)) {
                    correlationId = correlationId.Substring(CorrelationPayloadPrefix.Length);
                    sb.Append(CorrelationIDMarkerServiceDescription);
                    string[] possiblePayloadValues = correlationId.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries);
                    Array.Sort(possiblePayloadValues, (x, y) => String.Compare(x, y));
                    foreach (string value in possiblePayloadValues) {
                        sb.Append("\r\n");
                        sb.Append("\t\t\t\t\t").Append(value);
                    }
                }
                
            }

            sb.Append("\r\n");
            return offset + fieldLength;
        }

        private static string CreateTargetFilename(Specification specification) {
            string mid = specification.mid;
            if (mid[0] == '0') {
                mid = mid.Substring(1, 3);
            }

            string dstFilename;
            if (specification.revision == "000") {
                dstFilename = "MID-" + mid + "-" + specification.operationName;
            } else {
                dstFilename = "MID-" + mid + "." + specification.revision[2] + "-" + specification.operationName;
            }

            // do we have specific direction?
            if (specification.direction == Specification.MessageDirection.Request) {
                dstFilename += "-request";
            } else if (specification.direction == Specification.MessageDirection.Response) {
                dstFilename += "-response";
            }

            dstFilename = dstFilename + ".txt";

            return dstFilename;
        }
    }
}
