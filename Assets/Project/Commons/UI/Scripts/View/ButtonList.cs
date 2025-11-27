namespace Project.Commons.UI.Scripts.View
{
    public class ButtonList : ButtonListBase
    {
        public override void Init(ButtonListType buttonListType, int index = 0, bool isActive = false)
        {
            this.buttonListType = buttonListType;
            
            SetButtonIndex(index);
            buttons[ButtonIndex].SetActive(true);
            
            SetActive(isActive);
        }

        public override void MoveNext(bool isUp = true)
        {
            buttons[ButtonIndex].SetActive(false);
            SetButtonIndex( isUp ? ButtonIndex - 1 : ButtonIndex + 1 );
            buttons[ButtonIndex].SetActive(true);
        }
    }
}