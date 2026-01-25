using System;
using System.Collections.Generic;

namespace Project.Scripts.Infra
{
    /// <summary>
    /// キー設定のバインディングオーバーライドを保存するデータクラス
    /// 新しいInput Systemのバインディングオーバーライドを文字列として保存
    /// </summary>
    [Serializable]
    public class KeyConfigData
    {
        /// <summary>
        /// Input Actionのバインディングオーバーライドを保存
        /// キー: アクション名 (例: "Player/Move", "Player/Attack")
        /// 値: バインディングオーバーライドのJSON文字列
        /// </summary>
        public List<BindingOverride> bindingOverrides = new();
        
        [Serializable]
        public class BindingOverride
        {
            public string actionName;
            public string bindingId;
            public string overridePath;
        }
    }
}
