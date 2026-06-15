using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APP.Components.Dto
{
   
    public class SLStopWatch
    {
        private bool _isRunning = false;
        private DateTime? _startUtc;
        private DateTime? _endUtc;

        public TimeSpan Elapsed
        {
            get
            {
                if (!_startUtc.HasValue)
                {
                    return TimeSpan.Zero;
                }
                if (!_endUtc.HasValue)
                {
                    return (DateTime.UtcNow - _startUtc.Value);
                }
                return (_endUtc.Value - _startUtc.Value);
            }
        }

        public long ElapsedMilliseconds
        {
            get
            {
                return this.ElapsedTicks / TimeSpan.TicksPerMillisecond;
            }
        }

        public long ElapsedTicks
        {
            get { return this.Elapsed.Ticks; }
        }

        public bool IsRunning
        {
            get { return _isRunning; }
            private set { _isRunning = value; }
        }

        public void Reset()
        {
            Stop();
            _endUtc = null;
            _startUtc = null;
        }

        public void Start()
        {
            if (this.IsRunning)
            {
                return;
            }
            if ((_startUtc.HasValue) &&
                (_endUtc.HasValue))
            {
                // Resume the timer from its previous state
                _startUtc = _startUtc.Value +
                    (DateTime.UtcNow - _endUtc.Value);
            }
            else
            {
                // Start a new time-interval from scratch
                _startUtc = DateTime.UtcNow;
            }
            this.IsRunning = true;
            _endUtc = null;
        }

        public void Stop()
        {
            if (this.IsRunning)
            {
                this.IsRunning = false;
                _endUtc = DateTime.UtcNow;
            }
        }
    }
}
