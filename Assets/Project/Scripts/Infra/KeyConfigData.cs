using System;
using System.Collections.Generic;

namespace Project.Scripts.Infra
{
    [Serializable]
    public class KeyConfigData
    {
        public List<BindingOverride> bindingOverrides = new();
        
        [Serializable]
        public class BindingOverride
        {
            public string actionPath;
            public string bindingId;
            public string overridePath;
        }
    }
}
