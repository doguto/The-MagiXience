using System.Collections.Generic;

namespace Project.Scripts.Infra
{
    public class UserData
    {
        public int clearedStageNumber;
        public Dictionary<string, string> keyConfigMaps;
        
        public UserData()
        {
            clearedStageNumber = 0;
            keyConfigMaps = new ()
            {
                {"Attack","Enter"},
                {"Charge", "Space"},
                {"MoveLeft","AllowLeft"},
                {"MoveRight","AllowRight"},
                {"MoveUp","AllowUp"},
                {"MoveDown","AllowDown"},
            };
        }
    }
}
