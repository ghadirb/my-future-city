using UnityEngine;

namespace MyFutureCity
{
    public class CharacterAppearance : MonoBehaviour
    {
        public Renderer shirt;
        public GameObject hairShort;
        public GameObject hairLong;
        public Color boyShirt = new Color(0.20f, 0.45f, 0.85f);
        public Color girlShirt = new Color(0.90f, 0.35f, 0.60f);

        public void Apply(string characterId)
        {
            bool girl = characterId == "girl";
            if (shirt != null) shirt.material.color = girl ? girlShirt : boyShirt;
            if (hairLong != null) hairLong.SetActive(girl);
            if (hairShort != null) hairShort.SetActive(!girl);
        }
    }
}
