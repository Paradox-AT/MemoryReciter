using Microsoft.Diagnostics.Runtime;
using System;
using System.Diagnostics;

namespace CLRMD
{
    public class CLRStack : IDisposable
    {
        private DataTarget dataTarget;

        public CLRStack(string processName)
        {
            Process process = Process.GetProcessesByName(processName)[0];
            dataTarget = DataTarget.AttachToProcess(pid: process.Id, msecTimeout: 1000, attachFlag: AttachFlag.NonInvasive);
        }

        public bool GetData(bool showCLRVersion = false)
        {
            if (showCLRVersion) { ClrVersion(); }

            ClrRuntime runtime = dataTarget.ClrVersions[0].CreateRuntime();
            Console.WriteLine("Appdomain:");
            foreach (var appDomain in runtime.AppDomains)
            {
                Console.WriteLine("\tAppDomain Name: {0}", appDomain.Name);
                Console.WriteLine("\tApplicationBase: {0}", appDomain.ApplicationBase);
                Console.WriteLine("\tConfigurationFile: {0}", appDomain.ConfigurationFile);

                Console.WriteLine("\n\n\n\tModules:");
                var module = appDomain.Modules;
                for (int i = 0; i < appDomain.Modules.Count; i++)
                {
                    Console.WriteLine("\t\tModule {0}:", i);
                    Console.WriteLine("\t\t\tName: {0}", module[i].Name);
                    Console.WriteLine("\t\t\tIs Dynamic: {0}", module[i].IsDynamic);
                    Console.WriteLine("\t\t\tIs File: {0}", module[i].IsFile);
                    Console.WriteLine("\t\t\tAssembly Id: {0}", module[i].AssemblyId);
                    Console.WriteLine("\t\t\tAssembly Name: {0}", module[i].AssemblyName);
                    Console.WriteLine("\t\t\tPdb FileName: {0}", module[i].Pdb.FileName);
                    Console.WriteLine("\t\t\tPdb Revision: {0}", module[i].Pdb.Revision);
                }

                Console.WriteLine("\n\n\n\tThreads:");
                var thread = runtime.Threads;
                for (int i = 0; i < runtime.Threads.Count; i++)
                {
                    Console.WriteLine("\t\tThread {0}:", i);
                    Console.WriteLine("\t\t\tBlocking Objects: {0}", thread[i].BlockingObjects.Count);
                    Console.WriteLine("\t\t\tManaged Lock Count: {0}", thread[i].LockCount);
                    Console.WriteLine("\t\t\tCurrent Exception: {0}", thread[i].CurrentException);
                    Console.WriteLine("\t\t\tIs Aborted: {0}", thread[i].IsAborted);
                    Console.WriteLine("\t\t\tIs AbortRequested: {0}", thread[i].IsAbortRequested);
                    Console.WriteLine("\t\t\tIs Alive: {0}", thread[i].IsAlive);
                    Console.WriteLine("\t\t\tIs Suspending the Runtime: {0}", thread[i].IsSuspendingEE);
                    Console.WriteLine("\t\t\tIs Unstarted: {0}", thread[i].IsUnstarted);


                    Console.WriteLine("\n\t\t\tStacktrace:");
                    Console.WriteLine("\t\t\t\tStack Poiner\tInstruction pointer\tFrame\n");
                    foreach (ClrStackFrame frame in thread[i].StackTrace)
                    {
                        Console.WriteLine("\t\t\t\t{0:X}\t\t{1:X}\t\t\t{2}", frame.StackPointer, frame.InstructionPointer, frame.ToString());
                    }
                }
            }
            Console.WriteLine("\n\n\n\n");
            return true;
        }

        public bool ClrVersion()
        {
            foreach (ClrInfo version in dataTarget.ClrVersions)
            {
                Console.WriteLine("CLR version: {0}", version.Version.ToString());

                // This is the data needed to request the dac from the symbol server:
                ModuleInfo dacInfo = version.DacInfo;
                Console.WriteLine("Filesize:  {0:X}", dacInfo.FileSize);
                Console.WriteLine("Timestamp: {0:X}", dacInfo.TimeStamp);
                Console.WriteLine("Dac File:  {0}", dacInfo.FileName);

                string dacLocation = version.LocalMatchingDac;
                if (!string.IsNullOrEmpty(dacLocation))
                    Console.WriteLine("Local dac location: " + dacLocation);

            }
            return true;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    dataTarget.Dispose();
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Stack() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
