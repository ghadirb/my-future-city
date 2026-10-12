using System;

namespace MyFutureCity.Core
{
    /// <summary>Coins and experience. Operates directly on SaveData so it is always persisted.</summary>
    public class Wallet
    {
        public const int XpPerLevel = 100;

        private readonly SaveData save;
        public event Action Changed;

        public Wallet(SaveData save)
        {
            if (save == null) throw new ArgumentNullException("save");
            this.save = save;
        }

        public int Coins { get { return save.coins; } }
        public int Xp { get { return save.xp; } }
        public int Level { get { return 1 + save.xp / XpPerLevel; } }
        public int XpIntoLevel { get { return save.xp % XpPerLevel; } }

        public void Add(int coins, int xp)
        {
            if (coins < 0) coins = 0;
            if (xp < 0) xp = 0;
            if (coins == 0 && xp == 0) return;
            save.coins += coins;
            save.xp += xp;
            RaiseChanged();
        }

        public bool TrySpend(int coins)
        {
            if (coins < 0 || coins > save.coins) return false;
            save.coins -= coins;
            RaiseChanged();
            return true;
        }

        private void RaiseChanged()
        {
            var h = Changed;
            if (h != null) h();
        }
    }
}
