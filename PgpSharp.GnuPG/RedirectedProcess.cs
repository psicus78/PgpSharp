﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace PgpSharp
{
    /// <summary>
    /// Wraps a process with redirected input/outputs.
    /// </summary>
    class RedirectedProcess : IDisposable
    {
        public RedirectedProcess(string exeFile, string args)
        {
            _errors = new StringBuilder();
            _output = new StringBuilder();

            _process = new Process();
            _process.StartInfo.FileName = exeFile;
            _process.StartInfo.Arguments = args;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;

            _process.OutputDataReceived += (s, e) =>
            {
                _output.Append(e.Data);
            };
            _process.ErrorDataReceived += (s, e) =>
            {
                _errors.Append(e.Data);
            };
        }

        Process _process;
        StringBuilder _errors;
        StringBuilder _output;

        public int ExitCode { get; private set; }
        public string Error { get { return _errors.ToString(); } }
        public string Output { get { return _output.ToString(); } }
        public TextWriter Input
        {
            get
            {
                CheckDisposed();
                return _process.StandardInput;
            }
        }

        public bool Start()
        {
            CheckDisposed();
            if (_process.Start())
            {
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
                return true;
            }
            return false;
        }

        public void WaitForExit()
        {
            CheckDisposed();
            _process.WaitForExit();
            ExitCode = _process.ExitCode;
        }

        void CheckDisposed()
        {
            if (_process == null) { throw new ObjectDisposedException(typeof(RedirectedProcess).Name); }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_process != null)
            {
                _process.Dispose();
                _process = null;
            }
        }

        #endregion
    }
}
