using UnityEngine.Scripting.APIUpdating;

namespace Sunvale.Common.Tweening
{
    public interface ITweenClient   
    {

        public void CustomUpdate(float deltaTime);
        public void SetIndexNumber(int number);
        public int GetIndexNumber();
    }
}
