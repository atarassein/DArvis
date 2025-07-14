using System;

namespace DArvis.Types;

public class UpdateTimer
{
    public TimeSpan Timer { get; set; }
    public TimeSpan Delay { get; set; }

    public bool Elapsed
    {
        get { return (this.Timer >= this.Delay); }
    }

    public UpdateTimer(TimeSpan delay)
    {
        this.Timer = TimeSpan.Zero;
        this.Delay = delay;
    }

    public void Reset()
    {
        this.Timer = TimeSpan.Zero;
    }
    public void Update(TimeSpan elapsedTime)
    {
        this.Timer += elapsedTime;
    }
}