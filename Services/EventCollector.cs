using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TimeControl.Agent.WinForms.Models;

namespace TimeControl.Agent.WinForms.Services
{
    internal sealed class EventCollector : IDisposable
    {
        private const int MouseLoops = 10;
        private readonly ActiveWindowReader _windowReader;
        private readonly Timer _timer;
        private readonly List<MonitoringEvent> _pendingEvents;
        private int _lastMouseX;
        private int _lastMouseY;
        private int _mouseLoop;
        private string _lastPid;

        public EventCollector(ActiveWindowReader windowReader)
        {
            _windowReader = windowReader;
            _pendingEvents = new List<MonitoringEvent>();
            _timer = new Timer { Interval = 5000 };
            _timer.Tick += OnTick;
        }

        public event EventHandler EventsCaptured;

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public List<MonitoringEvent> Drain()
        {
            var result = new List<MonitoringEvent>();
            lock (_pendingEvents)
            {
                result.AddRange(_pendingEvents);
                _pendingEvents.Clear();
            }

            return result;
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                var current = _windowReader.Read();
                if (current == null)
                {
                    return;
                }

                var mouseMoved = current.MouseX != _lastMouseX || current.MouseY != _lastMouseY;
                var processChanged = !string.Equals(current.Pid, _lastPid, StringComparison.Ordinal);
                var keepSamplingIdleMouse = false;

                if (!mouseMoved)
                {
                    if (_mouseLoop < MouseLoops)
                    {
                        _mouseLoop++;
                        keepSamplingIdleMouse = true;
                    }
                }
                else
                {
                    _mouseLoop = 0;
                    keepSamplingIdleMouse = true;
                }

                if (!mouseMoved && !keepSamplingIdleMouse && !processChanged)
                {
                    return;
                }

                _lastPid = current.Pid;
                _lastMouseX = current.MouseX;
                _lastMouseY = current.MouseY;

                lock (_pendingEvents)
                {
                    _pendingEvents.Add(current);
                }

                var handler = EventsCaptured;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
            catch
            {
                // Keep the tray agent alive even when a foreground process exits during sampling.
            }
        }
    }
}
