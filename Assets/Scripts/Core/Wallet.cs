using UnityEngine;

namespace KaraPati
{
    /// <summary>
    /// The player's three currencies: gold, Pati stamps and energy, and the energy that refills over time.
    /// It only keeps the numbers and their rules (nothing goes below zero, energy refills up to a limit).
    /// Showing them on screen, sounds and saving stay in GameManager.
    /// </summary>
    public class Wallet
    {
        public int Gold { get; private set; }
        public int Stamps { get; private set; }
        public int Energy { get; private set; }

        /// <summary>Seconds collected towards the next energy point.</summary>
        public float RegenTimer { get; private set; }

        /// <summary>Start from saved (or new-game) values.</summary>
        public void Load(int gold, int stamps, int energy, float regenTimer)
        {
            Gold = gold;
            Stamps = stamps;
            Energy = energy;
            RegenTimer = regenTimer;
        }

        public void AddGold(int amount) { if (amount > 0) Gold += amount; }
        public void AddStamps(int amount) { if (amount > 0) Stamps += amount; }
        public void AddEnergy(int amount) { if (amount > 0) Energy += amount; }

        /// <summary>Takes the gold only if there is enough; false (and nothing taken) otherwise.</summary>
        public bool SpendGold(int amount)
        {
            if (amount < 0 || Gold < amount) return false;
            Gold -= amount;
            return true;
        }

        public bool SpendStamps(int amount)
        {
            if (amount < 0 || Stamps < amount) return false;
            Stamps -= amount;
            return true;
        }

        public bool SpendEnergy(int amount)
        {
            if (amount < 0 || Energy < amount) return false;
            Energy -= amount;
            return true;
        }

        /// <summary>Tops energy up to <paramref name="max"/> (a level up); never takes any away.</summary>
        public void FillEnergy(int max) => Energy = Mathf.Max(Energy, max);

        /// <summary>
        /// Lets <paramref name="seconds"/> pass: one energy point for every <paramref name="every"/> seconds, up to
        /// <paramref name="max"/>. Used every frame and once on loading for the time the game was closed.
        /// Returns how many points were added.
        /// </summary>
        public int Regen(float seconds, int max, float every)
        {
            if (Energy >= max) { RegenTimer = 0f; return 0; }
            RegenTimer += seconds;
            int gain = Mathf.FloorToInt(RegenTimer / every);
            if (gain <= 0) return 0;
            int before = Energy;
            Energy = Mathf.Min(max, Energy + gain);
            RegenTimer = Energy < max ? RegenTimer - gain * every : 0f;
            return Energy - before;
        }

        /// <summary>Seconds left until the next energy point.</summary>
        public int SecondsToNext(float every) => Mathf.CeilToInt(every - RegenTimer);
    }
}
