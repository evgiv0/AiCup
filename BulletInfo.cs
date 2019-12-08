using AiCup2019.Model;
using AiCup2019.OwnModels;

namespace aicup2019
{
    public struct BulletInfo
    {
        public Vec2Double FirstPoint { get; set; }
        public Vec2Double SecondPoint { get; set; }
        public ParametricLine Line { get; set; }
    }
}