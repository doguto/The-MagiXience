using UnityEngine;

namespace Project.Scenes.StageList.Scripts.View
{
    public class StageListSceneView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer backGroundRenderer;
        
        public void SetBackGround(Sprite sprite)
        {
            backGroundRenderer.sprite = sprite;
        }
    }
}
