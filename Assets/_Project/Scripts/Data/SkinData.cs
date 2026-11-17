using UnityEngine;

namespace Deadzone.Data
{
    [CreateAssetMenu(fileName = "NewSkin", menuName = "Deadzone/Skin Data")]
    public class SkinData : ScriptableObject
    {
        public string skinId = "hazmat_camo";
        public string skinName = "Hazmat Spec-Ops";
        public int gemCost = 50;
        public Color primaryColor = Color.yellow;
        public Color secondaryColor = Color.black;
        public Material customMaterial;
    }
}
