using DArvis.Types;

namespace DArvis.Components;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class ProcessMonitor : UpdateableComponent
    {
        public List<int> Processes = new List<int>();
        public event EventHandler Updated = delegate { };
        
        public delegate void ProcessEventHandler(int id, EventArgs e);
        public event ProcessEventHandler Attached = delegate { };
        public event ProcessEventHandler Removed = delegate { };
        public ProcessMonitor()
        {
            Timer = new UpdateTimer(TimeSpan.FromMilliseconds(500.0));
        }

        public override void Update(TimeSpan tick)
        {
            Timer.Update(tick);

            if (Timer.Elapsed)
            {
                Timer.Reset();
                base.Pulse();
                
                var count = Process.GetProcessesByName("Darkages");

                if (count.Length != Processes.Count)
                {
                    var id = count.Select(i => i.Id).Except(Processes).FirstOrDefault();
                    var p = count.FirstOrDefault(i => i.Id == id);

                    SetupProcess(p);
                }

                Updated(this, EventArgs.Empty);
            }
        }

        private void SetupProcess(Process p)
        {
            if (Processes.Contains(p.Id))
            {
                return;
            }
            
            try {
                p.EnableRaisingEvents = true;
                p.Exited += PExited;

                Processes.Add(p.Id);
                Attached(p.Id, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(e.StackTrace);
                Process.GetCurrentProcess().Kill();
            }           
        }

        void PExited(object? sender, EventArgs e)
        {
            if (sender == null)
            {
                foreach (var processId in Processes)
                {
                    if (Process.GetProcessById(processId).HasExited)
                    {
                        RemoveProcess(processId);
                        return;
                    }
                }
                return;
            }
            
            var p = (Process)sender;
            RemoveProcess(p.Id);
        }
        
        private void RemoveProcess(int id)
        {
            if (Processes.Contains(id))
            {
                Processes.Remove(id);
                Removed(id, EventArgs.Empty);
            }
        }
    }
