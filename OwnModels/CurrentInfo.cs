using System.Collections.Generic;
using AiCup2019.Model;

namespace AiCup2019.OwnModels
{
    public class CurrentInfo
    {
        public Unit Me { get; set; }
        public Unit? Enemy { get; set; }
        public LootBox? NearestWeapon { get; set; }
        public LootBox? NearestNotBazuka { get; internal set; }
        public LootBox? NearestHealth { get; set; }
        public LootBox? NearestMine { get; set; }
        public LootBox? BestWeapon { get; set; }
        public Game Game { get; set; }
    }
}