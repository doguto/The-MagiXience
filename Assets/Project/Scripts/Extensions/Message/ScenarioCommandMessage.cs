namespace Project.Scripts.Extensions.Message
{
    // 会話の途中に書かれたコマンド。会話シーンは通知するだけで、処理はバトル側のツリーが決める
    public readonly struct ScenarioCommandMessage
    {
        public ScenarioCommandMessage(string function, string[] args)
        {
            Function = function;
            Args = args ?? System.Array.Empty<string>();
        }

        public string Function { get; }
        public string[] Args { get; }
    }
}
