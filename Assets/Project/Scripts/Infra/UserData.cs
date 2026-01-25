using System.Collections.Generic;

namespace Project.Scripts.Infra
{
    public class UserData
    {
        public int clearedStageNumber;
        public KeyConfigData keyConfigData;
        
        public UserData()
        {
            clearedStageNumber = 0;
            keyConfigData = new KeyConfigData();
        }
    }
}
