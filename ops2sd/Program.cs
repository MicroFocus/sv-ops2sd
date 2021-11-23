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

namespace ops2sd {
    class Program {
        private const string specFileFilter = "MID-*.txt";

        static void Main(string[] args) {
            Console.WriteLine("Open Protocol Specification to Service Description converter v1.0");
            Console.WriteLine("Service Description can be used to create a virtual service in a Micro Focus Service Virtualization");
            Console.WriteLine();

            if (args.Length == 0) {
                Console.WriteLine("Usage: ops2sd <source folder> <target folder>");
                Console.WriteLine("  <source folder> ... specification files must follow MID-*.txt pattern");
                return;
            } else if (args.Length != 2) {
                Console.WriteLine("ERROR: Expected 2 arguments, but " + args.Length + " found!");
                return;
            }

            string srcFolder = args[0];
            string dstFolder = args[1];

            Console.WriteLine("Source folder: " + srcFolder);
            if (!Directory.Exists(srcFolder)) {
                Console.WriteLine("ERROR: Source folder does not exist");
                return;
            }

            Console.WriteLine("Target folder: " + dstFolder);
            if (!Directory.Exists(dstFolder)) {
                Console.WriteLine("WARNING: Destination folder does not exist. Creating");
                Directory.CreateDirectory(dstFolder);
            }

            srcFolder = Path.GetFullPath(srcFolder);
            dstFolder = Path.GetFullPath(dstFolder);

            if (srcFolder == dstFolder) {
                Console.WriteLine("ERROR: Source and destination folders must not be the same");
                return;
            }
            
            Console.WriteLine("Specification files found: " + Directory.EnumerateFiles(srcFolder, specFileFilter).Count());
            Console.WriteLine();

            ProcessSpecs(srcFolder, dstFolder);
        }

        private static void ProcessSpecs(string specFolder, string sdFolder) {
            List<Specification> specifications = new List<Specification>();

            foreach (string specFilePath in Directory.EnumerateFiles(specFolder, specFileFilter)) {
                Specification specification = Parser.ParseSpecification(specFilePath);
                specifications.Add(specification);

                PostProcessSpecificationParameters(specification.parameters);

                Console.WriteLine("DONE");
            }

            foreach (Specification specification in specifications) {
                ServiceDescriptionCreator.CreateServiceDescription(sdFolder, specification);
                Console.WriteLine("DONE");
            }
        }

        /// <summary>
        /// Validates and postprocesses specification.
        /// </summary>
        /// <param name="parameters"></param>
        private static void PostProcessSpecificationParameters(List<Parameter> parameters) {
            for (int i = 0; i < parameters.Count; i++) {
                if (i == 0 && parameters[i].CorrelationId == null) {
                    continue;
                }
                if (i > 0) {
                    // check offset and length follows one by another
                    if (parameters[i].StartByte != parameters[i-1].EndByte + 1) {
                        Console.WriteLine("ERROR: start byte offset does not follow end byte offset of the previous parameter");
                        Console.WriteLine("  Previous parameter: [" + parameters[i-1] + "]");
                        Console.WriteLine("  Current parameter:  [" + parameters[i] + "]");

                        throw new Exception("Specification faulty.");
                    }
                }

                if (string.IsNullOrEmpty(parameters[i].Name)) {
                    parameters[i].Name = parameters[i - 1].Name;
                    parameters[i - 1].Name = parameters[i - 1].Name + " PID";
                }

                if (parameters[i].UnknownLength && i < parameters.Count - 1) {
                    throw new Exception("Parameter with variable/unknown length can be only at the end.");
                }
            }
        }

    }
}
