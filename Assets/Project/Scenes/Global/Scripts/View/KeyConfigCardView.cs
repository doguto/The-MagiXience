using TMPro;
using UnityEngine;

namespace Project.Scenes.Global.Scripts.View
{
    public class KeyConfigCardView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI name;
        [SerializeField] TextMeshProUGUI primaryKey;
        [SerializeField] TextMeshProUGUI secondaryKey;

        public void Init(string name, string primaryKey, string secondaryKey)
        {
            this.name.text = name;
            this.primaryKey.text = primaryKey;
            this.secondaryKey.text = secondaryKey;
        }
    }
}
