using System.Reflection.Metadata;
using AiCup2019.Model;
using AiCup2019.OwnModels;

namespace aicup2019
{
    public class BulletInfo
    {
        public Vec2Double? FirstPoint { get; set; }

        private Vec2Double? _sec;
        public Vec2Double? SecondPoint
        {
            get { return _sec; }
            set
            {
                var tmp = value;
                _sec = value;
                if (FirstPoint.HasValue && SecondPoint.HasValue)
                {
                    var deltaX = SecondPoint.Value.X - FirstPoint.Value.X;
                    var deltaY = SecondPoint.Value.Y - FirstPoint.Value.Y;
                    _sec = new Vec2Double(FirstPoint.Value.X + deltaX * 20, FirstPoint.Value.Y + deltaY * 20);
                }
            }
        }



        public ParametricLine Line { get; set; }

        public bool СontainsPoint(Vec2Double currentBulletPosition)
        {
            if (FirstPoint.HasValue && SecondPoint.HasValue)
            {
                var dxc = currentBulletPosition.X - FirstPoint.Value.X;
                var dyc = currentBulletPosition.Y - FirstPoint.Value.Y;

                var dxl = SecondPoint.Value.X - FirstPoint.Value.X;
                var dyl = SecondPoint.Value.Y - FirstPoint.Value.Y;

                var cross = dxc * dyl - dyc * dxl;

                if (cross == 0)
                    return true;
            }

            return false;

        }
    }
}