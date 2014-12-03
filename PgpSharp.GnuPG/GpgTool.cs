﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace PgpSharp
{
    /// <summary>
    /// Implements <see cref="IPgpTool"/> as a gnupg cli wrapper.
    /// </summary>
    public class GpgTool : IPgpTool
    {
        #region IPgpTool Members

        /// <summary>
        /// Processes data with stream input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>
        /// Output stream.
        /// </returns>
        /// <exception cref="System.NotSupportedException"></exception>
        public Stream ProcessData(StreamDataInput input)
        {
            throw new NotSupportedException();

            // only way to reliably make this work is save to file and process it instead?

        }

        /// <summary>
        /// Processes data with file input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <exception cref="System.ArgumentNullException">input</exception>
        /// <exception cref="PgpException"></exception>
        public void ProcessData(FileDataInput input)
        {
            if (input == null) { throw new ArgumentNullException("input"); }
            input.Verify();
            using (var proc = new RedirectedProcess(GpgConfig.GpgExePath, CreateCommandLineArgs(input)))
            {
                if (proc.Start())
                {
                    if (input.NeedsPassphrase)
                    {
                        proc.PushInput(input.Passphrase);
                        proc.PushInput(Environment.NewLine);
                    }
                    proc.WaitForExit();

                    Debug.WriteLine("gpg output: " + proc.Output);
                    Debug.WriteLine("gpg exit code: " + proc.ExitCode);
                    var error = proc.Error;
                    if (!string.IsNullOrEmpty(error) && proc.ExitCode != 0)
                    {
                        Debug.WriteLine("gpg error: " + error);
                        throw new PgpException(error);
                    }
                }
            }
        }


        static string CreateCommandLineArgs(DataInput input)
        {
            StringBuilder args = new StringBuilder("--yes --batch ");
            if (input.Armorize)
            {
                args.Append("-a ");
            }
            switch (input.Operation)
            {
                case DataOperation.Decrypt:
                    args.AppendFormat("-d -u \"{0}\" ", input.Recipient);
                    break;
                case DataOperation.Encrypt:
                    args.AppendFormat("-e -r \"{0}\" ", input.Recipient);
                    break;
                case DataOperation.Sign:
                    args.AppendFormat("-s -u \"{0}\" ", input.Originator);
                    break;
                case DataOperation.SignAndEncrypt:
                    args.AppendFormat("-s -e -r \"{0}\" -u \"{1}\" ", input.Recipient, input.Originator);
                    break;
                case DataOperation.Verify:
                    args.Append("--verify ");
                    break;
            }

            if (input.NeedsPassphrase)
            {
                args.Append("--passphrase-fd 0 ");
            }

            var fileInput = input as FileDataInput;
            if (fileInput != null)
            {
                args.AppendFormat("-o \"{0}\" \"{1}\"", fileInput.OutputFile, fileInput.InputFile);
            }

            var finalArg = args.ToString();
            Debug.WriteLine("gpg args: " + finalArg);

            return finalArg;
        }

        #endregion
    }
}
