namespace Project.Scenes.Battle.Scripts.Runtime
{
    public enum SignalTarget
    {
        // 自分が出した敵・弾と、それらがさらに出したもの
        Spawned,
        // 同じグループ名を持つ敵・弾
        Group,
        // 場にいるすべての敵・弾 (自分を除く)
        All,
    }

    // ツリーから送られた合図。次のフレームの頭に宛先へ一斉に届く
    public readonly struct Signal
    {
        public Signal(IAgent sender, string key, SignalTarget target, string group)
        {
            Sender = sender;
            SenderSerial = sender?.Serial ?? 0;
            Key = key;
            Target = target;
            Group = group;
        }

        public IAgent Sender { get; }
        public int SenderSerial { get; }
        public string Key { get; }
        public SignalTarget Target { get; }
        public string Group { get; }

        public bool Reaches(IAgent recipient)
        {
            if (recipient == Sender) return false;
            switch (Target)
            {
                case SignalTarget.All:
                    return true;
                case SignalTarget.Group:
                    return !string.IsNullOrEmpty(Group) && recipient.Group == Group;
                default:
                    return Sender == null || IsSpawnedBySender(recipient);
            }
        }

        // 出した側をたどる。使い回されて別の敵・弾になった親は Serial が変わっているので、そこでたどるのをやめる
        bool IsSpawnedBySender(IAgent recipient)
        {
            var owner = recipient.Owner;
            var serial = recipient.OwnerSerial;
            while (owner != null)
            {
                if (owner == Sender && serial == SenderSerial) return true;
                if (owner.Serial != serial) return false;
                serial = owner.OwnerSerial;
                owner = owner.Owner;
            }
            return false;
        }
    }
}
