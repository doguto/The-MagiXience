using System.Collections.Generic;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // そのフレームに届いた合図だけを持つ。前のフレームの合図は、次に届いたときに捨てる
    public sealed class SignalInbox
    {
        readonly List<string> keys = new();
        int frame = -1;

        public void Add(string key, int currentFrame)
        {
            if (frame != currentFrame)
            {
                keys.Clear();
                frame = currentFrame;
            }
            keys.Add(key);
        }

        public bool Contains(string key, int currentFrame) => frame == currentFrame && keys.Contains(key);

        public void Clear()
        {
            keys.Clear();
            frame = -1;
        }
    }
}
